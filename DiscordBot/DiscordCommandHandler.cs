using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemnaBotService.DiscordBot
{
    public class DiscordCommandHandler 
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

        public async Task RoleSpawnCommand(IDiscordClientWrapper client, IUserMessage message)
        {
            if (message.Author is IGuildUser guildUser)
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

        public async Task PingCommand(IDiscordClientWrapper client, IUserMessage message)
        {
            if (IsOnCooldown("ping", _standardCooldown))
            {
                client.Log("ping is on cooldown.");
                return;
            }
            await client.Say(message.Channel, "Pong!");
            client.LogCommand(message.Author.Id.ToString(), "ping");
        }

        public async Task DragonCommand(IDiscordClientWrapper client, IUserMessage message, string username)
        {
            if (IsOnCooldown("dragon", TimeSpan.FromSeconds(10)))
            {
                client.Log("dragon is on cooldown.");
                return;
            }

            // required so the bot does not make multiple game instances for one user 
            if (!client.IsGameActive(username))
            {
                client.StartDragonGame(username, message.Channel);
                await client.Say(message.Channel, "Dragon game started!");
            }
            else
            {
                await client.Say(message.Channel, $"{username}, you have already started a game!");
            }

                client.LogCommand(message.Author.Id.ToString(), "dragon");
        }

        public async Task LeaderboardCommand(IDiscordClientWrapper client, IUserMessage message, DatabaseService dbService)
        {
            if (IsOnCooldown("leaderboard", TimeSpan.FromSeconds(10)))
            {
                client.Log("Leaderboard command is on cooldown.");
                return;
            }

            var topPlayers = dbService.GetTopPlayers(10).ToList();

            if (!topPlayers.Any())
            {
                await client.Say(message.Channel, "The leaderboard is currently empty! Play `%dragon` to log the first record.");
                client.LogCommand(message.Author.Id.ToString(), "leaderboard");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("🏆 **Top 10 Eternal Dragon Slayers** 🏆\n");
            sb.AppendLine("` Rank | Wins | Played | Player `");
            sb.AppendLine("`---------------------------------`");

            for (int i = 0; i < topPlayers.Count; i++)
            {
                var player = topPlayers[i];
                string rank = (i + 1).ToString().PadRight(4);
                string wins = player.GamesWon.ToString().PadRight(4);
                string played = player.GamesPlayed.ToString().PadRight(6);

                sb.AppendLine($"` #{rank} | {wins} | {played} `  **{player.Username}**");
            }

            var embedBuilder = new EmbedBuilder()
                .WithTitle("Server Hall of Fame")
                .WithDescription(sb.ToString())
                .WithColor(Color.Gold)
                .WithCurrentTimestamp();

            await message.Channel.SendMessageAsync(embed: embedBuilder.Build());
            client.LogCommand(message.Author.Id.ToString(), "leaderboard");
        }

        public interface IDiscordClientWrapper
        {
            void Log(string message);
            void LogCommand(string userId, string commandName);
            Task Say(IMessageChannel channel, string text);

            bool IsGameActive(string username);

            void StartDragonGame(string username, IMessageChannel channel);

        }
    }
}
