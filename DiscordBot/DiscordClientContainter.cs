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

            _client.ButtonExecuted += OnButtonExecuted;

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

        private async Task OnButtonExecuted(SocketMessageComponent component)
        {
            if (component.User is SocketGuildUser user)
            {
                ulong roleID = 0;

                switch (component.Data.CustomId)
                {
                    case "role_he":
                        roleID = 1294408497919692923;
                        break;
                    case "role_she":
                        roleID = 1294408498368610324;
                        break;
                    case "role_they":
                        roleID = 1294408499157012531;
                        break;
                    case "role_ask":
                        roleID = 1294408500113440869;
                        break;
                    case "role_viewer":
                        roleID = 1294403762869371021;
                        break;
                    case "role_streamer":
                        roleID = 1294405854920966215;
                        break;
                    case "role_artist":
                        roleID = 1514412482566029342;
                        break;
                    case "role_fighter":
                        roleID = 1514411853466308669;
                        break;
                    default:
                        return;
                }

                var role = user.Guild.GetRole(roleID);
                if (role != null)
                {
                    if (user.Roles.Any(r => r.Id == roleID))
                    {
                        await user.RemoveRoleAsync(role);
                        await component.RespondAsync($"Removed the **{role.Name}** role!", ephemeral: true);
                    }
                    else
                    {
                        await user.AddRoleAsync(role);
                        await component.RespondAsync($"Added the **{role.Name}** role!", ephemeral: true);
                    }
                }
                else
                {
                    await component.RespondAsync("Error: Role not found. Contact Admin!", ephemeral: true);
                }
            }
        }


        public async Task OnMessageReceived(SocketMessage message)
        {
            Log($"{message.Author.Username}: {message.Content}");


            if (message.Author.IsBot) return;

            var channel = message.Channel;
            string username = message.Author.Username;
            string[] args = message.Content.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (args.Length == 0) return;

            string command = args[0];
 
            if (activeGames.TryGetValue(username, out var game))
            {
                if (message.Content.StartsWith("%"))
                {
                    if (game._messenger is DiscordMessenger dm)
                    {
                        dm.ReceiveMessage(message.Content);
                        return;
                    }
                }


            }
            

            switch (command)
            {
                case "%roles":
                    if (message.Author is SocketGuildUser guildUser)
                    {
                        if (!guildUser.GuildPermissions.Administrator)
                        {
                            Log("Non-admin attempted to spawn role buttons. Command ignored.");
                            return; 
                        }
                    }

                    var builder = new ComponentBuilder()
                        .WithButton("He/Him", "role_he", ButtonStyle.Primary, emote: new Emoji("\U0001F499"), row: 0)
                        .WithButton("She/Her", "role_she", ButtonStyle.Primary, emote: new Emoji("\U0001Fa77"), row: 0)
                        .WithButton("They/Them", "role_they", ButtonStyle.Primary, emote: new Emoji("\U0001F49C"), row: 0)
                        .WithButton("Ask", "role_ask", ButtonStyle.Primary, emote: new Emoji("\U0001F49A"), row: 0)

                        .WithButton("Viewers!", "role_viewer", ButtonStyle.Secondary, emote: new Emoji("\U0001F440"), row: 1)
                        .WithButton("Streamers!", "role_streamer", ButtonStyle.Secondary, emote: new Emoji("\U0001F3A5"), row: 1)
                        .WithButton("Artists!", "role_artist", ButtonStyle.Secondary, emote: new Emoji("\U0001F58C"), row: 1)
                        .WithButton("Fighters!", "role_fighter", ButtonStyle.Secondary, emote: new Emoji("\U0001F94A"), row: 1);

                    await channel.SendMessageAsync("Welcome! Click the buttons to add or remove roles:", components: builder.Build());
                    break;

                case "%ping":
                    await Say(channel, "Pong!");
                    break;

                case "%dragon":
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
                    break;
            }
        }
    }
}
