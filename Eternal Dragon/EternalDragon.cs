using RemnaBotService.Eternal_Dragon;

namespace RemnaBotService
{
    public class EternalDragon
    {
        public BotMessenger _messenger { get; }
        Random random = new Random();

        public EternalDragon(BotMessenger messenger)
        {
            _messenger = messenger;
        }

        public async Task Dragon(string username)
        {
            bool playAgain = true;
            while (playAgain)
            {
                _messenger.SendGameMessage(new GameMessage
                {
                    Title = "Battle the Eternal Dragon!",
                    Description = "500HP vs 1000HP.  Use your wits and a bit of luck to outlast the Dragon!"
                });
                _messenger.SendGameMessage(new GameMessage
                {
                    Title = "Battle Rules",
                    Description = "You and the dragon pick your moves in the same turn. The dragon can do everything you can.\n\n" + "BRAVE: Attack with BASE BUFF and all multipliers applied, but you take more damage if the dragon also BRAVES and has a higher damage roll.\n" +
   "BLOCK: Mitigate or completelty neutralize the opponents attack, granting you BASE BUFF depending on the BLOCK potency. However, there's a small chance for catastrophic failure!\n" +
   "SPELL: Attack without BASE BUFF but with a much higher base attack. If your damage number is higher than the dragon's, regardless of which attack they chose, you gain the Channeled status. If lower, the dragons attack is empowered.\n" +
   "DODGE: Has a higher chance to neutralize damage than BLOCK, but also a higher chance to fail! A successful DODGE grants the Slip Counter status."
                });

                CombatDirector fight = null;
                bool startQuit = false;
                do
                {
                    _messenger.SendGameMessage(new GameMessage
                    {
                        Title = "Select Screen",
                        Description = "Start or Quit?"
                    });

                    string[] startQuitOptions = { "Start", "Quit" };
                    int startQuitIndex = await _messenger.GetUserSelectionAsync(startQuitOptions, username);

                    if (startQuitIndex == 0)
                    {
                        string[] weaponOptions = { "Stalwart Blade", "Nightfall Axe", "Brilliant Caststaff", "Unseen Daggers" };
                        int weaponIndex = await _messenger.GetUserSelectionAsync(weaponOptions, username);
                        string selectedWeapon = weaponOptions[weaponIndex];

                        fight = new CombatDirector(username, selectedWeapon);
                        fight.startGame = true;
                        startQuit = true;


                    }
                    else
                    {
                        fight = new CombatDirector(username, "None");
                        fight.quitGame = true;
                        startQuit = true;
                    }

                } while (!fight.startGame && !fight.quitGame);


                while (fight.startGame)
                {
                    string statusList = fight._player.Statuses.Count > 0
                        ? string.Join(", ", fight._player.Statuses.Keys)
                        : "None";

                    string[] actionOptions = { "BRAVE", "BLOCK", "DODGE", "SPELL", "FLEE" };
                    int actionIndex = await _messenger.GetUserSelectionAsync(actionOptions, username);
                    await Task.Delay(750);

                    CombatDirector.TurnIntent playerIntent = actionIndex switch
                    {
                        0 => fight.GetBraveIntent(fight._player),
                        1 => fight.GetBlockIntent(fight._player),
                        2 => fight.GetDodgeIntent(fight._player),
                        3 => fight.GetSpellIntent(fight._player),
                        _ => fight.GetFleeIntent()
                    };

                    CombatDirector.TurnIntent dragonIntent = fight.GetDragonIntent();
                    string turnResult = fight.ResolveTurn(playerIntent, dragonIntent);

                    bool gameOver = false;
                    string conclusionMessage = "";

                    if (fight._player.HP <= 0 || fight._dragon.HP <= 0)
                    {
                        fight.startGame = false;
                        fight.quitGame = true;
                        gameOver = true;

                        string conclusion = fight._player.HP <= 0
                            ? DialogContainer.GetText("PlayerDefeat")
                            : DialogContainer.GetText("PlayerVictory");

                        conclusionMessage = $"**{conclusion}**\n\nYour Final HP: {fight._player.HP} | Dragon's Final HP: {fight._dragon.HP}";
                    }

                    _messenger.SendGameMessage(new GameMessage
                    {
                        Title = "Combat Results",
                        Description = turnResult
                    });

                    await Task.Delay(750);

                    if (!gameOver)
                    {
                        _messenger.SendGameMessage(new GameMessage
                        {
                            Title = "Combat Tactical Menu",
                            Description = $"--- Next Turn Status: HP {fight._player.HP} | Dragon {fight._dragon.HP} | BASE BUFF {fight._player.BaseBuff} ---\nSTATUS: {statusList}"
                        });
                        await Task.Delay(750);
                    }
                    else
                    {
                        _messenger.SendGameMessage(new GameMessage
                        {
                            Title = "The Battle is Decided!",
                            Description = conclusionMessage
                        });

                        await Task.Delay(750);
                    }
                }

                if (fight.quitGame)
                    {
                        _messenger.SendGameMessage(new GameMessage { Title = "Select Screen", Description = "Restart (R) or Quit?" });
                        string input = await _messenger.GetUserInputAsync(username);

                        if (input == null)
                        {
                            playAgain = false;
                            _messenger.SendGameMessage(new GameMessage { Title = "Timeout", Description = "Timed out. Thanks for playing!" });
                        }

                        else if (input.Equals("%R", StringComparison.OrdinalIgnoreCase) || input.Equals("R", StringComparison.OrdinalIgnoreCase))
                        {
                            fight.Reset();
                            fight.startGame = true;
                            fight.quitGame = false;
                        }

                        else
                        {
                            playAgain = false;
                            _messenger.SendGameMessage(new GameMessage { Title = "Select Screen", Description = "Thanks for playing!" });
                        }
                    }
                }
            }

        }
    }
