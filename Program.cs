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

    static void Main(string[] args)
    {
        Console.CancelKeyPress += (s, e) =>
        {
            Console.WriteLine("Shutting down...");
            OnShutdown();
            e.Cancel = true; // Prevents immediate exit, letting the code finish
            Environment.Exit(0);
        };

        TwitchClient.SetupLogging();

        DiscordClient.SetupLogging();
        TwitchClient.Initialize();

        DiscordClient.Intialize();



        TwitchClient.OnStreamGoLive += async (sender, message) =>
        {
            ulong targetChannelId = 1293234433478099046;

            var channel = await DiscordClient.GetChannelAsync(targetChannelId);

            if (channel != null)
            {
                await DiscordClient.Say(channel, message);
            }
        };



        Console.ReadLine();


    }









}
