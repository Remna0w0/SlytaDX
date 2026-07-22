using Dapper;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.Auth;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Interfaces;
using WatsonWebsocket;
using static RemnaBotService.TwitchBot.TwitchCommandHandler;

namespace RemnaBotService.TwitchBot;




public class TwitchClientContainer : TwitchLogger, ITwitchClientWrapper
{
    /// <summary>
    /// Contains all tasks, commands, and means of intialization for the Twitch Bot
    /// </summary>
    
    static string baseDir = AppDomain.CurrentDomain.BaseDirectory;
    public TwitchClient Client = new TwitchClient();
    public ConnectionCredentials Credentials;
    public TwitchAPI API;
    DatabaseService _databaseService = new DatabaseService();
    static string configDir = Path.Combine(baseDir, "Config");
    static string miscDir = Path.Combine(baseDir, "Misc");
    static string tourneyPath = Path.Combine(miscDir, "tourney link.txt");
    static string arenaIDPath = Path.Combine(miscDir, "arena ID.txt");
    static string secretPath = Path.Combine(configDir, "twitch secret.txt");
    static string clientIdPath = Path.Combine(configDir, "bot client ID.txt");
    static string refreshPath = Path.Combine(configDir, "Refresh Token.txt");
    static string accessPath = Path.Combine(configDir, "Access Token.txt");
    private string streamName = "remnapi";
    public string BotUsername = "SlytaBot";
    public string streamerID;
    public string Secret = File.ReadAllText(secretPath);
    public string ClientID = File.ReadAllText(clientIdPath);
    public string RefreshToken = File.ReadAllText(refreshPath);
    private System.Timers.Timer liveCheckTimer;
    private bool isLive = false;
    public bool FileExists(string path) => File.Exists(path);
    public string ReadFileText(string path) => File.ReadAllText(path);
    public void WriteFileText(string path, string text) => File.WriteAllText(path, text);
    // currentlyRefrshing stops the program from constantly trying to refresh itself, preventing crashes
    private bool currentlyRefreshing = false;
    // To ensure nothing tries to write to a file at the same time as something else
    private static readonly SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);
    private DateTime _lastRefreshAttempt = DateTime.MinValue;
    public readonly TimeSpan _cooldown = TimeSpan.FromMinutes(2);
    public event EventHandler<string> OnStreamGoLive;
    public TwitchCommandHandler commander = new TwitchCommandHandler();
    WatsonWsServer wsServer;



    private TaskCompletionSource initializationCompletionSource = new TaskCompletionSource();
    public Task InitializationComplete() => initializationCompletionSource.Task;



    public async Task Initialize()
    {
        API = new TwitchAPI();
        API.Settings.Secret = Secret;
        API.Settings.ClientId = ClientID;
        ;

        await RefreshMyToken();

        if (!File.Exists(accessPath) || string.IsNullOrEmpty(API.Settings.AccessToken))
        {
            Log("CRITICAL: Access Token file missing or Invalid. Bot cannot start. Check root directory");
            return;
        }

        wsServer = new WatsonWsServer("*", 8085, false);
        wsServer.MessageReceived += OnWSSocketMessageReceived;
        wsServer.ClientConnected += (sender, args) =>
        {
            string origin = args.HttpRequest.Headers["Origin"];

            Log($"[WS CONNECT] New connection attempt from Origin: {origin}");

            if (origin != "https://dashboard.remnapi.net" && origin != "http://localhost:3000")
            {
                Log($"[WS SECURITY] Disconnecting unauthorized Origin: {origin}");
            }
        };

        wsServer.Start();
        Credentials = new ConnectionCredentials(BotUsername, $"oauth:{API.Settings.AccessToken}");
        Client.OnConnected += OnConnected;
        Client.OnJoinedChannel += JoinedChannel;
        Client.OnMessageReceived += MessageReceived;
        Client.OnMessageCleared += OnMessageCleared;
        Client.OnUserTimedout += async (s, e) => await HandleUserPurge(e.UserTimeout.Username);
        Client.OnUserBanned += async (s, e) => await HandleUserPurge(e.UserBan.Username);
        Client.OnChatCommandReceived += ChatCommand;
        Client.Initialize(Credentials);
        await Client.ConnectAsync();
        SetupLiveCheck();
        await ValidateTokenScopes();
        await GetStreamerID();
        await SyncFollowers();


        Client.OnDisconnected += async (sender, e) =>
        {
            if (currentlyRefreshing)
            {
                Log("Reconnector silenced.");
                return;
            }
            else
            {
                Log("Disconnected! Attempting to reconnect...");
                await Client.ConnectAsync();
                return;
            }
        };


        initializationCompletionSource.SetResult(); // Signal initialization complete
    }

    

    // Prints a list of all current allowed scopes so you can be sure your tokens are correct
    public async Task ValidateTokenScopes()
    {
        try
        {
            var validation = await API.Auth.ValidateAccessTokenAsync(File.ReadAllText(accessPath));

            Log("--- Token Scope Validation ---");
            Log($"Client ID: {validation.ClientId}");
            Log($"Scopes: {string.Join(", ", validation.Scopes)}");
            Log("------------------------------");
        }
        catch (Exception ex)
        {
            Log($"Failed to validate token: {ex.Message}");
        }
    }

    private void SetupLiveCheck()
    {
        {
            CheckIfLive();
            liveCheckTimer = new System.Timers.Timer(60000);
            liveCheckTimer.Elapsed += async (s, e) => await CheckIfLive();
            liveCheckTimer.AutoReset = true;
            liveCheckTimer.Enabled = true;
        }
    }

    // When the stream goes live, we want the discord side to send an announcement ASAP
    private async Task CheckIfLive()
    {
        if (!Client.IsConnected)
        {
            Log("Client offline, LIVE check postponed. Rechecking in 60 seconds...");
            return;
        }
        try
        {
            var response = await API.Helix.Streams.GetStreamsAsync(userLogins: new List<string> { streamName });

            bool live = response.Streams.Length > 0;

            var payload = new
            {
                Type = "StreamLiveStatus",
                LiveStatus = isLive
            };

            if (live && !isLive)
            {
                isLive = true;
                OnStreamGoLive?.Invoke(this, "@everyone Remna is LIVE! Come thru! https://www.twitch.tv/remnapi");
                Log("Streamer is LIVE! Rechecking in 60 seconds...");

                payload = new
                {
                    Type = "StreamLiveStatus",
                    LiveStatus = isLive
                };

                string jsonString = JsonSerializer.Serialize(payload);

                foreach (var client in wsServer.ListClients())
                {
                    await wsServer.SendAsync(client.Guid, jsonString);
                }
                Log("[Websocket Sent] Updated dashboard Live status to ONLINE.");
            }
            else if (!live)
            {
                isLive = false;
                Log("Streamer is OFFLINE! Rechecking in 60 seconds...");

                payload = new
                {
                    Type = "StreamLiveStatus",
                    LiveStatus = isLive
                };
                string jsonString = JsonSerializer.Serialize(payload);

                foreach (var client in wsServer.ListClients())
                {
                    await wsServer.SendAsync(client.Guid, jsonString);
                }
                Log("[Websocket Sent] Updated dashboard Live status to OFFLINE.");
            }
            else if (live && isLive)
            {
                string jsonString = JsonSerializer.Serialize(payload);

                foreach (var client in wsServer.ListClients())
                {
                    await wsServer.SendAsync(client.Guid, jsonString);
                }
                Log("[Websocket Sent] Updated dashboard Live status to ONLINE.");
            }
        }
        // This method also serves as a status check for the bot, updating its tokens when expired 
        catch (Exception ex) when (ex.Message.Contains("Invalid OAuth") || ex.Message.Contains("bad credentials"))
        {
            Log("Tokens expired! Refreshing tokens...");
            await RefreshMyToken();

        }
        catch (Exception ex)
        {
            Log($"General Error: {ex.Message}");
        }





    }

    // We need the streamer ID for things like the followage command
    public async Task GetStreamerID()
    {
        var userResponse = await API.Helix.Users.GetUsersAsync(logins: new System.Collections.Generic.List<string> { streamName });

        if (userResponse.Users.Length == 0)
        {
            Log("Broadcaster Id error, User not found");
            return;
        }
        streamerID = userResponse.Users[0].Id;
        Log("Broadcaster ID fetched.");
    }


    public async Task RefreshMyToken()
    {
        // prevents panic refrshing
        if (DateTime.Now - _lastRefreshAttempt < _cooldown)
        {
            Log("Refresh cooldown active. Skipping request.");
            return;
        }

        Log("Refreshing Token...");

        try
        {
            RefreshResponse refreshResult = await API.Auth.RefreshAuthTokenAsync(RefreshToken, Secret, ClientID);

            _lastRefreshAttempt = DateTime.Now;

            // Only proceed if the tokens are genuine 
            if (refreshResult != null && !string.IsNullOrEmpty(refreshResult.AccessToken) && !string.IsNullOrEmpty(refreshResult.RefreshToken))
            {
                API.Settings.AccessToken = refreshResult.AccessToken;

                if (Client.IsConnected)
                {
                    Log("Updating Chat Client credentials...");

                    currentlyRefreshing = true;

                    await Client.DisconnectAsync();

                    await Task.Delay(500);

                    // Update old creds before writing to the file
                    Client.SetConnectionCredentials(new ConnectionCredentials(BotUsername, $"oauth:{refreshResult.AccessToken}"));
                    await Client.ConnectAsync();
                    currentlyRefreshing = false;
                }

                await _fileLock.WaitAsync();
                try
                {
                    RefreshToken = refreshResult.RefreshToken;

                    File.WriteAllText(accessPath, refreshResult.AccessToken);
                    File.WriteAllText(refreshPath, RefreshToken);
                    Log("Tokens refreshed and API updated!");
                    Log($"New Access Token: {API.Settings.AccessToken}");
                }
                finally
                {
                    _fileLock.Release();
                }
            }
            else
            {
                Log("Error: Received invalid or null token. Skipping token write. Cooldown Started.");
            }
        }
        catch (Exception ex)
        {
            Log($"Critical Error! {ex.Message}");
        }
    }

    // Twitch's default command prefix is "!"
    private async Task ChatCommand(object? sender, OnChatCommandReceivedArgs e)
    {
        string username = e.ChatMessage.Username;
        try
        {
            string command = e.Command.Name.ToLower();

            switch (command)
            {
                case "beep":
                    commander.BeepCommand(this, e.ChatMessage.UserId);
                    break;

                    // multiple names for the same command to accomodate... ignorant users

                case "join":
                case "id":
                case "arena":
                    await commander.ArenaCommand(this, e.ChatMessage.UserId, arenaIDPath);
                    break;

                case "server":
                case "discord":
                    commander.DiscordCommand(this, e.ChatMessage.UserId);
                    break;

                case "bracket":
                case "tourney":
                    await commander.TourneyCommand(this, e.ChatMessage.UserId, tourneyPath);
                    break;

                case "lurk":
                    commander.LurkCommand(this, e.ChatMessage.UserId);
                    break;
                

                case "setid":
                    // The arena ID can only be set by mods or the streamer
                    // THIS IS IMPORTANT. Allowing this to be called by viewers is eseentially allowing anyone to write to a file on the machine running the program. Be cautious. 
                    if (e.ChatMessage.UserDetail.IsModerator || e.ChatMessage.UserDetail.IsVip || e.ChatMessage.IsBroadcaster)
                    {
                        if (e.Command.ArgumentsAsList.Count > 0)
                        {
                            await commander.SetIDCommand(this, e.ChatMessage.UserId, e.Command.ArgumentsAsList[0], arenaIDPath);
                        }
                        else
                        {
                            Say($"{e.ChatMessage.Username}, please provide an ID!");
                        }
                    }
                    break;

                case "openarena":
                    // This invokes the event which Discord uses to send an announcment
                    // IT IS IMPORTANT TO KEEP THIS COMMAND TO THE STREAMER ONLY. 
                    if (e.ChatMessage.IsBroadcaster)
                    {
                        commander.OpenArenaCommand(this, e.ChatMessage.UserId, arenaIDPath);
                    }
                    break;

                    // Gets the time a given user has been following the channel
                case "followage":
                    if (string.IsNullOrEmpty(streamerID))
                    {
                        Log("Error: streamerID is null! Retrying fetch...");
                        await GetStreamerID();
                        if (string.IsNullOrEmpty(streamerID))
                        {
                            Say("Sorry, I don't know who the streamer is yet. Try again in a moment.");
                            break;
                        }
                    }
                    await commander.FollowageCommand(this, e.ChatMessage.UserId, username, streamerID);
                    break;

            }
        }

        catch (Exception ex)
        {
            Log($"Command Error: {ex.Message}");
            Log(ex.ToString());
            Say("Error running command! Check logs!");
        }
    }

    public async Task<DateTime?> GetFollowDateAsync(string streamerId, string viewerId)
    {
        // The configuration logic you had inside the handler moves into the boundary container!
        API.Settings.ClientId = ClientID;
        API.Settings.AccessToken = File.ReadAllText(accessPath);

        var followsResponse = await API.Helix.Channels.GetChannelFollowersAsync(
            broadcasterId: streamerId,
            userId: viewerId
        );

        if (followsResponse.Data == null || followsResponse.Data.Length == 0)
        {
            return null;
        }

        return DateTime.Parse(followsResponse.Data[0].FollowedAt);
    }

    // Updates the follower database
    public async Task SyncFollowers()
    {
        Log("Starting full follower sync, please wait...");


        using var db = _databaseService.GetConnection();
        db.Open();
        using var transaction = db.BeginTransaction();

        try
        {
            int totalSynced = 0;
            string followCursor = null;

            do
            {
                // Twitch provides the list followers by pages of 100, so we have to flip through them
                var followList = await API.Helix.Channels.GetChannelFollowersAsync(streamerID, after: followCursor, first: 100);



                foreach (var follower in followList.Data)
                {

                    string sql = @"INSERT OR IGNORE INTO Viewers (UserID, Username, FollowDate, IsModerator)
                               VALUES (@id, @name, @joinDate, 0)";

                    db.Execute(sql, new
                    {
                        id = follower.UserId.ToString(),
                        name = follower.UserName,
                        joinDate = DateTime.Parse(follower.FollowedAt)
                    }, transaction);

                    totalSynced++;
                }
                followCursor = followList.Pagination?.Cursor;
            } while (followCursor != null);

            transaction.Commit();
            Log($"Sync Complete! {totalSynced} followers checked and saved.");
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            Log($"Follower Sync error: {ex.Message}");
        }
    }

    public void Say(string message)
    {
        if (!Client.IsConnected)
        {
            Log("Client not connected, message not sent.");
            return;
        }
        Client.SendMessageAsync(Client.JoinedChannels[0], message);
        Log($"I Said: {message}");
    }


    int followListTick = 0;
    private async Task MessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        followListTick++;
        Log($"Message from {e.ChatMessage.Username}:{e.ChatMessage.Message}");
        bool isMod = e.ChatMessage.UserDetail.IsModerator;
        string userID = e.ChatMessage.UserId.ToString();

        using var db = _databaseService.GetConnection();
        string checkSql = "SELECT COUNT(1) FROM Viewers WHERE UserID = @id";
        int exists = db.ExecuteScalar<int>(checkSql, new { id = userID });

        // Updates the users database entry every time they send a message, ensuring their information is consistent as long as they are chatting
        // Also catches any new followers not caught in the database, as long as they are chatting
        if (!e.ChatMessage.IsBroadcaster)
        {
            if (exists == 0)
            {
                var followCheck = await API.Helix.Channels.GetChannelFollowersAsync(streamerID, userId: userID);
                DateTime followDate = DateTime.Now;
                if (followCheck.Data.Length > 0)
                {
                    followDate = DateTime.Parse(followCheck.Data[0].FollowedAt);
                }
                string insertSql = @"INSERT OR IGNORE INTO Viewers (UserID, Username, FollowDate, IsModerator, Message_Count)
                               VALUES (@id, @name, @joinDate, @isMod, @msgCount)";
                db.Execute(insertSql, new
                {
                    id = userID,
                    name = e.ChatMessage.Username,
                    joinDate = followDate,
                    isMod = isMod ? 1 : 0,
                    msgCount = 1
                });
                Log($"New follower {e.ChatMessage.Username} added to the database!");
            }
            else
            {
                string updateSql = "UPDATE Viewers SET IsModerator = @isMod, Message_Count = Message_Count + 1 WHERE UserID = @id";
                db.Execute(updateSql, new
                {
                    isMod = isMod ? 1 : 0,
                    id = userID
                });
            }
        }

        if (e.ChatMessage.Username.ToLower() == "slytabot")
        {
            return; 
        }

        string processedMessage = e.ChatMessage.Message;

        if (e.ChatMessage.EmoteSet != null && e.ChatMessage.EmoteSet.Emotes.Count > 0)
        {
            var sortedEmotes = e.ChatMessage.EmoteSet.Emotes
                .OrderByDescending(x => x.StartIndex);

            foreach (var emote in sortedEmotes)
            {
                processedMessage = processedMessage.Remove(emote.StartIndex, (emote.EndIndex - emote.StartIndex) + 1)
                                                   .Insert(emote.StartIndex, $"{{EMOTE:{emote.Id}}}");
            }
        }

        var payload = new
        {
            Type = "ChatMessage",
            MessageID = e.ChatMessage.Id,
            Username = e.ChatMessage.Username,
            UserColor = string.IsNullOrEmpty(e.ChatMessage.HexColor) ? "#FFB080" : e.ChatMessage.HexColor,
            Message = processedMessage,
            IsMod = e.ChatMessage.UserDetail.IsModerator,
            IsVip = e.ChatMessage.UserDetail.IsVip,
            IsBroadcaster = e.ChatMessage.IsBroadcaster
        };

        string jsonString = JsonSerializer.Serialize(payload);

        foreach (var client in wsServer.ListClients())
        {
            await wsServer.SendAsync(client.Guid, jsonString);
        }
        if (followListTick >= 10)
        {
            foreach (var client in wsServer.ListClients())
            {
                await SendFollowersToClient(client.Guid);
            }
            followListTick = 0;
        }
    }

    private async void OnWSSocketMessageReceived(object sender, MessageReceivedEventArgs e)
    {
        try
        {
            string jsonString = System.Text.Encoding.UTF8.GetString(e.Data);
            Log($"[Websocket Received]: {jsonString}");

            using JsonDocument doc = JsonDocument.Parse(jsonString);
            JsonElement root = doc.RootElement;

            if (root.TryGetProperty("Action", out JsonElement actionProp))
            {
                string action = actionProp.GetString();

                if (action == "OpenArena")
                {
                    Log("[DASHBOARD ACTION] Remote execution of OpenArena triggered.");

                    commander.OpenArenaCommand(this, "DashboardAdmin", arenaIDPath);
                }

                else if (action == "GetFollowers")
                {
                    Log("[DASHBOARD ACTION] Client requested follower list. Querying database...");
                    await SendFollowersToClient(e.Client.Guid);
                }

                else if (action == "UpdateArenaCode")
                {
                    if (root.TryGetProperty("Code", out JsonElement codeProp))
                    {
                        string newCode = codeProp.GetString();
                        Log($"[DASHBOARD ACTION] Client has pushed new Arena ID: {newCode} ");

                        commander.SetIDCommand(this, "REMOTECLIENT", newCode, arenaIDPath);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log($"[Websocket Inbound Error]; {ex.Message}");
        }
    }

    private async Task OnMessageCleared(object sender, OnMessageClearedArgs e)
    {
        try
        {
            var deletePayload = new
            {
                Type = "DeleteMessage",
                TargetMessageId = e.TargetMessageId 
            };

            string json = JsonSerializer.Serialize(deletePayload);

            foreach (var client in wsServer.ListClients())
            {
                await wsServer.SendAsync(client.Guid, json);
            }
        }
        catch (Exception ex)
        {
            Log($"[DELETION ERROR] Failed to forward clear command: {ex.Message}");
        }
    }

    private async Task HandleUserPurge(string username)
    {
        try
        {
            var purgePayload = new
            {
                Type = "ClearUserMessages",
                TargetUsername = username
            };

            string json = JsonSerializer.Serialize(purgePayload);

            foreach (var client in wsServer.ListClients())
            {
                await wsServer.SendAsync(client.Guid, json);
            }

            Log($"[MODERATION] Sent clear command for timed out/banned user: {username}");
        }
        catch (Exception ex)
        {
            Log($"[PURGE ERROR] Failed to forward user wipe: {ex.Message}");
        }
    }

    private async Task SendFollowersToClient(Guid clientGuid)
    {
        try
        {
            using var db = _databaseService.GetConnection();

            string sql = @"
                SELECT UserID, Username, FollowDate, IsModerator, Message_Count
                FROM Viewers
                WHERE Message_Count > 0
                ORDER BY Message_Count DESC
                LIMIT 100";

            var followers = db.Query(sql).ToList();

            var payload = new
            {
                Type = "FollowerList",
                Data = followers
            };

            string json = JsonSerializer.Serialize(payload);

            await wsServer.SendAsync(clientGuid, json);
            Log($"[Websocket Sent] Dispatched {followers.Count} database records to client {clientGuid}");
        }
        catch (Exception ex)
        {
            Log($"[DATABASE TO WEBSOCKET ERROR]: {ex.Message}");
        }
    }

    private async Task JoinedChannel(object? sender, OnJoinedChannelArgs e)
    {
        Log($"Joined Channel: {e.Channel}");
        Say("Ready!");
    }

    private async Task OnConnected(object? sender, TwitchLib.Client.Events.OnConnectedEventArgs e)
    {
        Log("I have connected!");
        await Client.JoinChannelAsync(streamName);
    }

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

    // this is ran once by itself whenever you first setup the bot and whenever you want to update your scopes. 
    // requires the authorization code from you URL and the exact URI used 
    public async void GetInitialTokens(string authorizationCode, string redirectUri)
    {
        Log("Exchanging authorization code for tokens...");

        API = new TwitchAPI();
        await _fileLock.WaitAsync();
        try
        {
            AuthCodeResponse response = await API.Auth.GetAccessTokenFromCodeAsync(authorizationCode, Secret, redirectUri, ClientID);

            File.WriteAllText(accessPath, response.AccessToken);
            RefreshToken = response.RefreshToken;

            Log("Successfully received new tokens.");
            Log($"New Access Token: {File.ReadAllText(accessPath)}");
            Log($"New Refresh Token: {RefreshToken}");

            File.WriteAllText(refreshPath, RefreshToken);
        }
        catch (Exception ex)
        {
            Log($"Error exchanging code for tokens: {ex.Message}");
        }
        finally { _fileLock.Release(); }
    }

}
