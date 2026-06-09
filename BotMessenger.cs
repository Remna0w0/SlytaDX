using RemnaBotService.Eternal_Dragon;

namespace RemnaBotService
{
    public interface BotMessenger
    {
        void SendGameMessage(GameMessage message);
        Task<string> GetUserInputAsync(string username);
        Task<int> GetUserSelectionAsync(string[] options, string username);

    }
}
