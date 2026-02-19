using FluentAssertions;
using HabiBot.Application.Services;
using HabiBot.Bot.Commands;
using HabiBot.Bot.StateManagement;
using HabiBot.Domain.Entities;
using HabiBot.Infrastructure.Models.Telegram;
using HabiBot.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace HabiBot.Tests.Bot;

public class SetSummaryCommandTests
{
    private readonly Mock<ITelegramApiClient> _telegramClientMock;
    private readonly Mock<IUserStateManager> _stateManagerMock;
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<ILogger<SetSummaryCommand>> _loggerMock;
    private readonly SetSummaryCommand _setSummaryCommand;

    public SetSummaryCommandTests()
    {
        _telegramClientMock = new Mock<ITelegramApiClient>();
        _stateManagerMock = new Mock<IUserStateManager>();
        _userServiceMock = new Mock<IUserService>();
        _loggerMock = new Mock<ILogger<SetSummaryCommand>>();

        _setSummaryCommand = new SetSummaryCommand(
            _telegramClientMock.Object,
            _stateManagerMock.Object,
            _userServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public void SetSummaryCommand_ShouldHaveCorrectName()
    {
        _setSummaryCommand.Name.Should().Be("setsummary");
        _setSummaryCommand.Description.Should().NotBeNullOrWhiteSpace();
    }

    #region ExecuteAsync

    [Fact]
    public async Task ExecuteAsync_ShouldAskForRegistration_WhenUserNotRegistered()
    {
        // Arrange
        var update = CreateUpdate(123, 123, "/setsummary");

        _userServiceMock
            .Setup(s => s.GetByTelegramIdAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        await _setSummaryCommand.ExecuteAsync(update);

        // Assert
        _telegramClientMock.Verify(
            c => c.SendMessageAsync(
                It.Is<SendMessageRequest>(r => r.ChatId == 123 && r.Text.Contains("не зарегистрирован")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldShowSettings_WhenSummaryEnabled()
    {
        // Arrange
        var update = CreateUpdate(123, 123, "/setsummary");
        var user = new User
        {
            Id = 1,
            TelegramUserId = 123,
            Name = "Test",
            IsDailySummaryEnabled = true,
            DailySummaryTime = new TimeSpan(21, 0, 0)
        };

        _userServiceMock
            .Setup(s => s.GetByTelegramIdAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        await _setSummaryCommand.ExecuteAsync(update);

        // Assert
        _telegramClientMock.Verify(
            c => c.SendMessageAsync(
                It.Is<SendMessageRequest>(r =>
                    r.ChatId == 123 &&
                    r.Text.Contains("включена ✅") &&
                    r.Text.Contains("21:00") &&
                    r.ReplyMarkup != null),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldShowDisabledStatus_WhenSummaryDisabled()
    {
        // Arrange
        var update = CreateUpdate(123, 123, "/setsummary");
        var user = new User
        {
            Id = 1,
            TelegramUserId = 123,
            Name = "Test",
            IsDailySummaryEnabled = false,
            DailySummaryTime = new TimeSpan(21, 0, 0)
        };

        _userServiceMock
            .Setup(s => s.GetByTelegramIdAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        await _setSummaryCommand.ExecuteAsync(update);

        // Assert
        _telegramClientMock.Verify(
            c => c.SendMessageAsync(
                It.Is<SendMessageRequest>(r =>
                    r.ChatId == 123 &&
                    r.Text.Contains("отключена ❌")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region HandleCallbackAsync

    [Fact]
    public async Task HandleCallbackAsync_Time_ShouldSetWaitingState()
    {
        // Arrange
        var user = new User { Id = 1, TelegramUserId = 123, Name = "Test" };

        _userServiceMock
            .Setup(s => s.GetByTelegramIdAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        await _setSummaryCommand.HandleCallbackAsync(123, 123, "time");

        // Assert
        _stateManagerMock.Verify(
            s => s.SetState(123, UserState.WaitingForSummaryTime),
            Times.Once);

        _telegramClientMock.Verify(
            c => c.SendMessageAsync(
                It.Is<SendMessageRequest>(r => r.Text.Contains("формате ЧЧ:ММ")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleCallbackAsync_Disable_ShouldDisableSummary()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            TelegramUserId = 123,
            Name = "Test",
            IsDailySummaryEnabled = true
        };

        _userServiceMock
            .Setup(s => s.GetByTelegramIdAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        await _setSummaryCommand.HandleCallbackAsync(123, 123, "disable");

        // Assert
        _userServiceMock.Verify(
            s => s.UpdateDailySummarySettingsAsync(1, false, null, It.IsAny<CancellationToken>()),
            Times.Once);

        _telegramClientMock.Verify(
            c => c.SendMessageAsync(
                It.Is<SendMessageRequest>(r => r.Text.Contains("отключена")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleCallbackAsync_Enable_ShouldEnableSummary()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            TelegramUserId = 123,
            Name = "Test",
            IsDailySummaryEnabled = false,
            DailySummaryTime = new TimeSpan(20, 30, 0)
        };

        _userServiceMock
            .Setup(s => s.GetByTelegramIdAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        await _setSummaryCommand.HandleCallbackAsync(123, 123, "enable");

        // Assert
        _userServiceMock.Verify(
            s => s.UpdateDailySummarySettingsAsync(1, true, new TimeSpan(20, 30, 0), It.IsAny<CancellationToken>()),
            Times.Once);

        _telegramClientMock.Verify(
            c => c.SendMessageAsync(
                It.Is<SendMessageRequest>(r => r.Text.Contains("включена")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleCallbackAsync_Enable_ShouldUseDefaultTime_WhenNoTimeSet()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            TelegramUserId = 123,
            Name = "Test",
            IsDailySummaryEnabled = false,
            DailySummaryTime = null
        };

        _userServiceMock
            .Setup(s => s.GetByTelegramIdAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        await _setSummaryCommand.HandleCallbackAsync(123, 123, "enable");

        // Assert
        _userServiceMock.Verify(
            s => s.UpdateDailySummarySettingsAsync(1, true, new TimeSpan(21, 0, 0), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleCallbackAsync_Cancel_ShouldClearState()
    {
        // Arrange
        var user = new User { Id = 1, TelegramUserId = 123, Name = "Test" };

        _userServiceMock
            .Setup(s => s.GetByTelegramIdAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        await _setSummaryCommand.HandleCallbackAsync(123, 123, "cancel");

        // Assert
        _stateManagerMock.Verify(s => s.ClearState(123), Times.Once);
    }

    [Fact]
    public async Task HandleCallbackAsync_ShouldHandleUserNotFound()
    {
        // Arrange
        _userServiceMock
            .Setup(s => s.GetByTelegramIdAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        await _setSummaryCommand.HandleCallbackAsync(100, 123, "enable");

        // Assert
        _telegramClientMock.Verify(
            c => c.SendMessageAsync(
                It.Is<SendMessageRequest>(r => r.Text.Contains("не найден")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region HandleSummaryTimeInputAsync

    [Fact]
    public async Task HandleSummaryTimeInputAsync_ShouldUpdateTime_WhenValidFormat()
    {
        // Arrange
        _stateManagerMock
            .Setup(s => s.GetData<long>(123, "UserId"))
            .Returns(1);

        // Act
        await _setSummaryCommand.HandleSummaryTimeInputAsync(123, 123, "08:30");

        // Assert
        _userServiceMock.Verify(
            s => s.UpdateDailySummarySettingsAsync(1, true, new TimeSpan(8, 30, 0), It.IsAny<CancellationToken>()),
            Times.Once);

        _stateManagerMock.Verify(s => s.ClearState(123), Times.Once);

        _telegramClientMock.Verify(
            c => c.SendMessageAsync(
                It.Is<SendMessageRequest>(r => r.Text.Contains("08:30")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData("21:00")]
    [InlineData("0:00")]
    [InlineData("23:59")]
    [InlineData("9:05")]
    public async Task HandleSummaryTimeInputAsync_ShouldAcceptValidTimeFormats(string time)
    {
        // Arrange
        _stateManagerMock
            .Setup(s => s.GetData<long>(123, "UserId"))
            .Returns(1);

        // Act
        await _setSummaryCommand.HandleSummaryTimeInputAsync(123, 123, time);

        // Assert
        _userServiceMock.Verify(
            s => s.UpdateDailySummarySettingsAsync(1, true, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData("25:00")]
    [InlineData("12:60")]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("12")]
    [InlineData("12:00:00")]
    public async Task HandleSummaryTimeInputAsync_ShouldRejectInvalidTimeFormats(string time)
    {
        // Arrange
        _stateManagerMock
            .Setup(s => s.GetData<long>(123, "UserId"))
            .Returns(1);

        // Act
        await _setSummaryCommand.HandleSummaryTimeInputAsync(123, 123, time);

        // Assert
        _userServiceMock.Verify(
            s => s.UpdateDailySummarySettingsAsync(It.IsAny<long>(), It.IsAny<bool>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _telegramClientMock.Verify(
            c => c.SendMessageAsync(
                It.Is<SendMessageRequest>(r => r.Text.Contains("Неверный формат")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleSummaryTimeInputAsync_ShouldClearState_WhenUserIdNotFound()
    {
        // Arrange
        _stateManagerMock
            .Setup(s => s.GetData<long>(123, "UserId"))
            .Returns(0L);

        // Act
        await _setSummaryCommand.HandleSummaryTimeInputAsync(123, 123, "21:00");

        // Assert
        _stateManagerMock.Verify(s => s.ClearState(123), Times.Once);
    }

    #endregion

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
