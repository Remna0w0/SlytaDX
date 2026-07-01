using Discord;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RemnaBotService.TwitchBot;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using static RemnaBotService.TwitchBot.TwitchCommandHandler;

namespace SlytaTest
{
    public class TwitchHandlerTests
    {
        [Fact]

        public void BeepCommand_WhenNotOnCooldown_SendsBoopAndLogs()
        {
            var mockClient = Substitute.For<ITwitchClientWrapper>();

            var handler = new TwitchCommandHandler();

            handler.BeepCommand(mockClient, "slyta123");

            mockClient.Received(1).Say("Boop!");
            mockClient.Received(1).LogCommand("slyta123", "beep");
        }

        [Fact]

        public void BeepCommand_WhenOnCooldown_DoesNotSendBoop()
        {
            var mockClient = Substitute.For<ITwitchClientWrapper>();

            var handler = new TwitchCommandHandler();

            handler.BeepCommand(mockClient, "slyta123");
            handler.BeepCommand(mockClient, "slyta123");

            mockClient.Received(1).Say("Boop!");

            mockClient.Received(1).Log("beep is on cooldown.");
        }

        [Fact]

        public async Task ArenaCommand_WhenNotOnCooldown_WhenPathExists_SendsCodeAndLogs()
        {
            var mockClient = Substitute.For<ITwitchClientWrapper>();
            var handler = new TwitchCommandHandler();
            string fakePath = "Misc/arena ID.txt";

            mockClient.FileExists(fakePath).Returns(true);
            mockClient.ReadFileText(fakePath).Returns("LOBBY_CODE1");

            await handler.ArenaCommand(mockClient, "slyta123", fakePath);

            mockClient.Received(1).Say("LOBBY_CODE1");
            mockClient.Received(1).LogCommand("slyta123", "arena");
        }
        
        [Fact]
        public async Task ArenaCommand_WhenNotOnCooldown_WhenPathNotExists_SendDefaultAndLogError()
        {
            var mockClient = Substitute.For<ITwitchClientWrapper>();
            var handler = new TwitchCommandHandler();
            string fakePath = "Misc/arena ID.txt";

            mockClient.FileExists(fakePath).Returns(false);
            mockClient.ReadFileText(fakePath).Returns("LOBBY_CODE1");

            await handler.ArenaCommand(mockClient, "slyta123", fakePath);

            mockClient.Received(1).Log("ATTENTION: Arena ID file missing. Check root. Returning default response.");
            mockClient.Received(1).Say("No arena open!");

        }

        [Fact]

        public async Task ArenaCommand_WhenOnCooldown_DoesNotSendCode()
        {
            var mockClient = Substitute.For<ITwitchClientWrapper>();
            var handler = new TwitchCommandHandler();
            string fakePath = "Misc/arena ID.txt";

            mockClient.FileExists(fakePath).Returns(true);
            mockClient.ReadFileText(fakePath).Returns("LOBBY_CODE1");

            await handler.ArenaCommand(mockClient, "slyta123", fakePath);
            await handler.ArenaCommand(mockClient, "slyta123", fakePath);

            mockClient.Received(1).Say("LOBBY_CODE1");
            mockClient.Received(1).Log("arena is on cooldown.");
        }

        [Fact]
        public void DiscordCommand_WhenNotOnCooldown_SendsLinkAndLogs()
        {
            var mockClient = Substitute.For<ITwitchClientWrapper>();
            var handler = new TwitchCommandHandler();

            handler.DiscordCommand(mockClient, "slyta123");

            mockClient.Received(1).Say("You can join the discord at https://discord.gg/vtZtMAVVMh");
            mockClient.Received(1).LogCommand("slyta123", "discord");
        }

        [Fact] 
        
        public void DiscordCommand_WhenOnCooldown_DoesNotSendLink()
        {
            var mockClient = Substitute.For<ITwitchClientWrapper>();
            var handler = new TwitchCommandHandler();

            handler.DiscordCommand(mockClient, "slyta123");
            handler.DiscordCommand(mockClient, "slyta123");

            mockClient.Received(1).Say("You can join the discord at https://discord.gg/vtZtMAVVMh");
            mockClient.Received(1).Log("discord is on cooldown.");
        }

        [Fact]
        public async Task TourneyCommand_WhenNotOnCooldown_WhenPathExists_SendsTourneyLink()
        {
            var mockClient = Substitute.For<ITwitchClientWrapper>();
            var handler = new TwitchCommandHandler();
            string fakePath = "Misc/tourney link.txt";
            string fakeLink = "https://challonge.com/fake";

            mockClient.FileExists(fakePath).Returns(true);
            mockClient.ReadFileText(fakePath).Returns(fakeLink);

            await handler.TourneyCommand(mockClient, "slyta123", fakePath);

            mockClient.Received(1).Say(fakeLink);
            mockClient.Received(1).LogCommand("slyta123", "tourney");
        }

        [Fact]  

        public async Task TourneyCommand_WhenNotOnCooldown_WhenPathNotExists_SendsDefaultAndLogsError()
        {
            var mockClient = Substitute.For<ITwitchClientWrapper>();
            var handler = new TwitchCommandHandler();
            string fakePath = "Misc/tourney link.txt";
            string fakeLink = "https://challonge.com/fake";

            mockClient.FileExists(fakePath).Returns(false);
            mockClient.ReadFileText(fakePath).Returns(fakeLink);

            await handler.TourneyCommand(mockClient, "slyta123", fakePath);

            mockClient.Received(1).Say("No tournies open!");
            mockClient.Received(1).Log("ATTENTION: Tourney Link file missing. Check root. Returning default response");
        }

        [Fact]

        public async Task TourneyCommand_WhenOnCooldown_DoesNotSendTourneyLink()
        {
            var mockClient = Substitute.For<ITwitchClientWrapper>();
            var handler = new TwitchCommandHandler();
            string fakePath = "Misc/tourney link.txt";
            string fakeLink = "https://challonge.com/fake";

            mockClient.FileExists(fakePath).Returns(true);
            mockClient.ReadFileText(fakePath).Returns(fakeLink);

            await handler.TourneyCommand(mockClient, "slyta123", fakePath);
            await handler.TourneyCommand(mockClient, "slyta123", fakePath);

            mockClient.Received(1).Say(fakeLink);
            mockClient.Received(1).Log("tourney is on cooldown.");
        }

        [Fact]
        public void LurkCommand_WhenNotOnCooldown_SendsFriendlyResponse()
        {
            var mockClient = Substitute.For<ITwitchClientWrapper>();
            var handler = new TwitchCommandHandler();

            handler.LurkCommand(mockClient, "slyta123");

            mockClient.Received(1).Say("I see you big dog!");
        }

        [Fact]

        public void LurkCommand_WhenOnCooldown_DoesNotSendResponse()
        {
            var mockClient = Substitute.For<ITwitchClientWrapper>();
            var handler = new TwitchCommandHandler();

            handler.LurkCommand(mockClient, "slyta123");
            handler.LurkCommand(mockClient, "slyta123");

            mockClient.Received(1).Say("I see you big dog!");
            mockClient.Received(1).Log("lurk is on cooldown.");
        }

        [Fact]
        public async Task SetIDCommand_WhenPathExists_WritesNewIdAndAnnounces()
        {
            var mockClient = Substitute.For<ITwitchClientWrapper>();
            var handler = new TwitchCommandHandler();
            string fakePath = "Misc/arena ID.txt";

            mockClient.FileExists(fakePath).Returns(true);

            await handler.SetIDCommand(mockClient, "slyta123", "NEW_ARENA_123", fakePath);

            mockClient.Received(1).WriteFileText(fakePath, "NEW_ARENA_123");
            mockClient.Received(1).Say("ID updated to: NEW_ARENA_123");
            mockClient.Received(1).LogCommand("slyta123", "setid");
        }

        [Fact]

        public async Task SetIDCommand_WhenPathNotExists_SayDefaultAndLogError()
        {
            var mockClient = Substitute.For<ITwitchClientWrapper>();
            var handler = new TwitchCommandHandler();
            string fakePath = "Misc/arena ID.txt";

            mockClient.FileExists(fakePath).Returns(false);

            await handler.SetIDCommand(mockClient, "slyta123", "NEW_ARENA_123", fakePath);

            mockClient.Received(1).Say("Error! Contact host!");
            mockClient.Received(1).Log("ATTENTION: Arena ID file missing. Check root. Warning command user.");
            mockClient.DidNotReceive().WriteFileText(Arg.Any<string>(), Arg.Any<string>());
        }

        [Fact]
        public void OpenArenaCommand_FiresArenaOpenEventAndLogs()
        {

            var mockClient = Substitute.For<ITwitchClientWrapper>();
            var handler = new TwitchCommandHandler();
            string fakePath = "Misc/arena ID.txt";
            string fakeId = "ARENA_CODE_99";

            mockClient.ReadFileText(fakePath).Returns(fakeId);


            string receivedEventMessage = null;
            handler.ArenaOpen += (sender, eventArgs) => receivedEventMessage = eventArgs;

            handler.OpenArenaCommand(mockClient, "slyta123", fakePath);


            Assert.NotNull(receivedEventMessage);
            Assert.Contains(fakeId, receivedEventMessage);
            Assert.Contains("<@&1514411853466308669>", receivedEventMessage); 

            mockClient.Received(1).Say("Sending arena info to the discord server!");
            mockClient.Received(1).LogCommand("slyta123", "openarena");
        }

        [Fact]
        public async Task FollowageCommand_WhenUserFollows_SendsFormattedDuration()
        {
            // Arrange
            var mockClient = Substitute.For<ITwitchClientWrapper>();
            var handler = new TwitchCommandHandler();

            string viewerId = "12345";
            string username = "test_viewer";
            string streamerId = "67890";

            // Simulate following for exactly 2 years, 3 months, and 5 days
            DateTime fakeFollowDate = DateTime.UtcNow.AddDays(-825);
            mockClient.GetFollowDateAsync(streamerId, viewerId).Returns(Task.FromResult((DateTime?)fakeFollowDate));

            // Act
            await handler.FollowageCommand(mockClient, viewerId, username, streamerId);

            // Assert
            // Looking for the formatted string output by FormatTimeSpan
            mockClient.Received(1).Say("test_viewer, you have been following for 2 years, 3 months, 5 days!");
            mockClient.Received(1).LogCommand(viewerId, "followage");
        }

        [Fact]
        public async Task FollowageCommand_WhenUserIsStreamer_SaysCannotFollowSelf()
        {
            // Arrange
            var mockClient = Substitute.For<ITwitchClientWrapper>();
            var handler = new TwitchCommandHandler();

            string streamerId = "67890";
            string username = "remnapi";

            // Act - Pass matching IDs for viewer and streamer
            await handler.FollowageCommand(mockClient, streamerId, username, streamerId);

            // Assert
            mockClient.Received(1).Say("You can't follow yourself!");
            // The command short-circuits, so it shouldn't hit the Twitch API or log to the DB
            await mockClient.DidNotReceive().GetFollowDateAsync(Arg.Any<string>(), Arg.Any<string>());
        }

        [Fact]
        public async Task FollowageCommand_WhenUserDoesNotFollow_SaysNotFollowing()
        {

            var mockClient = Substitute.For<ITwitchClientWrapper>();
            var handler = new TwitchCommandHandler();

            string viewerId = "12345";
            string username = "test_viewer";
            string streamerId = "67890";

            mockClient.GetFollowDateAsync(streamerId, viewerId).Returns(Task.FromResult((DateTime?)null));

            await handler.FollowageCommand(mockClient, viewerId, username, streamerId);

            mockClient.Received(1).Say("test_viewer doesn't follow this channel!");
            mockClient.Received(1).LogCommand(viewerId, "followage");
        }

        [Fact]
        public async Task FollowageCommand_OnApiError_LogsErrorAndSendsFriendlyWarning()
        {

            var mockClient = Substitute.For<ITwitchClientWrapper>();
            var handler = new TwitchCommandHandler();

            string viewerId = "12345";
            string username = "test_viewer";
            string streamerId = "67890";

            mockClient.GetFollowDateAsync(streamerId, viewerId).ThrowsAsync(new Exception("Unauthorized scope"));

            await handler.FollowageCommand(mockClient, viewerId, username, streamerId);

            mockClient.Received(1).Log("Followage API Error: Unauthorized scope");
            mockClient.Received(1).Say("I don't have permission to see followers! Make sure I'm a mod and have the right scopes.");
            mockClient.Received(1).LogCommand(viewerId, "followage");
        }

        [Fact]
        public async Task FollowageCommand_WhenOnCooldown_DoesNotCallApiOrSendResponse()
        {
            
            var mockClient = Substitute.For<ITwitchClientWrapper>();
            var handler = new TwitchCommandHandler();

            string viewerId = "12345";
            string username = "test_viewer";
            string streamerId = "67890";

            
            DateTime fakeFollowDate = DateTime.UtcNow.AddDays(-10);
            mockClient.GetFollowDateAsync(streamerId, viewerId).Returns(Task.FromResult((DateTime?)fakeFollowDate));

            
            await handler.FollowageCommand(mockClient, viewerId, username, streamerId);
            await handler.FollowageCommand(mockClient, viewerId, username, streamerId);


            mockClient.Received(1).Say(Arg.Is<string>(s => s.Contains("test_viewer, you have been following for")));

            mockClient.Received(1).Log("followage is on cooldown.");


            await mockClient.Received(1).GetFollowDateAsync(streamerId, viewerId);
        }
    }
}
