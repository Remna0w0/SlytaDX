using static RemnaBotService.ClientCommand;

namespace RemnaBotService
{
    public class TwitchLogger : UniLogger
    {
        static string baseDir = AppDomain.CurrentDomain.BaseDirectory;

        static string fileLog = Path.Combine(baseDir, "twitchbot log.txt");

        public event Action<string> OnOutputLog;
        public void Log(string message)
        {

            OnOutputLog?.Invoke($"<Twitch> {message}");

        }
        public void Log(string source, string message)
        {
            OnOutputLog?.Invoke($"{source}: {message}");

        }
        public void SetupLogging()
        {
            if (File.Exists(fileLog))
            {

                string oldLog = fileLog.Replace(".txt", "_old.txt");
                if (File.Exists(oldLog)) File.Delete(oldLog);
                File.Move(fileLog, oldLog);
            }

            OnOutputLog += (message) =>

            {
                string formatted = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] <Twitch> {message}";
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");

                try
                {
                    File.AppendAllText(fileLog, formatted + Environment.NewLine);
                }
                catch { Console.WriteLine("ERROR WRITING TO LOG FILE. CHECK ROOT. IGNORE IF NO ISSUES."); }
            };
        }


    }
}
