using RemnaBotService.TwitchBot;
using TwitchLib.Api;
using TwitchLib.Api.Auth;
using TwitchLib.Api.Helix;
using TwitchLib.Api.Helix.Models.Channels.GetChannelFollowers;
using TwitchLib.Api.Interfaces;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Events;

namespace RemnaBotService;




public class TwitchClientContainer : TwitchLogger
{
    static string baseDir = AppDomain.CurrentDomain.BaseDirectory;
    public TwitchClient Client = new TwitchClient();
    public ConnectionCredentials Credentials;
    public TwitchAPI API;
    static string tourneyPath = Path.Combine(baseDir, "tourney link.txt");
    static string arenaIDPath = Path.Combine(baseDir, "arena ID.txt");
    static string secretPath = Path.Combine(baseDir, "twitch secret.txt");
    static string clientIdPath = Path.Combine(baseDir, "bot client ID.txt");
    static string refreshPath = Path.Combine(baseDir, "Refresh Token.txt");
    static string accessPath = Path.Combine(baseDir, "Access Token.txt");
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
    


    private Dictionary<string, EternalDragon> activeGames = new Dictionary<string, EternalDragon>();

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
                Log("Streamer is not LIVE! Rechecking in 60 seconds...");
            }
        }
        catch (Exception ex) when (ex.Message.Contains("401") || ex.Message.Contains("bad credentials"))
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
                    Say("Boop!");
                    break;

                case "join":
                case "id":
                case "arena":
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
                    break;

                case "server":
                case "discord":
                    Say("You can join the discord at https://discord.gg/vtZtMAVVMh");
                    break;


                case "tourney":
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
                    break;

                case "lurk":
                    Say("I see you big dog!");
                    break;

                case "setid":
                    if (e.ChatMessage.UserType.Equals("moderator") || e.ChatMessage.IsBroadcaster)
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
                    break;

                case "openarena":
                    if (e.ChatMessage.UserDetail.IsModerator || e.ChatMessage.IsBroadcaster)
                    {
                        if (IsOnCooldown("openarena", _standardCooldown))
                        {
                            Log("openarena is on global cooldown.");
                            break;
                        }
                        Say("Sending arena info to the discord server!");
                        ArenaOpen?.Invoke(this, $"<@&1514411853466308669>, a stream arena is open!\nID: {arenaID}");
                    }
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

                    if (IsOnCooldown("followage", TimeSpan.FromMinutes(1)))
                    {
                        Log($"followage is on cooldown for {username}.");
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



    private async Task MessageReceived(object? sender, OnMessageReceivedArgs e) => Log($"Message from {e.ChatMessage.Username}:{e.ChatMessage.Message}");

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

    public async void GetInitialTokens(string authorizationCode, string redirectUri)
    {
        Log("Exchanging authorization code for tokens...");
        await _fileLock.WaitAsync();
        try
        {
            AuthCodeResponse response = await API.Auth.GetAccessTokenFromCodeAsync(authorizationCode, Secret, redirectUri, ClientID);

            File.WriteAllText(accessPath, response.AccessToken);
            RefreshToken = response.RefreshToken;

            Log("Successfully received new tokens.");
            Log($"New Access Token: {File.ReadAllText(accessPath)}");
            Log($"New Refresh Token: {RefreshToken}");

            // Save the new refresh token to your file
            File.WriteAllText(refreshPath, RefreshToken);
        }
        catch (Exception ex)
        {
            Log($"Error exchanging code for tokens: {ex.Message}");
        }
        finally { _fileLock.Release(); }
    }

}
