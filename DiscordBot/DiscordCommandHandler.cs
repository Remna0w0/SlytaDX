using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemnaBotService.DiscordBot
{
    internal class DiscordCommandHandler 
    {
        private static readonly SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);
        private readonly TimeSpan _standardCooldown = TimeSpan.FromSeconds(15);
        private Dictionary<string, DateTime> _cooldowns = new Dictionary<string, DateTime>();

        private bool IsOnCooldown(string cooldownKey, TimeSpan cooldownLength)
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

        public async void RoleSpawnCommand(DiscordClientContainter client, SocketMessage message)
        {
            if (message.Author is SocketGuildUser guildUser)
            {
                if (!guildUser.GuildPermissions.Administrator)
                {
                    client.Log("Non-admin attempted to spawn role buttons. Command ignored.");
                    return;
                }
            }

            var builder = new ComponentBuilder()
                .WithButton("He/Him", "role_he", ButtonStyle.Primary, emote: new Emoji("\U0001F499"), row: 0)
                .WithButton("She/Her", "role_she", ButtonStyle.Primary, emote: new Emoji("\U0001Fa77"), row: 0)
                .WithButton("They/Them", "role_they", ButtonStyle.Primary, emote: new Emoji("\U0001F49C"), row: 0)
                .WithButton("Ask", "role_ask", ButtonStyle.Primary, emote: new Emoji("\U0001F49A"), row: 0)

                .WithButton("Viewers!", "role_viewer", ButtonStyle.Secondary, emote: new Emoji("\U0001F440"), row: 1)
                .WithButton("Streamers!", "role_streamer", ButtonStyle.Secondary, emote: new Emoji("\U0001F3A5"), row: 1)
                .WithButton("Artists!", "role_artist", ButtonStyle.Secondary, emote: new Emoji("\U0001F58C"), row: 1)
                .WithButton("Fighters!", "role_fighter", ButtonStyle.Secondary, emote: new Emoji("\U0001F94A"), row: 1);

            await message.Channel.SendMessageAsync("Welcome! Click the buttons to add or remove roles:", components: builder.Build());
        }

        public async void PingCommand(DiscordClientContainter client, SocketMessage message)
        {
            if (IsOnCooldown("ping", _standardCooldown))
            {
                client.Log("ping is on cooldown.");
                return;
            }
            await client.Say(message.Channel, "Pong!");
            client.LogCommand(message.Author.Id.ToString(), "ping");
        }

        public async void DragonCommand(DiscordClientContainter client, SocketMessage message, string username)
        {
            if (IsOnCooldown("dragon", TimeSpan.FromSeconds(10)))
            {
                client.Log("dragon is on cooldown.");
                return;
            }

            // required so the bot does not make multiple game instances for one user 
            if (!client.activeGames.ContainsKey(username))
            {
                var messenger = new DiscordMessenger(client, message.Channel, username);
                var newGame = new EternalDragon(messenger);
                client.activeGames.Add(username, newGame);

                _ = Task.Run(async () =>
                {
                    try { await newGame.Dragon(username); }
                    finally { client.activeGames.Remove(username); }
                });

                await client.Say(message.Channel, "Dragon game started!");
            }
            client.LogCommand(message.Author.Id.ToString(), "dragon");
        }
    }
}
