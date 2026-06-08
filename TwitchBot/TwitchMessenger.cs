using RemnaBotService.Eternal_Dragon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Client.Events;
using TwitchLib.Communication.Interfaces;

namespace RemnaBotService.TwitchBot
{
    public class TwitchMessenger : BotMessenger
    {
        private readonly TwitchClientContainer _client;
        public TwitchMessenger(TwitchClientContainer client) => _client = client;

        public void SendGameMessage(GameMessage msg)
        {
            // Twitch doesn't support embeds, so just send the text
            // We prioritize the Description if there's no plain Text
            string output = !string.IsNullOrEmpty(msg.Text) ? msg.Text : $"{msg.Title}: {msg.Description}";
            _client.Say(output);
        }
        public async Task<string> GetUserInputAsync(string username)
        {

            var tcs = new TaskCompletionSource<string>();

            void MessageReceivedHandler(object? sender, OnMessageReceivedArgs e)
            {
                if (e.ChatMessage.Username == username)
                {
                    _client.Client.OnMessageReceived -= MessageReceivedHandler; // Use client.Client
                    tcs.SetResult(e.ChatMessage.Message);
                }
            }

            _client.Client.OnMessageReceived += MessageReceivedHandler; // Use client.Client

            return await tcs.Task;
        }

        public async Task<int> GetUserSelectionAsync(string[] options, string username) // async Task<int>
        {
            _client.Say("Use !1, !2, etc. to select:");
            for (int i = 0; i < options.Length; i++)
            {
                _client.Say($"{i + 1}. {options[i]}");
            }

            while (true)
            {
                string input = await GetUserInputAsync(username); // Await here
                if (input.StartsWith("!") && input.Length > 1)
                {
                    string numberPart = input.Substring(1);
                    if (int.TryParse(numberPart, out int selection) && selection >= 1 && selection <= options.Length)
                    {
                        return selection - 1;
                    }
                }
                else
                {
                    _client.Say("Invalid selection. Please use %1, %2, etc.");
                }
            }
        }


    }
}
