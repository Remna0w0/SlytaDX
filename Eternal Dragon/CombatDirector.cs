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
        int dialogIndex;

        public CombatDirector(string playerName, string weaponName)
        {
            EquipWeapon(weaponName);
            _player.Name = playerName;
            _dragon = new Combatant("Dragon", 1000, 60);
        }

        public void EquipWeapon(string weaponName)
        {
            weapon = WeaponLibrary.GetWeapon(weaponName);

            // Initialize player stats based on weapon
            _player = new Combatant("Player", 500, weapon.BasePower);

            // Apply passive start-of-fight effects
            if (weapon.StartsChanneled)
            {
                _player.Statuses["Channeled"] = 1.1;
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
                blocker.BaseBuff -= 15;
                return new TurnIntent { Action = "Block", DamageMultiplier = 1.0 };
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
                dodger.BaseBuff -= 15;
                return new TurnIntent { Action = "Dodge", DamageMultiplier = 1.0 };
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
            if (_dragon.HP < 300)
            {
                int scaredRoll = _random.Next(1, 101);
                if (scaredRoll <= 37) return GetBlockIntent(_dragon);
                if (scaredRoll <= 75) return GetDodgeIntent(_dragon);
                if (scaredRoll <= 80) return GetBraveIntent(_dragon);
                return GetSpellIntent(_dragon);
            }

            int roll = _random.Next(1, 101);
            if (roll <= 60) return GetBraveIntent(_dragon);
            if (roll <= 80) return GetSpellIntent(_dragon);
            return GetBlockIntent(_dragon);
        }

        public string ResolveTurn(TurnIntent playerIntent, TurnIntent dragonIntent)
        {
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

            if (playerIntent.Action == "Brave")
            {
                if (playerDmg > 0)
                {
                    _player.BaseBuff = 0; 
                }
            }

            if (dragonIntent.Action == "Brave")
            {
                if (dragonDmg > 0)
                {
                    _dragon.BaseBuff = 0; 
                }
            }

            if (playerDmg > dragonDmg && playerIntent.Action == "Brave" && dragonIntent.Action == "Brave")
            {
                playerDmg = (int)(playerDmg * 1.2);
            }

            if (dragonDmg > playerDmg && playerIntent.Action == "Brave" && dragonIntent.Action == "Brave")
            {
                dragonDmg = (int)(dragonDmg * 1.2);
            }

            if (playerDmg > dragonDmg && playerIntent.Action == "Spell")
            {
                if (_player.Statuses.ContainsKey("Channeled"))
                {
                    _player.Statuses["Channeled"] += 0.1;
                }
                else
                {
                    _player.Statuses["Channeled"] = 1.1;
                }

            }
            else if (playerDmg < dragonDmg && playerIntent.Action == "Spell")
            {
                dragonDmg = (int)(dragonDmg * 1.2);
            }

            else if (dragonDmg > playerDmg && dragonIntent.Action == "Spell")
            {
                if (_dragon.Statuses.ContainsKey("Channeled"))
                {
                    _dragon.Statuses["Channeled"] += 0.1;
                }
                else
                {
                    _dragon.Statuses["Channeled"] = 1.1;
                }

            }
            else if (dragonDmg < playerDmg && dragonIntent.Action == "Spell")
            {
                playerDmg = (int)(dragonDmg * 1.2);
            }

            _dragon.HP -= playerDmg;
            _player.HP -= dragonDmg;

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

            if (_dragon.Statuses.ContainsKey("Enraged") && _dragon.IsFirstEnragedAttack)
            {
                _player.BasePower = Math.Max(0, _player.BasePower - 15);
                _player.Statuses["Sunder"] = 1.0; // Mark as Sundered for flavor/tracking
                _dragon.IsFirstEnragedAttack = false; // Only happens once
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

            sb.AppendLine(DialogContainer.GetText($"Player_Intent_{player.Action}"));
            sb.AppendLine(DialogContainer.GetText($"Dragon_Intent_{dragon.Action}"));
            sb.AppendLine(""); 

            if (player.Action == "Block")
            {
                if (player.DamageMultiplier == 0) sb.AppendLine(DialogContainer.GetText("Player_Block_Neutralize"));
                else if (player.DamageMultiplier > 1.0) sb.AppendLine(DialogContainer.GetText("Player_Block_Failure"));
                else sb.AppendLine(DialogContainer.GetText("Player_Block_Mitigate"));
            }
            else if (player.Action == "Spell")
            {
                sb.AppendLine(playerDmg > dragonDmg
                    ? DialogContainer.GetText("Player_Spell_Success_Channeled")
                    : DialogContainer.GetText("Player_Spell_Overpowered_Empowered"));
            }
            else if (player.Action == "Dodge")
            {
                if (player.DamageMultiplier == 0) sb.AppendLine(DialogContainer.GetText("Player_Dodge_Success"));
                else sb.AppendLine(DialogContainer.GetText("Player_Dodge_Failure"));
            }

            if (dragon.Action == "Block")
            {
                if (dragon.DamageMultiplier == 0) sb.AppendLine(DialogContainer.GetText("Dragon_Block_Neutralize"));
                else if (dragon.DamageMultiplier > 1.0) sb.AppendLine(DialogContainer.GetText("Dragon_Block_Failure"));
                else sb.AppendLine(DialogContainer.GetText("Dragon_Block_Mitigate"));
            }
            else if (dragon.Action == "Spell")
            {
                sb.AppendLine(dragonDmg > playerDmg
                    ? DialogContainer.GetText("Dragon_Spell_Success_Channeled")
                    : DialogContainer.GetText("Dragon_Spell_Overpowered_Empowered"));
            }
            else if (dragon.Action == "Dodge")
            {
                if (dragon.DamageMultiplier == 0) sb.AppendLine(DialogContainer.GetText("Dragon_Dodge_Success"));
                else sb.AppendLine(DialogContainer.GetText("Dragon_Dodge_Failure"));
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