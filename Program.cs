namespace RemnaBotService;

internal class Program
{
    static public TwitchClientContainer TwitchClient = new TwitchClientContainer();
    static public DiscordClientContainter DiscordClient = new DiscordClientContainter();

    static void Main(string[] args)
    {
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
