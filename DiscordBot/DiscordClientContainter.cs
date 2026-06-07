using Discord;
using Discord.WebSocket;

namespace RemnaBotService
{
    class DiscordClientContainter : DiscordLogger
    {
        private DiscordSocketClient _client;
        static string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        static string tokenPath = Path.Combine(baseDir, "SlytaBot Token.txt");
        public string DisToken = File.ReadAllText(tokenPath);


        public async Task Intialize()
        {
            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
            };

            _client = new DiscordSocketClient(config);

            _client.Ready += OnReady;
            _client.MessageReceived += OnMessageReceived;

            await _client.LoginAsync(TokenType.Bot, DisToken);
            await _client.StartAsync();

        }

        private Task OnReady()
        {
            Log("I have connected!");
            return Task.CompletedTask;
        }

        public async Task<IMessageChannel> GetChannelAsync(ulong channelID)
        {
            var channel = await _client.GetChannelAsync(channelID);
            return channel as IMessageChannel;
        }

        public async Task Say(IMessageChannel channel, string text)
        {
            await channel.SendMessageAsync(text);
        }


        private async Task OnMessageReceived(SocketMessage message)
        {
            if (message.Author.IsBot) return;

            var channel = message.Channel;

            if (message.Content == "%ping")
            {
                await Say(channel, "Pong!");
            }
        }
    }
}
