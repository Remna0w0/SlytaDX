using RemnaBotService.Eternal_Dragon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemnaBotService
{
    public interface BotMessenger
    {
        void SendGameMessage(GameMessage message);
        Task<string> GetUserInputAsync(string username);
        Task<int> GetUserSelectionAsync(string[] options, string username);

    }
}
