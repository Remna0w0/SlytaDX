using Discord;
using NSubstitute;
using RemnaBotService.DiscordBot;
using System;
using System.Threading.Tasks;
using Xunit;
using static RemnaBotService.DiscordBot.DiscordCommandHandler;


namespace RemnaBotService.SlytaTest
{
    public class DiscordCommandHandlerTests
    {
        [Fact]
        public async Task PingCommand_WhenNotOnCooldown_SendsPongAndLogs()
        {
            var mockClient = Substitute.For<IDiscordClientWrapper>();
            var mockMessage = Substitute.For<IUserMessage>();
            var mockChannel = Substitute.For<IMessageChannel>();

            mockMessage.Channel.Returns(mockChannel);
            mockMessage.Author.Id.Returns(123456789UL);

            var handler = new DiscordCommandHandler();

            await handler.PingCommand(mockClient, mockMessage);

            await mockClient.Received(1).Say(mockChannel, "Pong!");

            mockClient.Received(1).LogCommand("123456789", "ping");
        }

         [Fact]

         public async Task PingCommand_WhenOnCooldown_DoesNotSendPong()
        {
            var mockClient = Substitute.For<IDiscordClientWrapper>();
            var mockMessage = Substitute.For<IUserMessage>();
            var mockChannel = Substitute.For<IMessageChannel>();

            mockMessage.Channel.Returns(mockChannel);
            mockMessage.Author.Id.Returns(123456789UL);

            var handler = new DiscordCommandHandler();

            await handler.PingCommand(mockClient, mockMessage);
            await handler.PingCommand(mockClient, mockMessage);

            await mockClient.Received(1).Say(mockChannel, "Pong!");

            mockClient.Received(1).Log("ping is on cooldown.");
        }

        [Fact]
        
        public async Task RoleSpawnCommand_WhenUserIsNotAdmin_LogAndReturnEarly()
        {
            var mockClient = Substitute.For<IDiscordClientWrapper>();
            var mockMessage = Substitute.For<IUserMessage>();
            var mockUser = Substitute.For<IGuildUser>();

            mockUser.GuildPermissions.Returns(new GuildPermissions(administrator: false));
            mockMessage.Author.Returns(mockUser);

            var handler = new DiscordCommandHandler();

            await handler.RoleSpawnCommand(mockClient, mockMessage);

            mockClient.Received(1).Log("Non-admin attempted to spawn role buttons. Command ignored.");

            await mockMessage.Channel.DidNotReceive().SendMessageAsync(
                Arg.Any<string>(),
                components: Arg.Any<MessageComponent>()
                );
        }

        [Fact]

        public async Task RoleSpawnCommand_WhenUserIsAdmin_SendMessageWithCorrectButtons()
        {
            var mockClient = Substitute.For<IDiscordClientWrapper>();
            var mockMessage = Substitute.For<IUserMessage>();
            var mockChannel = Substitute.For<IMessageChannel>();
            var mockUser = Substitute.For<IGuildUser>();

            mockUser.GuildPermissions.Returns(new GuildPermissions(administrator: true));
            mockMessage.Author.Returns(mockUser);
            mockMessage.Channel.Returns(mockChannel);

            var handler = new DiscordCommandHandler();

            await handler.RoleSpawnCommand(mockClient, mockMessage);

            await mockChannel.Received(1).SendMessageAsync(
                "Welcome! Click the buttons to add or remove roles:",
                components: Arg.Is<MessageComponent>(components =>

                components.Components
                .OfType<ActionRowComponent>()
                .SelectMany(row => row.Components)
                .OfType<ButtonComponent>()
                .Any(c => c.CustomId == "role_he") &&

                components.Components
                .OfType<ActionRowComponent>()
                .SelectMany(row => row.Components)
                .OfType<ButtonComponent>()
                .Any(c => c.CustomId == "role_she") &&


                components.Components
                .OfType<ActionRowComponent>()
                .SelectMany(row => row.Components)
                .OfType<ButtonComponent>()
                .Any(c => c.CustomId == "role_viewer")

                )
             );
        }

        [Fact]

        public async Task DragonCommand_WhenOnCooldown_LogAndReturnEarly()
        {
            var mockClient = Substitute.For<IDiscordClientWrapper>();
            var mockMessage = Substitute.For<IUserMessage>();
            var mockChannel = Substitute.For<IMessageChannel>();

            mockClient.IsGameActive("Slyta").Returns(false);
            mockMessage.Channel.Returns(mockChannel);
            mockMessage.Author.Id.Returns(123456789UL);

            var handler = new DiscordCommandHandler();

            await handler.DragonCommand(mockClient, mockMessage, "Slyta");
            await handler.DragonCommand(mockClient, mockMessage, "Slyta");

            await mockClient.Received(1).Say(mockChannel, "Dragon game started!");

            mockClient.Received(1).Log("dragon is on cooldown.");


        }

        [Fact]

        public async Task DragonCommand_WhenOffCooldown_WhenUserGameExists_AlertUserAndDoesNotDuplicate()
        {
            var mockClient = Substitute.For<IDiscordClientWrapper>();
            var mockMessage = Substitute.For<IUserMessage>();
            var mockChannel = Substitute.For<IMessageChannel>();

            mockClient.IsGameActive("Slyta").Returns(true);
            mockMessage.Channel.Returns(mockChannel);
            mockMessage.Author.Id.Returns(123456789UL);

            var handler = new DiscordCommandHandler();

            await handler.DragonCommand(mockClient, mockMessage, "Slyta");

            mockClient.DidNotReceive().StartDragonGame(Arg.Any<string>(), Arg.Any<IMessageChannel>());
            await mockClient.Received(1).Say(mockChannel, "Slyta, you have already started a game!");

            mockClient.Received(1).LogCommand("123456789", "dragon");
        }


        [Fact]

        public async Task DragonCommand_WhenOffCooldown_WhenUserGameNotExists_StartGameAndAlertUser()
        {
            var mockClient = Substitute.For<IDiscordClientWrapper>();
            var mockMessage = Substitute.For<IUserMessage>();
            var mockChannel = Substitute.For<IMessageChannel>();

            mockClient.IsGameActive("Slyta").Returns(false);
            mockMessage.Channel.Returns(mockChannel);
            mockMessage.Author.Id.Returns(123456789UL);

            var handler = new DiscordCommandHandler();

            await handler.DragonCommand(mockClient, mockMessage, "Slyta");

            mockClient.Received(1).StartDragonGame("Slyta", mockChannel);
            await mockClient.Received(1).Say(mockChannel, "Dragon game started!");
            mockClient.Received(1).LogCommand("123456789", "dragon");
        }
    }
}