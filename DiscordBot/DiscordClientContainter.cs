using Discord;
using Discord.WebSocket;
using RemnaBotService.DiscordBot;

namespace RemnaBotService
{
    class DiscordClientContainter : DiscordLogger
    {
        private DiscordSocketClient _client;
        static string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        static string tokenPath = Path.Combine(baseDir, "SlytaBot Token.txt");
        public string DisToken = File.ReadAllText(tokenPath);
        private Dictionary<string, EternalDragon> activeGames = new Dictionary<string, EternalDragon>();

        public async Task Intialize()
        {
            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
            };

            _client = new DiscordSocketClient(config);
            _client.Log += (logMessage) =>
            {
                Log($"{logMessage.Severity}: {logMessage.Message}");
                return Task.CompletedTask;
            };
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
            Log($"I said: {text}");
        }
        public async Task SayEmbed(IMessageChannel channel, Discord.Embed build)
        {
            await channel.SendMessageAsync(embed: build);

            Log($"Sent Embed: {build.Title}");
        }


        public async Task OnMessageReceived(SocketMessage message)
        {
            Log($"{message.Author.Username}: {message.Content}");
            if (message.Author.IsBot) return;

            var channel = message.Channel;
            string username = message.Author.Username;

            if (message.Content == "%ping")
            {
                await Say(channel, "Pong!");
                return;
            }

            if (activeGames.TryGetValue(username, out var game))
            {

                if (message.Content.StartsWith("%"))
                {
                    if (game._messenger is DiscordMessenger dm)
                    {
                        dm.ReceiveMessage(message.Content);
                    }
                }

            }

            if (message.Content == "%dragon")
            {
                if (!activeGames.ContainsKey(username))
                {
                    var messenger = new DiscordMessenger(this, channel, username);
                    var newGame = new EternalDragon(messenger);
                    activeGames.Add(username, newGame);

                    _ = Task.Run(async () =>
                    {
                        try { await newGame.Dragon(username); }
                        finally { activeGames.Remove(username); }
                    });

                    await Say(channel, "Dragon game started!");
                }
            }
        }
    }
}
