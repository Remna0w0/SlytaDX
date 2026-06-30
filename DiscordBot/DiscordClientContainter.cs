using Dapper;
using Discord;
using Discord.WebSocket;
using RemnaBotService.DiscordBot;
using static RemnaBotService.DiscordBot.DiscordCommandHandler;

namespace RemnaBotService.DiscordBot
{
    public class DiscordClientContainter : DiscordLogger, IDiscordClientWrapper
    {
        /// <summary>
        /// Contains all tasks, commands, and means of intialization for the Discord Bot
        /// </summary>

        private DiscordSocketClient _client;
        private DatabaseService _databaseService = new DatabaseService();
        static string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        static string congifDir = Path.Combine(baseDir, "Config");
        static string tokenPath = Path.Combine(congifDir, "SlytaBot Token.txt");
        public string DisToken = File.ReadAllText(tokenPath);
        public Dictionary<string, EternalDragon> activeGames = new Dictionary<string, EternalDragon>();
        DiscordCommandHandler commander = new DiscordCommandHandler();

        public bool IsGameActive(string username) => activeGames.ContainsKey(username);
        
        public async Task Intialize()
        {
            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent | GatewayIntents.GuildMembers,
                AlwaysDownloadUsers = true
            };

            _client = new DiscordSocketClient(config);

            // Log files are local, so we deliver API generated logs here
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
        // Full resync on Ready in case there were new users while offline
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

        // for message tasks that dont come from commands, so the bot knows where to send them
        public async Task<IMessageChannel> GetChannelAsync(ulong channelID)
        {
            var channel = await _client.GetChannelAsync(channelID);
            return channel as IMessageChannel;
        }

        public async Task Say(IMessageChannel channel, string text)
        {
            await channel.SendMessageAsync(text);
        }
        public async Task SayEmbed(IMessageChannel channel, Discord.Embed build)
        {
            await channel.SendMessageAsync(embed: build);

            Log($"Sent Embed: {build.Title}");
        }

        private async Task OnButtonExecuted(SocketMessageComponent component)
        {
            // this is currently only used by the onboarder, but could be used for other perpetual buttons
            if (component.User is SocketGuildUser user)
            {
                ulong roleID = 0;

                // returns for the role onboarder, may consolidate into a method later
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

            try
            {
                // ignore all bots
                if (message.Author.IsBot) return;




                if (message.Author is SocketGuildUser user)
                {
                    string userID = message.Author.Id.ToString();
                    bool isMod = user.GuildPermissions.Administrator || user.GuildPermissions.ManageMessages;

                    using var db = _databaseService.GetConnection();

                    // if users are active, this ensures database doesnt miss any user for too long, even if they are missed by the intial sync
                    string updateSql = "UPDATE ServerMembers SET IsModerator = @isMod, Message_Count = Message_Count + 1 WHERE UserID = @id";

                    int rowsAffected = db.Execute(updateSql, new
                    {
                        isMod = isMod ? 1 : 0,
                        id = userID
                    });
                    if (rowsAffected == 0)
                    {
                        string insertSql = @"INSERT INTO ServerMembers (UserID, Username, IsModerator, Message_Count)
                                         VALUES (@id, @name, @isMod, 1)";

                        db.Execute(insertSql, new
                        {
                            id = userID,
                            name = user.Username,
                            isMod = isMod ? 1 : 0
                        });
                        Log($"Registered missed user: {user.Username}");
                    }
                }

                var channel = message.Channel;
                string username = message.Author.Username;
                string[] args = message.Content.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (args.Length == 0) return;

                string command = args[0];

                // for dragon game. Sense its moving over to buttons, this may no longer be need soon
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

                if (message is not SocketUserMessage userMessage) return;


                switch (command)
                {
                    // this will only be ran once, or if the roles have changed and new buttons are added/edited
                    case "%roles":
                        commander.RoleSpawnCommand(this, userMessage);
                        break;

                    case "%ping":
                        commander.PingCommand(this, userMessage);
                        break;

                    // cooldown does not affect actual game content, only the command itself
                    case "%dragon":
                        commander.DragonCommand(this, userMessage, username);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log($"MessageRecieved error: {ex.Message}");
            }

        }

        public void StartDragonGame(string username, IMessageChannel channel)
        {
            var messenger = new DiscordMessenger(this, channel, username);
            var newGame = new EternalDragon(messenger);
            activeGames.Add(username, newGame);

            _ = Task.Run(async () =>
            {
                try
                {
                    await newGame.Dragon(username);
                }
                finally
                {
                    activeGames.Remove(username);
                }
            });
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
                    }, transaction);

                    totalSynced++;
                }

                transaction.Commit();
                Log($"Server member sync complete! Checked and saved {totalSynced} members.");
            }
            catch (Exception ex)
            {
                // should add a way to retry the sync 
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
            Log($"Registered new user: {user.Username}");
        }
        
        // ensures that the database updates as the user changes username/roles/etc, as userID remains the same
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

        public DiscordSocketClient GetInteractionClient() => _client;

        
        public void LogCommand(string userId, string commandName)
        {
            try
            {
                using var db = _databaseService.GetConnection();

                string sql = @"INSERT INTO CommandLog (UserID, CommandName, Timestamp) 
                   VALUES (@userId, @commandName, CURRENT_TIMESTAMP)";

                db.Execute(sql, new { userId, commandName });
            }
            catch (Exception ex)
            {
                Log($"[DATABASE ERROR] Could not log command {commandName}: {ex.Message}");
            }

        }
    }
}
