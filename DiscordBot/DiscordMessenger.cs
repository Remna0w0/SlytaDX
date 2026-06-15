using Discord;
using RemnaBotService.Eternal_Dragon;
using System.Text;

namespace RemnaBotService.DiscordBot
{
    internal class DiscordMessenger : BotMessenger
    {
        private readonly DiscordClientContainter _client;
        private readonly IMessageChannel _channel;
        private readonly string _username;
        private TaskCompletionSource<string> _tcs;
        public DiscordMessenger(DiscordClientContainter client, IMessageChannel channel, string username)
        {
            _client = client;
            _channel = channel;
            _username = username;
        }


        public void SendMessage(string message) => _client.Say(_channel, message);
        public void SendGameMessage(GameMessage msg)
        {
            if (msg.IsEmbed)
            {
                var color = Color.DarkBlue;
                if (msg.Description.Contains("Victory"))
                    color = Color.Green;
                else if (msg.Description.Contains("Defeat"))
                    color = Color.Red;
                var builder = new EmbedBuilder()
                    .WithTitle(msg.Title)
                    .WithDescription(msg.Description)
                    .WithColor(color);
                _client.SayEmbed(_channel, builder.Build());
            }
            else
            {
                _client.Say(_channel, msg.Text);
            }
        }

        public async Task<string> GetUserInputAsync(string username)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(600));
            _tcs = new TaskCompletionSource<string>();

            using (cts.Token.Register(() => _tcs.TrySetCanceled()))
            {
                try
                {
                    return await _tcs.Task;
                }
                catch (OperationCanceledException)
                {
                    return null;
                }
            }
        }

        public async Task<int> GetUserSelectionAsync(string[] options, string username) // async Task<int>
        {
            var sb = new StringBuilder();
            sb.AppendLine("Use %1, %2, etc. to select:");
            for (int i = 0; i < options.Length; i++)
            {
                sb.AppendLine($"{i + 1}. {options[i]}");
            }

            SendGameMessage(new GameMessage
            {
                Title = "Choices",
                Description = sb.ToString()
            });

            while (true)
            {

                string input = await GetUserInputAsync(username);
                if (input == null)
                {
                    SendGameMessage(new GameMessage { Title = "Timeout", Description = "Too slow! Game over." });
                    return 66;
                }
                if (input.StartsWith("%") && input.Length > 1)
                {
                    string numberPart = input.Substring(1);
                    if (int.TryParse(numberPart, out int selection) && selection >= 1 && selection <= options.Length)
                    {
                        return selection - 1;
                    }
                }
                else
                {
                    SendGameMessage(new GameMessage
                    {
                        Title = "Invalid Input",
                        Description = "Please use %1, %2, etc. to select your choice."
                    });
                }
            }
        }

        public void ReceiveMessage(string content)
        {
            _tcs?.TrySetResult(content);
        }


    }
}
