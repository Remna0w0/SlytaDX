namespace RemnaBotService;

internal class Program
{
    static public TwitchClientContainer TwitchClient = new TwitchClientContainer();
    static public DiscordClientContainter DiscordClient = new DiscordClientContainter();
    static public void OnShutdown()
    {
        TwitchClient.Dispose();
        DiscordClient.Dispose();
    }

    static async Task Main(string[] args)
    {
         Console.CancelKeyPress += (s, e) =>
        {
            Console.WriteLine("Shutting down...");
            OnShutdown();
            e.Cancel = true; 
            Environment.Exit(0);
        };

        // Logging is called before initialization so the initialization itself can be logged 
        TwitchClient.SetupLogging();
        DiscordClient.SetupLogging();

        await TwitchClient.Initialize();
        await DiscordClient.Intialize();


        // This is where the events for twitch/discord cross communication are put into effect
        TwitchClient.OnStreamGoLive += async (sender, message) =>
        {
            ulong targetChannelId = 1293234433478099046;

            var channel = await DiscordClient.GetChannelAsync(targetChannelId);

            if (channel != null)
            {
                await DiscordClient.Say(channel, message);
            }
        };

        TwitchClient.ArenaOpen += async (sender, message) =>
        {
            ulong targetChannelId = 1514442325529596036;

            var channel = await DiscordClient.GetChannelAsync(targetChannelId);

            if (channel != null)
            {
                await DiscordClient.Say(channel, message);
            }
        };



        Console.ReadLine();
        
          

    }









}
