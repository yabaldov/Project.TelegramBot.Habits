using FluentAssertions;
using HabiBot.Application.DTOs;
using HabiBot.Application.Services;
using HabiBot.Bot.Commands;
using HabiBot.Bot.StateManagement;
using HabiBot.Domain.Entities;
using HabiBot.Infrastructure.Models.Telegram;
using HabiBot.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace HabiBot.Tests.Bot;

public class StartCommandTests
{
    private readonly Mock<ITelegramApiClient> _telegramClientMock;
    private readonly Mock<IUserStateManager> _stateManagerMock;
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<ILogger<StartCommand>> _loggerMock;
    private readonly StartCommand _startCommand;

    public StartCommandTests()
    {
        _telegramClientMock = new Mock<ITelegramApiClient>();
        _stateManagerMock = new Mock<IUserStateManager>();
        _userServiceMock = new Mock<IUserService>();
        _loggerMock = new Mock<ILogger<StartCommand>>();

        _startCommand = new StartCommand(
            _telegramClientMock.Object,
            _stateManagerMock.Object,
            _userServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public void StartCommand_ShouldHaveCorrectNameAndDescription()
    {
        // Assert
        _startCommand.Name.Should().Be("start");
        _startCommand.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldWelcomeExistingUser_WhenUserAlreadyRegistered()
    {
        // Arrange
        var userId = 123456789L;
        var chatId = 123456789L;
        var update = CreateUpdate(userId, chatId, "/start");

        var existingUser = new User
        {
            Id = 1,
            TelegramChatId = userId,
            Name = "Алексей"
        };

        _userServiceMock
            .Setup(s => s.GetByTelegramIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        // Act
        await _startCommand.ExecuteAsync(update);

        // Assert
        _telegramClientMock.Verify(
            c => c.SendMessageAsync(
                It.Is<SendMessageRequest>(r => r.ChatId == chatId && r.Text.Contains("Алексей")),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _stateManagerMock.Verify(s => s.ClearState(userId), Times.Once);
        _stateManagerMock.Verify(s => s.SetState(userId, It.IsAny<UserState>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldStartRegistration_WhenUserNotRegistered()
    {
        // Arrange
        var userId = 123456789L;
        var chatId = 123456789L;
        var update = CreateUpdate(userId, chatId, "/start");

        _userServiceMock
            .Setup(s => s.GetByTelegramIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        await _startCommand.ExecuteAsync(update);

        // Assert
        _telegramClientMock.Verify(
            c => c.SendMessageAsync(
                It.Is<SendMessageRequest>(r => r.ChatId == chatId && r.Text.Contains("называть")),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _stateManagerMock.Verify(s => s.SetState(userId, UserState.WaitingForName), Times.Once);
    }

    [Fact]
    public async Task HandleNameInputAsync_ShouldCreateUser_WhenValidName()
    {
        // Arrange
        var userId = 123456789L;
        var chatId = 123456789L;
        var name = "Алексей";
        var update = CreateUpdate(userId, chatId, name);

        var createdUser = new User
        {
            Id = 1,
            TelegramUserId = userId,
            TelegramChatId = chatId,
            Name = name
        };

        _userServiceMock
            .Setup(s => s.CreateUserAsync(It.IsAny<CreateUserDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdUser);

        // Act
        await _startCommand.HandleNameInputAsync(update, name);

        // Assert
        _userServiceMock.Verify(
            s => s.CreateUserAsync(
                It.Is<CreateUserDto>(dto => dto.TelegramUserId == userId && dto.TelegramChatId == chatId && dto.Name == name),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _telegramClientMock.Verify(
            c => c.SendMessageAsync(
                It.Is<SendMessageRequest>(r => r.ChatId == chatId && r.Text.Contains("Алексей")),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _stateManagerMock.Verify(s => s.ClearState(userId), Times.Once);
    }

    [Fact]
    public async Task HandleNameInputAsync_ShouldRejectInvalidName_WhenNameTooLong()
    {
        // Arrange
        var userId = 123456789L;
        var chatId = 123456789L;
        var name = new string('А', 101); // 101 символ
        var update = CreateUpdate(userId, chatId, name);

        // Act
        await _startCommand.HandleNameInputAsync(update, name);

        // Assert
        _userServiceMock.Verify(
            s => s.CreateUserAsync(It.IsAny<CreateUserDto>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _telegramClientMock.Verify(
            c => c.SendMessageAsync(
                It.Is<SendMessageRequest>(r => r.ChatId == chatId && r.Text.Contains("100 символов")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static Update CreateUpdate(long userId, long chatId, string text)
    {
        return new Update
        {
            UpdateId = 1,
            Message = new Message
            {
                MessageId = 1,
                From = new TelegramUser { Id = userId, FirstName = "Test" },
                Chat = new Chat { Id = chatId, Type = "private" },
                Text = text,
                Date = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            }
        };
    }
}
