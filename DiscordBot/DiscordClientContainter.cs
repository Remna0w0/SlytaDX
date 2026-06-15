using Discord;
using Discord.WebSocket;
using RemnaBotService.DiscordBot;
using Dapper;
using Microsoft.Data.Sqlite;

namespace RemnaBotService
{
    class DiscordClientContainter : DiscordLogger
    {
        
        private DiscordSocketClient _client;
        private DatabaseService _databaseService = new DatabaseService();
        static string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        static string tokenPath = Path.Combine(baseDir, "SlytaBot Token.txt");
        public string DisToken = File.ReadAllText(tokenPath);
        private Dictionary<string, EternalDragon> activeGames = new Dictionary<string, EternalDragon>();

        public async Task Intialize()
        {
            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent | GatewayIntents.GuildMembers,
                AlwaysDownloadUsers = true
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
            _client.UserJoined += OnUserJoined;
            _client.GuildMemberUpdated += OnGuildMemberUpdated;

            await _client.LoginAsync(TokenType.Bot, DisToken);
            await _client.StartAsync();

        }

        private async Task OnReady()
        {
            Log("I have connected!");
            foreach (var guild in _client.Guilds)
            {
                await guild.DownloadUsersAsync();
                await SyncServerMembers(guild);
                Log($"Synced members for guild: {guild.Name}");
            }
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

        public async Task SyncServerMembers(SocketGuild guild)
        {
            Log("Beginning server member sync....");

            ulong modRoleId = 1294406629747462194;

            using var db = _databaseService.GetConnection();
            db.Open();

            using var transaction = db.BeginTransaction();

            try
            {
                int totalSynced = 0;    
                string sql = @"INSERT OR IGNORE INTO ServerMembers (UserID, Username, JoinDate, IsModerator)
                               VALUES (@id, @name, @joinDate, @isMod)";

                foreach (var user in guild.Users)
                {
                    bool isMod = user.GuildPermissions.ManageMessages || user.GuildPermissions.Administrator;


                    db.Execute(sql, new
                    {
                        id = user.Id.ToString(),
                        name = user.Username,
                        joinDate = user.JoinedAt?.DateTime ?? DateTime.Now,
                        isMod = isMod ? 1 : 0
                    },   transaction);

                    totalSynced++;
                }
                
                transaction.Commit();
                Log($"Server member sync complete! Checked and saved {totalSynced} members.");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Log($"Server sync error: {ex.Message}");
            }
        }

        private async Task OnUserJoined(SocketGuildUser user)
        {
            string sql = @"INSERT OR IGNORE INTO ServerMembers (UserID, Username, JoinDate)
                               VALUES (@id, @name, @joinDate)";

            using var db = _databaseService.GetConnection();
            db.Execute(sql, new
            {
                id = user.Id.ToString(),
                name = user.Username,
                joinDate = DateTime.Now
            });
        }

        private async Task OnGuildMemberUpdated(Cacheable<SocketGuildUser, ulong> before, SocketGuildUser after)
        {
            var beforeUser = await before.GetOrDownloadAsync();

            bool nameChanged = beforeUser != null && beforeUser.Username != after.Username;


            bool beforeMod = beforeUser != null && (beforeUser.GuildPermissions.ManageMessages || beforeUser.GuildPermissions.Administrator);
            bool afterMod = after.GuildPermissions.ManageMessages || after.GuildPermissions.Administrator;


            using var db = _databaseService.GetConnection();


            if (nameChanged)
            {
                string sqlName = "UPDATE ServerMembers SET Username = @name WHERE UserID = @id";
                await db.ExecuteAsync(sqlName, new { name = after.Username, id = after.Id.ToString() });
                Log($"User {after.Id} updated username to: {after.Username}");
            }

            if (beforeUser == null || beforeMod != afterMod)
            {
                string sqlMod = "UPDATE ServerMembers SET IsModerator = @isMod WHERE UserID = @id";
                await db.ExecuteAsync(sqlMod, new { isMod = afterMod ? 1 : 0, id = after.Id.ToString() });
                Log($"User {after.Username} moderation status updated to: {afterMod}");
            }
        }
    }
}
