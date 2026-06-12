# SlytaBot

SlytaBot is a custom, cross-platform bridge bot designed to sync Twitch and Discord activities. It handles real-time stream notifications, arena management, and server role assignments.

## Features

* **Cross-Platform Integration:** Triggers Discord alerts directly from Twitch chat commands.
* **Arena Management:** Allows moderators to update and share arena IDs across both platforms seamlessly.
* **Discord Role Assignment:** Uses interactive buttons for users to self-assign pronouns and community roles (e.g., Viewer, Streamer, Artist, Fighter).
* **Automatic Token Management:** Handles Twitch API token refreshing automatically to keep the bot connected without manual intervention.

## Setup Requirements

To run the bot, ensure the following text files are present in the root directory:

* `twitch secret.txt`
* `bot client ID.txt`
* `Refresh Token.txt`
* `Access Token.txt`
* `SlytaBot Token.txt` (Discord Bot Token)
* `tourney link.txt`
* `arena ID.txt`

> **⚠️ SECURITY WARNING:** These files contain sensitive authentication tokens. **NEVER** commit these files to GitHub. Ensure they are included in your `.gitignore` file before pushing your code.

## Commands

### Twitch
| Command | Description |
| :--- | :--- |
| `!id` / `!arena` | Displays the current Arena ID. |
| `!discord` | Provides the Discord invite link. |
| `!openarena` | (Mod only) Sends an alert to Discord with the current Arena ID. |
| `!setid [ID]` | (Mod only) Updates the stored Arena ID. |
| `!tourney` | Displays the current tournament link. |

### Discord
| Command | Description |
| :--- | :--- |
| `%roles` | (Admin only) Spawns the role assignment button panel. |
| `%dragon` | Starts an interactive "Eternal Dragon" game session. |
| `%ping` | Returns "Pong!" |

## Technical Architecture

SlytaBot uses a modular approach with specific containers for each platform:

* **`TwitchClientContainer`**: Manages Twitch chat connectivity, API interactions, and live status checks.
* **`DiscordClientContainer`**: Manages Discord gateway events, button interactions, and message routing.
* **`Program.cs`**: Orchestrates the startup process and bridges events (like `ArenaOpen`) between the two platforms.
