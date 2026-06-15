using Microsoft.Extensions.Logging;
using RemnaBotService.Eternal_Dragon;
using System.Text;

namespace RemnaBotService
{
    internal class CombatDirector
    {

        public bool startGame = false;
        public bool quitGame = false;
        public Combatant _player;
        public Combatant _dragon;
        public Weapon weapon;
        private Random _random = new Random();
        private bool _hasEnrageBeenReported = false;
        private bool _hasSunderBeenReported = false;
        private bool _wasSunderInflictedThisTurn = false;

        public CombatDirector(string playerName, string weaponName)
        {
            EquipWeapon(weaponName);
            _player.Name = playerName;
            _dragon = new Combatant("Dragon", 1200, 60);
        }

        public void EquipWeapon(string weaponName)
        {
            weapon = WeaponLibrary.GetWeapon(weaponName);

            // Initialize player stats based on weapon
            _player = new Combatant("Player", 750, weapon.BasePower);

            // Apply passive start-of-fight effects
            if (weapon.StartsChanneled)
            {
                _player.Statuses["Channeled"] = 1.1;
            }

            if (weapon.SpellBoosted)
            {
                _player.SpellBase = 80;
            }
        }

        public string GetWeaponName() => weapon?.Name ?? "None";
        public int GetBasePower() => weapon?.BasePower ?? 50;
        public TurnIntent GetBlockIntent(Combatant blocker)
        {
            int roll = _random.Next(1, 101);

            if (roll <= 50) // Mitigate
            {
                blocker.BaseBuff += 10;
                return new TurnIntent { Action = "Block", DamageMultiplier = 0.5 };
            }
            else if (roll <= 90) // Complete Block
            {
                blocker.BaseBuff += 20;
                return new TurnIntent { Action = "Block", DamageMultiplier = 0.0 };
            }
            else // Failure
            {
                if (blocker.BaseBuff - 15 <= 0)
                {
                    blocker.BaseBuff = 0;
                    return new TurnIntent { Action = "Block", DamageMultiplier = 1.0 };
                }
                else
                {
                    blocker.BaseBuff -= 15;
                    return new TurnIntent { Action = "Dodge", DamageMultiplier = 1.0 };
                }
            }
        }

        public TurnIntent GetBraveIntent(Combatant braver)
        {
            return new TurnIntent { Action = "Brave", FlatDamage = braver.GetCalculatedAttack(false) };
        }

        public TurnIntent GetDodgeIntent(Combatant dodger)
        {
            int roll = _random.Next(1, 101);
            roll = (int)Math.Min(101, roll - weapon.DodgeBonus);
            bool success = roll <= 75;

            if (success)
            {
                dodger.BaseBuff += 5;
                return new TurnIntent { Action = "Dodge", DamageMultiplier = 0.0 };
            }
            else
            {
                if (dodger.BaseBuff - 15 <= 0)
                {
                    dodger.BaseBuff = 0;
                    return new TurnIntent { Action = "Dodge", DamageMultiplier = 1.0 };
                }
                else
                {
                    dodger.BaseBuff -= 15;
                    return new TurnIntent { Action = "Dodge", DamageMultiplier = 1.0 };
                }
            }
        }


        public TurnIntent GetSpellIntent(Combatant caster)
        {
            return new TurnIntent { Action = "Spell", FlatDamage = caster.GetCalculatedAttack(true) };
        }

        public TurnIntent GetFleeIntent()
        {
            return new TurnIntent { Action = "Flee" };
        }


        public class TurnIntent
        {
            public string Action { get; set; } // "Block", "Brave", "Spell", "Dodge"
            public double DamageMultiplier { get; set; } = 1.0;
            public int FlatDamage { get; set; } = 0;

        }

        public TurnIntent GetDragonIntent()
        {
            // =========================================================================
            // SMART ENDGAME TACTICIAN AI (Triggers when Dragon is wounded below 500 HP)
            // =========================================================================
            if (_dragon.HP < 500)
            {
                int customRoll = _random.Next(1, 101);

                // CONDITIONAL PRIORITY 1: High BaseBuff Cash-out
                if (_dragon.BaseBuff >= 25)
                {
                    // 70% chance to unleash that massive accumulated raw damage
                    if (customRoll <= 70) return GetBraveIntent(_dragon);
                    if (customRoll <= 85) return GetSpellIntent(_dragon);
                    return GetBlockIntent(_dragon);
                }

                // CONDITIONAL PRIORITY 2: Holding a Slip Counter
                if (_dragon.Statuses.ContainsKey("SlipCounter"))
                {
                    // Highly aggressive to maximize the multiplier window before it clears
                    if (customRoll <= 50) return GetBraveIntent(_dragon);
                    if (customRoll <= 90) return GetSpellIntent(_dragon);
                    return GetDodgeIntent(_dragon);
                }

                // CONDITIONAL PRIORITY 3: High Magical Channeling
                if (_dragon.Statuses.ContainsKey("Channeled") && _dragon.Statuses["Channeled"] >= 1.3)
                {
                    // 65% chance to drop a heavily scaled Spell payload
                    if (customRoll <= 65) return GetSpellIntent(_dragon);
                    if (customRoll <= 85) return GetBraveIntent(_dragon);
                    return GetBlockIntent(_dragon);
                }

                // DEFAULT ENDGAME CONDITION: None of the above apply -> Turtle Up
                // Heavily defensive shell (80% combined block/dodge) to safely fish for buffs
                if (customRoll <= 45) return GetBlockIntent(_dragon);
                if (customRoll <= 80) return GetDodgeIntent(_dragon);
                if (customRoll <= 90) return GetBraveIntent(_dragon);
                return GetSpellIntent(_dragon);
            }

            // =========================================================================
            // STANDARD AI BEHAVIOR (Above 500 HP)
            // =========================================================================
            int roll = _random.Next(1, 101);
            if (roll <= 60) return GetBraveIntent(_dragon);
            if (roll <= 80) return GetSpellIntent(_dragon);
            return GetBlockIntent(_dragon);
        }

        public string ResolveTurn(TurnIntent playerIntent, TurnIntent dragonIntent)
        {
            _wasSunderInflictedThisTurn = false;
            if (playerIntent.Action == "Flee")
            {

                startGame = false;
                quitGame = true;
                return DialogContainer.GetText("PlayerFlee");
            }

            if (playerIntent.Action != "Brave" &&  playerIntent.Action != "Spell")
            {
                if (_player.Statuses.ContainsKey("SlipCounter")) _player.Statuses.Remove("SlipCounter");
            }

            if (dragonIntent.Action != "Brave" && dragonIntent.Action != "Spell")
            {
                if (_dragon.Statuses.ContainsKey("SlipCounter")) _dragon.Statuses.Remove("SlipCounter");
            }

            if (playerIntent.Action == "Dodge" && playerIntent.DamageMultiplier == 0.0)
            {
                _player.Statuses["SlipCounter"] = weapon.SlipCounterMultiplier;
            }

            if (dragonIntent.Action == "Dodge" && dragonIntent.DamageMultiplier == 0.0)
            {
                _dragon.Statuses["SlipCounter"] = 1.75;
            }

            int playerDmg = (int)(playerIntent.FlatDamage * dragonIntent.DamageMultiplier);
            int dragonDmg = (int)(dragonIntent.FlatDamage * playerIntent.DamageMultiplier);



            if (playerDmg > dragonDmg && playerIntent.Action == "Brave" && dragonIntent.Action == "Brave")
            {
                playerDmg = (int)(playerDmg * 1.2);
            }

            if (dragonDmg > playerDmg && playerIntent.Action == "Brave" && dragonIntent.Action == "Brave")
            {
                dragonDmg = (int)(dragonDmg * 1.2);
            }

            if (playerIntent.Action == "Spell" && (dragonIntent.Action == "Brave" || dragonIntent.Action == "Spell"))
            {
                if (playerDmg > dragonDmg)
                    _player.Statuses["Channeled"] = _player.Statuses.ContainsKey("Channeled") ? _player.Statuses["Channeled"] + 0.1 : 1.1;
                else if (playerDmg < dragonDmg)
                    dragonDmg = (int)(dragonDmg * 1.2);
            }

            if (dragonIntent.Action == "Spell" && (playerIntent.Action == "Brave" || playerIntent.Action == "Spell"))
            {
                if (dragonDmg > playerDmg)
                    _dragon.Statuses["Channeled"] = _dragon.Statuses.ContainsKey("Channeled") ? _dragon.Statuses["Channeled"] + 0.1 : 1.1;
                else if (dragonDmg < playerDmg)
                    playerDmg = (int)(playerDmg * 1.2);
            }

            _dragon.HP -= playerDmg;
            _player.HP -= dragonDmg;

            if (playerIntent.Action == "Brave" && playerDmg > 0)
            {
                    _player.BaseBuff = 0;
            }

            if (dragonIntent.Action == "Brave" && dragonDmg > 0)
            {
                    _dragon.BaseBuff = 0;
            }

            if (playerIntent.Action == "Brave" || playerIntent.Action == "Spell")
            {
                if (_player.Statuses.ContainsKey("SlipCounter")) _player.Statuses.Remove("SlipCounter");
            }

            if (dragonIntent.Action == "Brave" || dragonIntent.Action == "Spell")
            {
                if (_dragon.Statuses.ContainsKey("SlipCounter")) _dragon.Statuses.Remove("SlipCounter");
            }

            if (weapon.HasMighty)
            {
                if (playerDmg > dragonDmg || dragonIntent.Action == "Block")
                {
                    _player.BasePower += 10;
                }
            }

            if (_dragon.Statuses.ContainsKey("Enraged") && _dragon.IsFirstEnragedAttack &&  dragonDmg > 0)
            {                
                    _player.BasePower = Math.Max(0, _player.BasePower - 15);
                    _player.SpellBase = Math.Max(0, _player.SpellBase - 25);
                    _player.Statuses["Sunder"] = 1.0;

                    _dragon.IsFirstEnragedAttack = false;
                    _wasSunderInflictedThisTurn = true;
            }

            if (_dragon.HP < 401)
            {
                if (_dragon.Statuses.ContainsKey("Enraged"))
                {
                    _dragon.Statuses["Enraged"] += 0.05;
                }
                else
                {
                    _dragon.Statuses["Enraged"] = 1.2;
                    _dragon.IsFirstEnragedAttack = true;
                }
            }
            return FormulateTurnNarrative(playerIntent, dragonIntent, playerDmg, dragonDmg);

        }

        public string FormulateTurnNarrative(TurnIntent player, TurnIntent dragon, int playerDmg, int dragonDmg)
        {
            var sb = new StringBuilder();

            if (player.Action == "Block" && dragon.Action == "Block")
            {
                sb.AppendLine(DialogContainer.GetText("BlockStalemate"));
            }
            else if (player.Action == "Dodge" && dragon.Action == "Dodge")
            {
                sb.AppendLine(DialogContainer.GetText("DodgeStalemate"));
            }
            else if ((player.Action == "Dodge" && dragon.Action == "Block") || (player.Action == "Block" && dragon.Action == "Dodge"))
            {
                sb.AppendLine("🛡️ Both fighters pull back into defensive maneuvers, observing each other closely without trading blows!");
            }
            else
            {
                sb.AppendLine(DialogContainer.GetText($"Player_Intent_{player.Action}"));
                sb.AppendLine(DialogContainer.GetText($"Dragon_Intent_{dragon.Action}"));
            }

            sb.AppendLine("");

            if (player.Action == "Block")
            {
                if (player.DamageMultiplier == 0)
                    sb.AppendLine("🛡️ **Player Block (Perfect):** You completely absorbed the impact! Your stance hardens, granting you **+20 Base Buff**.");
                else if (player.DamageMultiplier >= 1.0)
                    sb.AppendLine("❌ **Player Block (Failed):** Your guard shattered! Stance compromised.");
                else
                    sb.AppendLine("🛡️ **Player Block (Mitigated):** You safely absorbed half the attack energy, gaining **+10 Base Buff**.");
            }
            else if (player.Action == "Dodge")
            {
                if (player.DamageMultiplier == 0)
                    sb.AppendLine("💨 **Player Dodge (Success):** You successfully evaded the threat, banking **+5 Base Buff** for your next counter-attack.");
                else
                    sb.AppendLine("❌ **Player Dodge (Failed):** Your footing slipped! Stance compromised.");
            }
            else if (player.Action == "Spell" && (dragon.Action == "Brave" || dragon.Action == "Spell"))
            {
                sb.AppendLine(playerDmg > dragonDmg
                    ? DialogContainer.GetText("Player_Spell_Success_Channeled")
                    : DialogContainer.GetText("Player_Spell_Overpowered_Empowered"));
            }

            if (dragon.Action == "Block")
            {
                if (dragon.DamageMultiplier == 0)
                    sb.AppendLine("🐲 **Dragon Block (Perfect):** The beast neutralized the momentum perfectly, stacking **+20 Base Buff**.");
                else if (dragon.DamageMultiplier >= 1.0)
                    sb.AppendLine("❌ **Dragon Block (Failed):** The dragon's guard failed to raise in time.");
                else
                    sb.AppendLine("🐲 **Dragon Block (Mitigated):** The dragon mitigated the brunt of the strike, banking **+10 Base Buff**.");
            }
            else if (dragon.Action == "Dodge")
            {
                if (dragon.DamageMultiplier == 0)
                    sb.AppendLine("💨 **Dragon Dodge (Success):** The dragon smoothly sidestepped, accumulating **+5 Base Buff**.");
                else
                    sb.AppendLine("❌ **Dragon Dodge (Failed):** The dragon was too slow to avoid the impact.");
            }
            else if (dragon.Action == "Spell" && (player.Action == "Brave" || player.Action == "Spell"))
            {
                sb.AppendLine(dragonDmg > playerDmg
                    ? DialogContainer.GetText("Dragon_Spell_Success_Channeled")
                    : DialogContainer.GetText("Dragon_Spell_Overpowered_Empowered"));
            }

            if (_wasSunderInflictedThisTurn && !_hasSunderBeenReported)
            {
                sb.AppendLine("\n💥 **CRITICAL SHATTER:** The Enraged Dragon's attack breaches your defenses, inflicting **Sunder**! Your physical Base Power is reduced by 15 and your Spell Base is suppressed by 25!");
                _hasSunderBeenReported = true;
            }

            if (_dragon.HP < 401 && !_hasEnrageBeenReported)
            {
                sb.AppendLine("\n🛑 **BEWARE:** The Eternal Dragon drops below 401 HP! Crimson runes ignite across its scales as it enters an **Enraged** state, preparing a devastating Sunder attack!");
                _hasEnrageBeenReported = true;
            }

            sb.AppendLine($"\n⚔️ **Turn Result:** You dealt {playerDmg} damage! The Dragon dealt {dragonDmg} damage!");

            return sb.ToString();
        }




        /* For Discord Only

            public void DragonSet()
            {
                dragonHP = 0;
                bool isInt;
                do
                {
                    Console.Write("Dragon's HP: ");
                    string playerInput = Console.ReadLine().ToUpper();
                    isInt = int.TryParse(playerInput, out int result);
                    if (result <= 0 || !isInt)
                    {
                        Console.WriteLine("Invalid input, please enter a number greater than 0");

                    }
                    dragonHP = result;
                }
                while (dragonHP <= 0 || !isInt);


            }

            public void PlayerSet()
            {
                playerHP = 0;
                bool isInt;
                do
                {
                    Console.Write("Your HP: ");
                    string playerInput = Console.ReadLine().ToUpper();
                    isInt = int.TryParse(playerInput, out int result);
                    if (result <= 0 || !isInt)
                    {
                        Console.WriteLine("Invalid input, please enter a number greater than 0");

                    }
                    playerHP = result;
                }
                while (playerHP <= 0 || !isInt);


            }

          */




        public void Reset()
        {
            _player = new Combatant("Player", 500, weapon.BasePower);
            _dragon = new Combatant("Dragon", 1000, 60);

            startGame = true;
            quitGame = false;

            if (weapon.StartsChanneled) _player.Statuses["Channeled"] = 1.1;
        }

    }
}