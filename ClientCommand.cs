namespace RemnaBotService
{
    public class ClientCommand
    {
        public interface UniLogger
        {
            public event Action<string> OnOutputLog;
            void Log(string message);
            void Log(string source, string message);

            void SetupLogging()
            {
                OnOutputLog += (message) =>
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
                };
            }
        }






    }
}
