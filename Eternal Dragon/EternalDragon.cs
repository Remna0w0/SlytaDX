using RemnaBotService.Eternal_Dragon;
using TwitchLib.Client.Events;

namespace RemnaBotService
{
    public class EternalDragon 
    {
        public BotMessenger _messenger { get; }

        public EternalDragon(BotMessenger messenger)
        {
            _messenger = messenger;
        }

        public async Task Dragon(string username)
        {
            bool playAgain = true;
            while (playAgain)
            {
                _messenger.SendGameMessage(new GameMessage {
                    Title = "Battle the Eternal Dragon!",
                    Description = "500HP vs 1000HP.  Use your wits and a bit of lucky to outlast the Dragon!"
                });

                CombatDirector fight = new CombatDirector(); // Initialize CombatDirector here.

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
                        fight.startGame = true; // Directly start the game with default HP
                    }
                    else
                    {
                        fight.quitGame = true;
                        startQuit = true;
                    }

                } while (!fight.startGame && !fight.quitGame);

                _messenger.SendGameMessage(new GameMessage
                {
                    Title = "Battle Rules",
                    Description = "You and the Dragon roll random damage numbers each turn. The higher number determines the attacker.\n\n" + "BRAVE: High risk! High damage, but you take more damage if the dragon counters.\n" +
                  "BLOCK: Low risk! Mitigates incoming damage to keep your HP up, while also boosting your BLOCK BUFF, giving you guaranteed power to your next attack!"
                });

                while (fight.startGame)
                {
                    _messenger.SendGameMessage(new GameMessage
                    {
                        Title = "Combat Tactical Menu",
                        Description = "BRAVE or BLOCK\n" + $"--- Current Status: HP {fight.playerHP} | Dragon {fight.dragonHP} | BLOCK BUFF {fight.blockBuff} ---"
                    });

                    string[] actionOptions = { "BRAVE", "BLOCK" };
                    int actionIndex = await _messenger.GetUserSelectionAsync(actionOptions, username);
                    string resultMessage = string.Empty;
                    string conclusionMessage = string.Empty;
                    if (actionIndex == 0)
                    {
                        resultMessage = fight.Brave();
                        conclusionMessage = fight.HPCheck();
                    }
                    else
                    {
                        resultMessage = fight.Block();
                        conclusionMessage = fight.HPCheck();
                    }

                    _messenger.SendGameMessage(new GameMessage
                    {
                        Title = "Combat Results",
                        Description = $"{resultMessage}\n{conclusionMessage}"
                    });
                    
                }

                if (fight.quitGame)
                {
                    _messenger.SendGameMessage(new GameMessage { Title = "Select Screen", Description = "Restart (R) or Quit?" });
                    string input = await _messenger.GetUserInputAsync(username);
                    if (input.Equals("%R", StringComparison.OrdinalIgnoreCase) || input.Equals("R", StringComparison.OrdinalIgnoreCase))
                    {
                        fight.Reset();
                        fight.startGame = false;
                        fight.quitGame = false;
                        _messenger.SendGameMessage(new GameMessage { Title = "Select Screen", Description = "\n\n\n\n" });
                    }
                    else
                    {
                        playAgain = false;
                        _messenger.SendGameMessage(new GameMessage { Title = "Select Screen", Description = "Thanks for playing!" });
                        Thread.Sleep(5000);
                    }
                }
            }
        }

    }
}