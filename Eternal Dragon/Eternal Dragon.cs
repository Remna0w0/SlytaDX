using TwitchLib.Client.Events;

namespace RemnaBotService
{
    public class EternalDragon : TwitchClientContainer
    {
        private TwitchClientContainer client;

        public void SetClient(TwitchClientContainer client)
        {
            this.client = client;
        }

        public async Task Dragon(string username)
        {
            bool playAgain = true;
            while (playAgain)
            {
                client.Say("Battle the Eternal Dragon! 500HP vs 1000HP.  'BLOCK' (50% dmg, +20 atk) or 'BRAVE' (high risk/reward).");

                CombatDirector fight = new CombatDirector(); // Initialize CombatDirector here.

                bool startQuit = false;
                do
                {
                    client.Say("Start or Quit?");
                    string[] startQuitOptions = { "Start", "Quit" };
                    int startQuitIndex = await GetUserSelection(startQuitOptions, username);

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

                while (fight.startGame)
                {
                    client.Say($"HP: {fight.playerHP} Dragon: {fight.dragonHP} Buff: {fight.blockBuff}");
                    client.Say("BRAVE or BLOCK?");

                    string[] actionOptions = { "BRAVE", "BLOCK" };
                    int actionIndex = await GetUserSelection(actionOptions, username);

                    if (actionIndex == 0)
                    {
                        fight.Brave();
                        fight.HPCheck(client);
                    }
                    else
                    {
                        fight.Block();
                        fight.HPCheck(client);
                    }
                }

                if (fight.quitGame)
                {
                    client.Say("Restart (R) or Quit?");
                    string input = await GetUserInput(username);
                    if (input.Equals("R", StringComparison.OrdinalIgnoreCase))
                    {
                        fight.Reset();
                        fight.startGame = false;
                        fight.quitGame = false;
                        client.Say("\n\n\n\n");
                    }
                    else
                    {
                        playAgain = false;
                        client.Say("Thanks for playing!");
                        Thread.Sleep(5000);
                    }
                }
            }
        }

        private async Task<int> GetUserSelection(string[] options, string username) // async Task<int>
        {
            client.Say("Use !1, !2, etc. to select:");
            for (int i = 0; i < options.Length; i++)
            {
                client.Say($"{i + 1}. {options[i]}");
            }

            while (true)
            {
                string input = await GetUserInput(username); // Await here
                if (int.TryParse(input.Substring(1), out int selection) && selection >= 1 && selection <= options.Length)
                {
                    return selection - 1;
                }
                else
                {
                    client.Say("Invalid selection. Please use !1, !2, etc.");
                }
            }
        }

        private async Task<string> GetUserInput(string username) // async Task<string>
        {
            await client.InitializationComplete(); // Wait for initialization

            var tcs = new TaskCompletionSource<string>();

            void MessageReceivedHandler(object? sender, OnMessageReceivedArgs e)
            {
                if (e.ChatMessage.Username == username)
                {
                    client.Client.OnMessageReceived -= MessageReceivedHandler; // Use client.Client
                    tcs.SetResult(e.ChatMessage.Message);
                }
            }

            client.Client.OnMessageReceived += MessageReceivedHandler; // Use client.Client

            return await tcs.Task;
        }
    }
}