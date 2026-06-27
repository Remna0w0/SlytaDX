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
        public void BeepCommand(TwitchClientContainer client, OnChatCommandReceivedArgs e)
        {
            if (IsOnCooldown("beep", _standardCooldown))
            {
                client.Log("beep is on cooldown.");
                return;
            }
            client.Say("Boop!");
            client.LogCommand(e.ChatMessage.UserId, "beep");
        }


        public async void ArenaCommand(TwitchClientContainer client, OnChatCommandReceivedArgs e, string arenaIDPath)
        {
            if (IsOnCooldown("arena", TimeSpan.FromSeconds(10)))
            {
                client.Log("arena is on cooldown.");
                return;
            }
            await _fileLock.WaitAsync();
            try
            {
                if (File.Exists(arenaIDPath))
                {
                    client.Say(File.ReadAllText(arenaIDPath));
                }
                else
                {
                    // Tell the admin the actual issue, hide it from user to reduce confusion 
                    client.Log("ATTENTION: Arena ID file missing. Check root. Returning default response.");
                    client.Say("No arena open!");
                }
            }
            finally { _fileLock.Release(); }
            client.LogCommand(e.ChatMessage.UserId, "arena");
        }

        public void DiscordCommand(TwitchClientContainer client,OnChatCommandReceivedArgs e)
        {
            client.Say("You can join the discord at https://discord.gg/vtZtMAVVMh");
            client.LogCommand(e.ChatMessage.UserId, "discord");
        }

        public async void TourneyCommand(TwitchClientContainer client,OnChatCommandReceivedArgs e, string tourneyPath, string tourneyLink)
        {
            if (IsOnCooldown("tourney", _standardCooldown))
            {
                client.Log("tourney is on cooldown.");
                return;
            }

            await _fileLock.WaitAsync();
            try
            {
                if (File.Exists(tourneyPath))
                {
                    client.Say(tourneyLink);
                }
                else
                {
                    client.Log("ATTENTION: Tourney Link file missing. Check root. Returning default response");
                    client.Say("No tournies open!");
                }
            }
            finally { _fileLock.Release(); }
            client.LogCommand(e.ChatMessage.UserId, "tourney");
        }

        public void LurkCommand(TwitchClientContainer client,OnChatCommandReceivedArgs e)
        {
            if (IsOnCooldown("lurk", TimeSpan.FromSeconds(5)))
            {
                client.Log("lurk is on cooldown.");
                return;
            }

            client.Say("I see you big dog!");
            client.LogCommand(e.ChatMessage.UserId, "lurk");
        }

        public async void SetIDCommand(TwitchClientContainer client,OnChatCommandReceivedArgs e, string arenaIDPath)
        {
            // The arena ID can only be set by mods or the streamer
            // THIS IS IMPORTANT. Allowing this to be called by viewers is eseentially allowing anyone to write to a file on the machine running the program. Be cautious. 
            if (e.ChatMessage.UserDetail.IsModerator || e.ChatMessage.UserDetail.IsVip || e.ChatMessage.IsBroadcaster)
            {
                if (e.Command.ArgumentsAsList.Count > 0)
                {
                    await _fileLock.WaitAsync();
                    try
                    {
                        if (File.Exists(arenaIDPath))
                        {
                            string newID = e.Command.ArgumentsAsList[0];
                            File.WriteAllText(arenaIDPath, newID);
                            client.Say($"ID updated to: {newID}");
                        }
                        else
                        {
                            client.Log("ATTENTION: Arena ID file missing. Check root. Warning command user.");
                            client.Say("Error! Contact host!");
                        }
                    }
                    finally { _fileLock.Release(); }
                }
                else
                {
                    client.Say($"{e.ChatMessage.Username}, please provide an ID!");
                }
            }
            client.LogCommand(e.ChatMessage.UserId, "setid");
        }


        public void OpenArenaCommand(TwitchClientContainer client,OnChatCommandReceivedArgs e, string arenaIDPath)
        {
            // This invokes the event which Discord uses to send an announcment
            // IT IS IMPORTANT TO KEEP THIS COMMAND TO THE STREAMER ONLY. 
            if (e.ChatMessage.IsBroadcaster)
            {
                if (IsOnCooldown("openarena", _standardCooldown))
                {
                    client.Log("openarena is on global cooldown.");
                    return;
                }
                client.Say("Sending arena info to the discord server!");
                ArenaOpen?.Invoke(this, $"<@&1514411853466308669>, a stream arena is open!\nID: {File.ReadAllText(arenaIDPath)}");
            }
            client.LogCommand(e.ChatMessage.UserId, "openarena");
        }


        public async void FollowageCommand(TwitchClientContainer client,OnChatCommandReceivedArgs e, string streamerID, string accessPath, string username)
        {
            if (string.IsNullOrEmpty(streamerID))
            {
                client.Log("Error: streamerID is null! Retrying fetch...");
                await client.GetStreamerID();
                if (string.IsNullOrEmpty(streamerID))
                {
                    client.Say("Sorry, I don't know who the streamer is yet. Try again in a moment.");
                    return;
                }
            }

            if (IsOnCooldown("followage", _standardCooldown))
            {
                client.Log("followage is on cooldown.");
                return;
            }

            string viewerID = e.ChatMessage.UserId;


            client.API.Settings.ClientId = client.ClientID;
            client.API.Settings.AccessToken = File.ReadAllText(accessPath);

            client.Log($"DEBUG: Attempting followage check | ViewerID: '{viewerID}' | StreamerID: '{streamerID}'");

            if (viewerID == streamerID)
            {
                client.Say("You can't follow yourself!");
                return;
            }

            try
            {
                var followsResponse = await client.API.Helix.Channels.GetChannelFollowersAsync(
                    broadcasterId: streamerID,
                    userId: viewerID
                );

                if (followsResponse.Data == null || followsResponse.Data.Length == 0)
                {
                    client.Say($"{username} doesn't follow this channel!");
                    return;
                }

                // Compare the time they followed to the time the command was called, 
                DateTime followedAt = DateTime.Parse(followsResponse.Data[0].FollowedAt);
                TimeSpan followDuration = DateTime.UtcNow - followedAt;

                client.Say($"{username}, you have been following for {FormatTimeSpan(followDuration)}!");
            }
            catch (Exception ex)
            {
                client.Log($"Followage API Error: {ex.Message}");
                client.Say("I don't have permission to see followers! Make sure I'm a mod and have the right scopes.");
            }
            client.LogCommand(e.ChatMessage.UserId, "followage");
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


    }
}
