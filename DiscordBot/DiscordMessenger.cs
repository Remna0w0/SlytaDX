using Discord;
using Discord.WebSocket;
using RemnaBotService.Eternal_Dragon;
using System.Threading.Tasks;

namespace RemnaBotService.DiscordBot
{
    internal class DiscordMessenger : BotMessenger
    {
        private readonly DiscordClientContainter _client;
        private readonly IMessageChannel _channel;
        private readonly string _username;
        private TaskCompletionSource<string> _tcs;
        private TaskCompletionSource<string> _buttonTcs;
        public DiscordMessenger(DiscordClientContainter client, IMessageChannel channel, string username)
        {
            _client = client;
            _channel = channel;
            _username = username;
        }


        public void SendMessage(string message) => _client.Say(_channel, message);
        public void SendGameMessage(GameMessage msg) => SendGameMessageWithComponents(msg, null);

        private void SendGameMessageWithComponents(GameMessage msg, MessageComponent components = null)
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

                _channel.SendMessageAsync(embed: builder.Build(), components: components);
            }
            else
            {
                _channel.SendMessageAsync(text: msg.Text, components: components);
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

        public async Task<int> GetUserSelectionAsync(string[] options, string username)
        {
            var componentBuilder = new ComponentBuilder();

            for (int i = 0; i < options.Length; i++)
            {
                componentBuilder.WithButton(options[i], $"btn_{i}_{username}", ButtonStyle.Primary);
            }

            SendGameMessageWithComponents(new GameMessage
            {
                Title = "Action Select",
                Description = "Click one of the tactical buttons below to execute your choice:"
            }, componentBuilder.Build());

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(600));
            _buttonTcs = new TaskCompletionSource<string>();

            using (cts.Token.Register(() => _buttonTcs.TrySetCanceled()))
            {
                try
                {
                    _client.GetInteractionClient().ButtonExecuted += HandleButtonInteraction;
                    string customId = await _buttonTcs.Task;

                    string[] parts = customId.Split('_');
                    return int.Parse(parts[1]);
                }
                catch (OperationCanceledException)
                {
                    SendGameMessage(new GameMessage { Title = "Timeout", Description = "Too slow! Game over." });
                    return 66;
                }
                finally
                {
                    _client.GetInteractionClient().ButtonExecuted -= HandleButtonInteraction;
                }
            }
        }

        private Task HandleButtonInteraction(SocketMessageComponent interaction)
        {
            if (interaction.Data.CustomId.StartsWith("btn_"))
            {
                string[] parts = interaction.Data.CustomId.Split('_');
                string targetUser = parts[2];

                // Stop other users from messing with someone else's instance menu
                if (interaction.User.Username != targetUser)
                {
                    interaction.RespondAsync("This is not your battle menu!", ephemeral: true);
                    return Task.CompletedTask;
                }

                var disabledComponents = new ComponentBuilder();
                foreach (var component in interaction.Message.Components)
                {
                    if (component is ActionRowComponent row)
                    {
                        foreach (var button in row.Components.OfType<ButtonComponent>())
                        {
                            disabledComponents.WithButton(
                                label: button.Label,
                                customId: button.CustomId,
                                style: button.Style,
                                disabled: true);
                        }
                    }
                }

                interaction.UpdateAsync(msg => msg.Components = disabledComponents.Build());

                _buttonTcs?.TrySetResult(interaction.Data.CustomId);
            }
            return Task.CompletedTask;
        }

        public void ReceiveMessage(string content)
        {
            _tcs?.TrySetResult(content);
        }


    }
}
