using System.Collections.Concurrent;

namespace RemnaBotService
{
    public class DiscordLogger : IsBot, IDisposable
    {
        // Note: The logger classes DO NOT contain a Semaphore because they should be the only things writing to their associated files. 
        // If you add something that will write to this class' log file, I suggest adding a Semaphore


        static string baseDir = AppDomain.CurrentDomain.BaseDirectory;

        static string fileLog = Path.Combine(baseDir, "Log/discordbot log.txt");

        public event Action<string> OnOutputLog;

        private static BlockingCollection<string> _logQueue = new BlockingCollection<string>();

        // since logging is now stored in separate files for the separate platforms, the source is not really need. Looks nicer though!
        public void Log(string message)
        {
            OnOutputLog?.Invoke($"<Discord> {message}");
        }
        public void Log(string source, string message)
        {
            OnOutputLog?.Invoke($"{source}: {message}");
        }

        // we keep the prior log file for debugging purposes, and to check its metadata to see if the bot crashed before being restarted by bat 
        public void SetupLogging()
        {
            if (File.Exists(fileLog))
            {

                string oldLog = fileLog.Replace(".txt", "_old.txt");
                if (File.Exists(oldLog)) File.Delete(oldLog);
                File.Move(fileLog, oldLog);
            }

            // ensure each entry gets it's own line
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
