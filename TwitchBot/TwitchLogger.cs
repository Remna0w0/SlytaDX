using System.Collections.Concurrent;

namespace RemnaBotService
{
    public abstract class TwitchLogger : IsBot, IDisposable
    {
        static string baseDir = AppDomain.CurrentDomain.BaseDirectory;

        static string fileLog = Path.Combine(baseDir, "Log/twitchbot log.txt");

        public event Action<string> OnOutputLog;

        private static BlockingCollection<string> _logQueue = new BlockingCollection<string>();
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

            Task.Run(() =>
            {
                foreach (var message in _logQueue.GetConsumingEnumerable())
                {
                    try
                    {
                        File.AppendAllText(fileLog, message + Environment.NewLine);
                    }
                    catch (Exception ex)
                    {
                        Console.Write($"[CRITICAL LOG ERROR]: Could not write to file: {ex.Message}");
                    }
                }
            });

            OnOutputLog += (message) =>

            {
                string formatted = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");

                _logQueue.Add(formatted);
            };
        }

        public void Dispose()
        {
            _logQueue.CompleteAdding();
        }


    }
}
