using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Client.Events;

namespace RemnaBotService.TwitchBot
{
    public class TwitchCommandHandler 
    {
        private static readonly SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);
        private readonly TimeSpan _standardCooldown = TimeSpan.FromSeconds(15);
        private Dictionary<string, DateTime> _cooldowns = new Dictionary<string, DateTime>();
        public event EventHandler<string> ArenaOpen;




        public bool IsOnCooldown(string cooldownKey, TimeSpan cooldownLength)
        {
            if (_cooldowns.TryGetValue(cooldownKey, out DateTime lastUsedTime))
            {
                if (DateTime.Now - lastUsedTime < cooldownLength)
                {
                    return true;
                }
            }
            _cooldowns[cooldownKey] = DateTime.Now;
            return false;
        }
        public void BeepCommand(ITwitchClientWrapper client, string userId)
        {
            if (IsOnCooldown("beep", _standardCooldown))
            {
                client.Log("beep is on cooldown.");
                return;
            }
            client.Say("Boop!");
            client.LogCommand(userId, "beep");
        }


        public async Task ArenaCommand(ITwitchClientWrapper client, string userId, string arenaIDPath)
        {
            if (IsOnCooldown("arena", TimeSpan.FromSeconds(10)))
            {
                client.Log("arena is on cooldown.");
                return;
            }
            await _fileLock.WaitAsync();
            try
            {
                if (client.FileExists(arenaIDPath))
                {
                    client.Say(client.ReadFileText(arenaIDPath));
                }
                else
                {
                    // Tell the admin the actual issue, hide it from user to reduce confusion 
                    client.Log("ATTENTION: Arena ID file missing. Check root. Returning default response.");
                    client.Say("No arena open!");
                }
            }
            finally { _fileLock.Release(); }
            client.LogCommand(userId, "arena");
        }

        public void DiscordCommand(ITwitchClientWrapper client, string userId)
        {
            if (IsOnCooldown("discord", _standardCooldown))
            {
                client.Log("discord is on cooldown.");
                return;
            }
            client.Say("You can join the discord at https://discord.gg/vtZtMAVVMh");
            client.LogCommand(userId, "discord");
        }

        public async Task TourneyCommand(ITwitchClientWrapper client, string userId, string tourneyPath)
        {
            if (IsOnCooldown("tourney", _standardCooldown))
            {
                client.Log("tourney is on cooldown.");
                return;
            }

            await _fileLock.WaitAsync();
            try
            {
                if (client.FileExists(tourneyPath))
                {
                    client.Say(client.ReadFileText(tourneyPath));
                }
                else
                {
                    client.Log("ATTENTION: Tourney Link file missing. Check root. Returning default response");
                    client.Say("No tournies open!");
                }
            }
            finally { _fileLock.Release(); }
            client.LogCommand(userId, "tourney");
        }

        public void LurkCommand(ITwitchClientWrapper client, string userId)
        {
            if (IsOnCooldown("lurk", TimeSpan.FromSeconds(5)))
            {
                client.Log("lurk is on cooldown.");
                return;
            }

            client.Say("I see you big dog!");
            client.LogCommand(userId, "lurk");
        }

        public async Task SetIDCommand(ITwitchClientWrapper client, string userId, string idArg, string arenaIDPath)
        {
                    
                    await _fileLock.WaitAsync();
                    try
                    {
                        if (client.FileExists(arenaIDPath))
                        {
                            client.WriteFileText(arenaIDPath, idArg);
                            client.Say($"ID updated to: {idArg}");
                        }
                        else
                        {
                            client.Log("ATTENTION: Arena ID file missing. Check root. Warning command user.");
                            client.Say("Error! Contact host!");
                        }
                    }
                    finally { _fileLock.Release(); }

                    client.LogCommand(userId, "setid");
        }


        public void OpenArenaCommand(ITwitchClientWrapper client, string userId, string arenaIDPath)
        {
                client.Say("Sending arena info to the discord server!");
                ArenaOpen?.Invoke(this, $"<@&1514411853466308669>, a stream arena is open!\nID: {client.ReadFileText(arenaIDPath)}");
            
            client.LogCommand(userId, "openarena");
        }


        public async Task FollowageCommand(ITwitchClientWrapper client, string viewerID, string username, string streamerID)
        {

            if (IsOnCooldown("followage", _standardCooldown))
            {
                client.Log("followage is on cooldown.");
                return;
            }

           
            client.Log($"DEBUG: Attempting followage check | ViewerID: '{viewerID}' | StreamerID: '{streamerID}'");

            if (viewerID == streamerID)
            {
                client.Say("You can't follow yourself!");
                return;
            }

            try
            {

                DateTime? followDate = await client.GetFollowDateAsync(streamerID, viewerID);

                if (followDate == null)
                {
                    client.Say($"{username} doesn't follow this channel!");
                    client.LogCommand(viewerID, "followage");
                    return;
                }

                // Compare the time they followed to the time the command was called, 
                TimeSpan followDuration = DateTime.UtcNow - followDate.Value;

                client.Say($"{username}, you have been following for {FormatTimeSpan(followDuration)}!");
            }
            catch (Exception ex)
            {
                client.Log($"Followage API Error: {ex.Message}");
                client.Say("I don't have permission to see followers! Make sure I'm a mod and have the right scopes.");
            }
            client.LogCommand(viewerID, "followage");
        }




        // for the followage command
        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            var years = timeSpan.Days / 365;
            var months = (timeSpan.Days % 365) / 30;
            var days = (timeSpan.Days % 365) % 30;
            var hours = timeSpan.Hours;
            var minutes = timeSpan.Minutes;

            var parts = new List<string>();
            if (years > 0) parts.Add($"{years} year{(years > 1 ? "s" : "")}");
            if (months > 0) parts.Add($"{months} month{(months > 1 ? "s" : "")}");
            if (days > 0) parts.Add($"{days} day{(days > 1 ? "s" : "")}");
            if (hours > 0) parts.Add($"{hours} hour{(hours > 1 ? "s" : "")}");
            if (minutes > 0) parts.Add($"{minutes} minute{(minutes > 1 ? "s" : "")}");

            if (parts.Count == 0)
            {
                return "just now";
            }

            return string.Join(", ", parts);
        }


        public interface ITwitchClientWrapper
        {
            void Log(string message);
            void LogCommand(string userId, string message);
       
            void Say(string message);

            Task<DateTime?> GetFollowDateAsync(string streamerId, string viewerId);

            bool FileExists(string path);
            string ReadFileText(string path);

            void WriteFileText(string path, string text);

        }


    }
}
