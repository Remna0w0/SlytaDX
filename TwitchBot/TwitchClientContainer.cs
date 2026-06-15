using Dapper;
using TwitchLib.Api;
using TwitchLib.Api.Auth;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;

namespace RemnaBotService;




public class TwitchClientContainer : TwitchLogger
{
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
    public string arenaID = File.ReadAllText(arenaIDPath);
    public string tourneyLink = File.ReadAllText(tourneyPath);
    private System.Timers.Timer liveCheckTimer;
    private bool isLive = false;
    private bool currentlyRefreshing = false;
    private static readonly SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);
    private DateTime _lastRefreshAttempt = DateTime.MinValue;
    private readonly TimeSpan _cooldown = TimeSpan.FromMinutes(2);
    private readonly TimeSpan _standardCooldown = TimeSpan.FromSeconds(15);
    private Dictionary<string, DateTime> _cooldowns = new Dictionary<string, DateTime>();
    public event EventHandler<string> OnStreamGoLive;
    public event EventHandler<string> ArenaOpen;




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


        Credentials = new ConnectionCredentials(BotUsername, $"oauth:{API.Settings.AccessToken}");
        Client.OnConnected += OnConnected;
        Client.OnJoinedChannel += JoinedChannel;
        Client.OnMessageReceived += MessageReceived;
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

            if (live && !isLive)
            {
                isLive = true;
                OnStreamGoLive?.Invoke(this, "@everyone Remna is LIVE! Come thru! https://www.twitch.tv/remnapi");
                Log("Streamer is LIVE! Rechecking in 60 seconds...");
            }
            else if (!live)
            {
                isLive = false;
                Log("Streamer is OFFLINE! Rechecking in 60 seconds...");
            }
        }
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

    private async Task GetStreamerID()
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

            if (refreshResult != null && !string.IsNullOrEmpty(refreshResult.AccessToken) && !string.IsNullOrEmpty(refreshResult.RefreshToken))
            {
                API.Settings.AccessToken = refreshResult.AccessToken;

                if (Client.IsConnected)
                {
                    Log("Updating Chat Client credentials...");

                    currentlyRefreshing = true;

                    await Client.DisconnectAsync();

                    await Task.Delay(500);

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

    private bool IsOnCooldown(string cooldownKey, TimeSpan cooldownLength)
    {
        if (_cooldowns.TryGetValue(cooldownKey, out DateTime lastUsedTime))
        {
            if (DateTime.Now - lastUsedTime < cooldownLength)
            {
                return true;
            }
        }
        _cooldowns[cooldownKey] = DateTime.Now;
        return false;
    }



    private async Task ChatCommand(object? sender, OnChatCommandReceivedArgs e)
    {
        string username = e.ChatMessage.Username;
        try
        {
            string command = e.Command.Name.ToLower();

            switch (command)
            {
                case "beep":
                    if (IsOnCooldown("beep", _standardCooldown))
                    {
                        Log("beep is on cooldown.");
                        break;
                    }
                    Say("Boop!");
                    LogCommand(e.ChatMessage.UserId, "beep");
                    break;

                case "join":
                case "id":
                case "arena":
                    if (IsOnCooldown("arena", TimeSpan.FromSeconds(10)))
                    {
                        Log("arena is on cooldown.");
                        break;
                    }
                    await _fileLock.WaitAsync();
                    try
                    {
                        if (File.Exists(arenaIDPath))
                        {
                            Say(File.ReadAllText(arenaIDPath));
                        }
                        else
                        {
                            Log("ATTENTION: Arena ID file missing. Check root. Returning default response.");
                            Say("No arena open!");
                        }
                    }
                    finally { _fileLock.Release(); }
                    LogCommand(e.ChatMessage.UserId, "arena");
                    break;

                case "server":
                case "discord":
                    Say("You can join the discord at https://discord.gg/vtZtMAVVMh");
                    LogCommand(e.ChatMessage.UserId, "discord");
                    break;

                case "bracket":
                case "tourney":
                    if (IsOnCooldown("tourney", _standardCooldown))
                    {
                        Log("tourney is on cooldown.");
                        break;
                    }

                    await _fileLock.WaitAsync();
                    try
                    {
                        if (File.Exists(tourneyPath))
                        {
                            Say(tourneyLink);
                        }
                        else
                        {
                            Log("ATTENTION: Tourney Link file missing. Check root. Returning default response");
                            Say("No tournies open!");
                        }
                    }
                    finally { _fileLock.Release(); }
                    LogCommand(e.ChatMessage.UserId, "tourney");
                    break;

                case "lurk":
                    if (IsOnCooldown("lurk", TimeSpan.FromSeconds(5)))
                    {
                        Log("lurk is on cooldown.");
                        break;
                    }

                    Say("I see you big dog!");
                    LogCommand(e.ChatMessage.UserId, "lurk");
                    break;

                case "setid":
                    if (e.ChatMessage.UserDetail.IsModerator || e.ChatMessage.UserDetail.IsVip || e.ChatMessage.IsBroadcaster)
                    {
                        if (e.Command.ArgumentsAsList.Count > 0)
                        {
                            await _fileLock.WaitAsync();
                            try
                            {
                                if (File.Exists(arenaIDPath))
                                {
                                    string newID = e.Command.ArgumentsAsList[0];
                                    File.WriteAllText(arenaIDPath, newID);
                                    arenaID = newID;
                                    Say($"ID updated to: {newID}");
                                }
                                else
                                {
                                    Log("ATTENTION: Arena ID file missing. Check root. Warning command user.");
                                    Say("Error! Contact host!");
                                }
                            }
                            finally { _fileLock.Release(); }
                        }
                        else
                        {
                            Say($"{e.ChatMessage.Username}, please provide an ID!");
                        }
                    }
                    LogCommand(e.ChatMessage.UserId, "setid");
                    break;

                case "openarena":
                    if (e.ChatMessage.UserDetail.IsModerator || e.ChatMessage.UserDetail.IsVip || e.ChatMessage.IsBroadcaster)
                    {
                        if (IsOnCooldown("openarena", _standardCooldown))
                        {
                            Log("openarena is on global cooldown.");
                            break;
                        }
                        Say("Sending arena info to the discord server!");
                        ArenaOpen?.Invoke(this, $"<@&1514411853466308669>, a stream arena is open!\nID: {arenaID}");
                    }
                    LogCommand(e.ChatMessage.UserId, "openarena");
                    break;

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

                    if (IsOnCooldown("followage", _standardCooldown))
                    {
                        Log("followage is on cooldown.");
                        break;
                    }

                    string viewerID = e.ChatMessage.UserId;


                    API.Settings.ClientId = ClientID;
                    API.Settings.AccessToken = File.ReadAllText(accessPath);

                    Log($"DEBUG: Attempting followage check | ViewerID: '{viewerID}' | StreamerID: '{streamerID}'");

                    if (viewerID == streamerID)
                    {
                        Say("You can't follow yourself!");
                        break;
                    }

                    try
                    {
                        var followsResponse = await API.Helix.Channels.GetChannelFollowersAsync(
                            broadcasterId: streamerID,
                            userId: viewerID
                        );

                        if (followsResponse.Data == null || followsResponse.Data.Length == 0)
                        {
                            Say($"{username} doesn't follow this channel!");
                            break;
                        }


                        DateTime followedAt = DateTime.Parse(followsResponse.Data[0].FollowedAt);
                        TimeSpan followDuration = DateTime.UtcNow - followedAt;

                        Say($"{username}, you have been following for {FormatTimeSpan(followDuration)}!");
                    }
                    catch (Exception ex)
                    {
                        Log($"Followage API Error: {ex.Message}");
                        Say("I don't have permission to see followers! Make sure I'm a mod and have the right scopes.");
                    }
                    LogCommand(e.ChatMessage.UserId, "followage");
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



    private async Task MessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        Log($"Message from {e.ChatMessage.Username}:{e.ChatMessage.Message}");
        bool isMod = e.ChatMessage.UserDetail.IsModerator;
        string userID = e.ChatMessage.UserId.ToString();

        using var db = _databaseService.GetConnection();
        string checkSql = "SELECT COUNT(1) FROM Viewers WHERE UserID = @id";
        int exists = db.ExecuteScalar<int>(checkSql, new { id = userID });

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

    private async Task JoinedChannel(object? sender, OnJoinedChannelArgs e)
    {
        Log($"Joined Channel: {e.Channel}");
        Say("Ready!");
    }

    private string FormatTimeSpan(TimeSpan timeSpan)
    {
        var years = timeSpan.Days / 365;
        var months = (timeSpan.Days % 365) / 30;
        var days = (timeSpan.Days % 365) % 30;
        var hours = timeSpan.Hours;
        var minutes = timeSpan.Minutes;

        var parts = new List<string>();
        if (years > 0) parts.Add($"{years} year{(years > 1 ? "s" : "")}");
        if (months > 0) parts.Add($"{months} month{(months > 1 ? "s" : "")}");
        if (days > 0) parts.Add($"{days} day{(days > 1 ? "s" : "")}");
        if (hours > 0) parts.Add($"{hours} hour{(hours > 1 ? "s" : "")}");
        if (minutes > 0) parts.Add($"{minutes} minute{(minutes > 1 ? "s" : "")}");

        if (parts.Count == 0)
        {
            return "just now";
        }

        return string.Join(", ", parts);
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
