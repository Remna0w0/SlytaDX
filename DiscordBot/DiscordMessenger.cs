using Discord;
using Discord.WebSocket;
using RemnaBotService.Eternal_Dragon;
using System.Threading.Tasks;

namespace RemnaBotService.DiscordBot
{
    internal class DiscordMessenger : BotMessenger
    {
        /// <summary>
        /// The messengers are mainly for sending complex messages to their associated platforms without having to define it within the Client Containers themselves. 
        /// It also is used for receiving input from users out side the scope of the original command in the Client Container.
        /// They are primarily used for the Dragon game, but could easily be used for other purposes.
        /// The discord messenger focuses the most on sending embeds an d recieving button inputs, but can be used to send and receive standard text messages as well.
        /// </summary>
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
                // changes the banner color of the embed if the game is over
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
                // custom button ID so we can define how the messenger responds to interaction, important to ignore other users
                componentBuilder.WithButton(options[i], $"btn_{i}_{username}", ButtonStyle.Primary);
            }

            SendGameMessageWithComponents(new GameMessage
            {
                Title = "Action Select",
                Description = "Click one of the tactical buttons below to execute your choice:"
            }, componentBuilder.Build());

            // timeout to not lockout threads 
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(600));
            _buttonTcs = new TaskCompletionSource<string>();

            using (cts.Token.Register(() => _buttonTcs.TrySetCanceled()))
            {
                try
                {
                    _client.GetInteractionClient().ButtonExecuted += HandleButtonInteraction;
                    string customId = await _buttonTcs.Task;

                    string[] parts = customId.Split('_');
                    // part 1 gives the index of the option chosen, and that index has a meaning in game 
                    return int.Parse(parts[1]);
                }
                catch (OperationCanceledException)
                {
                    SendGameMessage(new GameMessage { Title = "Timeout", Description = "Too slow! Game over." });
                    // designated game kill switch
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
            // only handles buttons that use the custom ID
            if (interaction.Data.CustomId.StartsWith("btn_"))
            {
                string[] parts = interaction.Data.CustomId.Split('_');
                // part 2 contains the username used to validate the button press, shown below
                string targetUser = parts[2];

                // Stop other users from messing with someone else's instance menu
                if (interaction.User.Username != targetUser)
                {
                    interaction.RespondAsync("This is not your battle menu!", ephemeral: true);
                    return Task.CompletedTask;
                }

                // if it is the correct user, all the options need to stop being clickable
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

                // the button task can only be completed if this methods accepts the interaction from the right user
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
