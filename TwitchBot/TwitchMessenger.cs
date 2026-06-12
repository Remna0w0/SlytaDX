using RemnaBotService.Eternal_Dragon;
using TwitchLib.Client.Events;

namespace RemnaBotService.TwitchBot
{
    public class TwitchMessenger : BotMessenger
    {
        private readonly TwitchClientContainer _client;
        public TwitchMessenger(TwitchClientContainer client) => _client = client;

        public void SendGameMessage(GameMessage msg)
        {

            string output = !string.IsNullOrEmpty(msg.Text) ? msg.Text : $"{msg.Title}: {msg.Description}";
            _client.Say(output);
        }
        public async Task<string> GetUserInputAsync(string username)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(600));
            var tcs = new TaskCompletionSource<string>();
            async Task MessageReceivedHandler(object? sender, OnMessageReceivedArgs e)
            {
                if (e.ChatMessage.Username.Equals(username, StringComparison.OrdinalIgnoreCase))
                {
                    tcs.TrySetResult(e.ChatMessage.Message);
                }
            }

            _client.Client.OnMessageReceived += MessageReceivedHandler;




            try
            {
                using (cts.Token.Register(() => tcs.TrySetCanceled()))
                {
                    return await tcs.Task;
                }

            }
            catch (OperationCanceledException)
            {
                return null;
            }
            finally
            {
                _client.Client.OnMessageReceived -= MessageReceivedHandler;
            }
        }


        public async Task<int> GetUserSelectionAsync(string[] options, string username) // async Task<int>
        {
            _client.Say("Use !1, !2, etc. to select:");
            for (int i = 0; i < options.Length; i++)
            {
                _client.Say($"{i + 1}. {options[i]}");
            }

            while (true)
            {
                string input = await GetUserInputAsync(username);
                if (input == null)
                {
                    SendGameMessage(new GameMessage { Title = "Timeout", Description = "Too slow! Game over." });
                    return 66;
                }
                if (input.StartsWith("!") && input.Length > 1)
                {
                    string numberPart = input.Substring(1);
                    if (int.TryParse(numberPart, out int selection) && selection >= 1 && selection <= options.Length)
                    {
                        return selection - 1;
                    }
                }
                else
                {
                    _client.Say("Invalid selection. Please use %1, %2, etc.");
                }
            }
        }


    }
}
