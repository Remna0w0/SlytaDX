# SlytaDX: Cross-Platform Streaming Bridge Bot

SlytaDX is a custom, asynchronous cross-platform bridge application built with C# and .NET to seamlessly orchestrate event synchronization between Twitch and Discord streaming communities. 

Originally built to automate community logistics, the project has evolved into a robust showcase of modern software patterns, including architectural decoupling for automated testing, event-driven cross-platform communication, and secure automated API OAuth lifecycle management.

---

## 🏗️ Technical Architecture & Design Patterns

SlytaDX utilizes a decoupled, containerized approach to isolate external platform network dependencies from core business domain logic.

* **`DiscordClientContainer` & `TwitchClientContainer`**: Act as low-level gateway abstractions. They hold direct socket connections to the Discord Gateway and Twitch IRC networks, handling raw payload parsing.
* **Interface-Driven Decoupling (`IDiscordClientWrapper`)**: Core command logic in the handlers interacts entirely with abstract wrappers rather than unmockable concrete API structures (like Discord's `SocketMessage` or TwitchLib's event arguments). This architectural boundary allows for comprehensive test execution entirely in memory.
* **State and Concurrency Management**: Asynchronous actions (such as long-running turn-based game states or multi-platform announcement hooks) are managed via robust concurrency models using thread-safe structures like `SemaphoreSlim` and `BlockingCollection<T>` for sequential, non-blocking I/O file logging operations.
* **Data Persistence**: Uses an optimized SQLite data layer driven by **Dapper (Micro-ORM)** to track viewer history, moderator status permissions, and command metrics with minimal overhead.

---

## 🧪 Quality Assurance & Testing Suite

SlytaDX includes an automated test assembly built using **xUnit** and **NSubstitute** to enforce regression safety across the bot’s command handlers. 

### Core Testing Strategies Emplemented:
1. **Behavior & API Rate-Limit Verification**: Using `NSubstitute` to ensure commands execute successfully while asserting that premium external API endpoints are only called when necessary—short-circuiting early during active cooldowns to save rate limits[cite: 13].
2. **File System Simulation & Isolation**: Testing file-dependent commands (such as tracking tournament links or active lobby configurations) cleanly in memory by stubbing virtual file existence and read/write states[cite: 13].
3. **Event-Driven Payload Tracking**: Subscribing temporary local handlers within tests to validate that platform cross-over events (like broadcasting stream alerts across boundaries) fire reliably with correct layouts and structural strings[cite: 13].
4. **Permission Boundary Constraints**: Validating that structural authorization flows correctly allow or short-circuit operations based on mock context evaluation (`IGuildUser` permissions).
5. **Asynchronous Date & Time Calculations**: Robust unit validation evaluating complex `TimeSpan` formatting loops (e.g., computing precise follow durations using mock chronological milestones safely decoupled from CPU clock skew)[cite: 13, 15].


To execute the test suite via the CLI, navigate to the test directory and run:
``dotnet test``

## 🛠️ Key Features & Implementation Wins
1. Automated Token & OAuth Lifecycle Management
To maintain continuous connectivity with the Twitch API without manual human maintenance, SlytaDX implements a background token refreshing mechanism. When an authorization payload expires, the client catches the lifecycle fault, securely processes a refresh rotation utilizing client-secret assertions, and modifies local state targets dynamically.
2. Event-Driven Cross-Platform Synchronization
By leveraging traditional C# EventHandler mechanisms, actions executed on one platform instantly ripple through the other. For example, a moderator setting an interactive game key via Twitch IRC fires custom events routed by Program.cs to trigger clean layout updates over the Discord HTTP Gateway.
3. Interactive State Machine: "Eternal Dragon"
An asynchronous, turn-based battle game framework deployed within Discord. It leverages modern Discord interaction states, processing user responses through TaskCompletionSource<T> input captures to drive a linear game loop without freezing worker threads or blocking global chat handling.

## 🎮 Command Index
### Discord Interface (Prefix: %)
| Command | Authorization | Description |
| :--- | :--- | :--- | 
| `%roles` | `Admin` | Generates a persistent interactive button matrix for self-assigning pronoun and community group nodes. |
| `%dragon` | `Everyone` | Launches an instance of the asynchronous "Eternal Dragon" text adventure loop. |
| `%ping` | `Everyone` | Low-latency socket heartbeat diagnostics test (Returns "Pong!"). |

### Twitch Interface (Prefix: !)
| Command | Authorization | Description |
| :--- | :--- | :--- | 
|`!id/!arena` | `Everyone` | Echoes the active game lobby code. |
| `!discord` | `Everyone` | Prints a secure link to the server community space. |
| `!setid` | `Streamer/Mod/VIP` | Updates local text file to the argument provided, which is echoed by '!id/!arena' and '!openarena'. |
| `!openarena` | `Streamer` | Invokes cross-platform event to send an alert to the discord server with the details of the current lobby code. |
| `!followage` | `Everyone` | Intersects Twitch Helix API nodes to compute precisely how long a specific user entity has followed the target stream. |

## ⚙️ Configuration & Environment
To securely configure a local instance of the application, create a /Config directory at the binary execution path containing the following token parameters:

* `bot client ID.txt` (Twitch App Client Identifier)

* `twitch secret.txt` (Twitch Developer Client Secret)

* `Access Token.txt / Refresh Token.txt` (Dynamic OAuth values)

* `SlytaBot Token.txt` (Discord Bot API Private Token)

> ⚠️ Security Enforcement Note: The directory profiles containing private secret values are strictly decoupled from source control tracking via .gitignore. Never commit active production certificates to public version arrays.

## 📝 Project Context & Disclaimer
SlytaDX is a specialized engineering project designed specifically to accelerate my technical mastery of asynchronous architecture models, defensive C# design, and automated validation patterns. It is tuned intentionally to fulfill the requirements of my independent live-content communities. This repository serves as a professional portfolio documenting my application development standards, algorithmic optimization habits, and system design progression.
