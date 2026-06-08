using RemnaBotService.TwitchBot;
using System.Linq.Expressions;
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
    static string tourneyPath = Path.Combine(baseDir, "tourney link.txt");
    static string arenaIDPath = Path.Combine(baseDir, "arena ID.txt");
    static string secretPath = Path.Combine(baseDir, "twitch secret.txt");
    static string clientIdPath = Path.Combine(baseDir, "bot client ID.txt");
    static string refreshPath = Path.Combine(baseDir, "Refresh Token.txt");
    static string accessPath = Path.Combine(baseDir, "Access Token.txt");
    private string streamName = "remnapi";
    public string BotUsername = "SlytaBot";
    public string Secret = File.ReadAllText(secretPath);
    public string ClientID = File.ReadAllText(clientIdPath);
    public string RefreshToken = File.ReadAllText(refreshPath);
    public string arenaID = File.ReadAllText(arenaIDPath);
    public string tourneyLink = File.ReadAllText(tourneyPath);
    private System.Timers.Timer liveCheckTimer;
    private bool isLive = false;
    public event EventHandler<string> OnStreamGoLive;


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

        API.Settings.AccessToken = File.ReadAllText(accessPath);

       

        
        Credentials = new ConnectionCredentials(BotUsername, $"oauth:{File.ReadAllText(accessPath)}");
        Client.OnConnected += OnConnected;
        Client.OnJoinedChannel += JoinedChannel;
        Client.OnMessageReceived += MessageReceived;
        Client.OnChatCommandReceived += ChatCommand;
        Client.OnLog += OnLog;
        Client.Initialize(Credentials);
        Client.Connect();
        SetupLiveCheck();

        Client.OnDisconnected += (sender, e) =>
        {
            Log("Disconnected! Attempting to reconnect...");
            Client.Connect(); 
        };


        initializationCompletionSource.SetResult(); // Signal initialization complete
    }

    private void SetupLiveCheck()
    {
        CheckIfLive();
        liveCheckTimer = new System.Timers.Timer(60000);
        liveCheckTimer.Elapsed += async (s, e) => await CheckIfLive();
        liveCheckTimer.AutoReset = true;
        liveCheckTimer.Enabled = true;
    }

    private async Task CheckIfLive()
    {
        try
        {
            var response = await API.Helix.Streams.GetStreamsAsync(userLogins: new List<string> { streamName });

            bool live = response.Streams.Length > 0;

            if (live && !isLive)
            {
                isLive = true;
                OnStreamGoLive?.Invoke(this, "@everyone Remna is LIVE! Come thru! https://www.twitch.tv/remnapi");
                Log("Streamer is live! Rechecking in 60 seconds...");
            }
            else if (!live)
            {
                isLive = false;
                Log("Streamer is not live! Rechecking in 60 seconds...");
            }
        }
        catch (Exception ex) when (ex.Message.Contains("401") || ex.Message.Contains("bad credentials"))
        {
            Log("Tokens expired! Refreshing tokens...");
            await RefreshMyToken();


            API.Settings.AccessToken = File.ReadAllText(accessPath);

            await CheckIfLive();
        }
        catch (Exception ex)
        {
            Log($"General Error: {ex.Message}");
        }

    }



    public async Task RefreshMyToken()
    {
        Log("Refreshing Token...");
        try
        {
            RefreshResponse refreshResult = await API.Auth.RefreshAuthTokenAsync(RefreshToken, Secret, ClientID);
            RefreshToken = refreshResult.RefreshToken;

            Log($"New Access Token: {refreshResult.AccessToken}");

            File.WriteAllText(accessPath, refreshResult.AccessToken);
            File.WriteAllText(refreshPath, RefreshToken);
        }
        catch (Exception ex)
        {
            Log(ex.Message);
        }
    }




    private async void ChatCommand(object? sender, OnChatCommandReceivedArgs e)
    {
        try
        {
            if (e.Command.CommandText.Equals("beep", StringComparison.OrdinalIgnoreCase))
            {
                Say("Boop!");
            }
            else if (e.Command.CommandText.Equals("id", StringComparison.OrdinalIgnoreCase) || (e.Command.CommandText.Equals("arena", StringComparison.OrdinalIgnoreCase)))
            {
                Say(File.ReadAllText(arenaIDPath));
            }
            else if (e.Command.CommandText.Equals("discord", StringComparison.OrdinalIgnoreCase))
            {
                Say("You can join the discord at https://discord.gg/vtZtMAVVMh");
            }
            else if (e.Command.CommandText.Equals("dragon", StringComparison.OrdinalIgnoreCase))
            {
                string username = e.Command.ChatMessage.Username;

                if (!activeGames.ContainsKey(username))
                {
                    var messenger = new TwitchMessenger(this);
                    var game = new EternalDragon(messenger);

                    try
                    {
                        await game.Dragon(username); // Await the game
                    }
                    finally
                    {
                        activeGames.Remove(username); // Ensure removal even if error
                    }
                }
                else
                {
                    Say($"{username}, you already have a Dragon game running!");
                }
            }
            else if (e.Command.CommandText.Equals("tourney", StringComparison.OrdinalIgnoreCase))
            {
                Say(tourneyLink);
            }
            else if (e.Command.CommandText.Equals("lurk", StringComparison.OrdinalIgnoreCase))
            {
                Say("I see you big dog!");
            }
            else if (e.Command.CommandText.Equals("setid", StringComparison.OrdinalIgnoreCase) && (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster))
            {
                if (e.Command.ArgumentsAsList.Count > 0)
                {
                    string newID = e.Command.ArgumentsAsList[0];
                    File.WriteAllText(arenaIDPath, newID);
                    Say($"ID updated to: {newID}");
                }
                else
                {
                    Say($"{e.Command.ChatMessage.Username}, please provide an ID!");
                }
            }
            else if (e.Command.CommandText.Equals("commands", StringComparison.OrdinalIgnoreCase))
            {
                Say("!beep, !id, !arena, !discord, !dragon, !tourney");
            }
        }
        catch (Exception ex)
        {
            Log($"Command Error: {ex.Message}");
        }
    }

    public void Say(string message)
    {
        Client.SendMessage(Client.JoinedChannels[0], message);
    }

    private void OnLog(object? sender, OnLogArgs e) => Log($"Log: {e.Data}");

    private void MessageReceived(object? sender, OnMessageReceivedArgs e) => Log($"Message from {e.ChatMessage.Username}:{e.ChatMessage.Message}");

    private void JoinedChannel(object? sender, OnJoinedChannelArgs e)
    {
        Log($"Joined Channel: {e.Channel}");
        Say("Hi friend!");
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

    private void OnConnected(object? sender, OnConnectedArgs e)
    {
        Log("I have connected!");
        Client.JoinChannel(streamName);
    }

    public async Task GetInitialTokens(string authorizationCode, string redirectUri)
    {
        Log("Exchanging authorization code for tokens...");
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
    }

}
