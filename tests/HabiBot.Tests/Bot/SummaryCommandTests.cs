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

public class SummaryCommandTests
{
    private readonly Mock<ITelegramApiClient> _telegramClientMock;
    private readonly Mock<IUserStateManager> _stateManagerMock;
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<IDailySummaryService> _dailySummaryServiceMock;
    private readonly Mock<ILogger<SummaryCommand>> _loggerMock;
    private readonly SummaryCommand _summaryCommand;

    public SummaryCommandTests()
    {
        _telegramClientMock = new Mock<ITelegramApiClient>();
        _stateManagerMock = new Mock<IUserStateManager>();
        _userServiceMock = new Mock<IUserService>();
        _dailySummaryServiceMock = new Mock<IDailySummaryService>();
        _loggerMock = new Mock<ILogger<SummaryCommand>>();

        _summaryCommand = new SummaryCommand(
            _telegramClientMock.Object,
            _stateManagerMock.Object,
            _userServiceMock.Object,
            _dailySummaryServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public void SummaryCommand_ShouldHaveCorrectName()
    {
        _summaryCommand.Name.Should().Be("summary");
        _summaryCommand.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldAskForRegistration_WhenUserNotRegistered()
    {
        // Arrange
        var update = CreateUpdate(123, 123, "/summary");

        _userServiceMock
            .Setup(s => s.GetByTelegramIdAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        await _summaryCommand.ExecuteAsync(update);

        // Assert
        _telegramClientMock.Verify(
            c => c.SendMessageAsync(
                It.Is<SendMessageRequest>(r => r.ChatId == 123 && r.Text.Contains("не зарегистрирован")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSendSummary_WhenUserRegistered()
    {
        // Arrange
        var update = CreateUpdate(123, 123, "/summary");
        var user = new User { Id = 1, TelegramUserId = 123, Name = "Test" };

        _userServiceMock
            .Setup(s => s.GetByTelegramIdAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _dailySummaryServiceMock
            .Setup(s => s.GenerateSummaryTextAsync(1, It.IsAny<DateOnly>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync("<b>Сводка</b>");

        // Act
        await _summaryCommand.ExecuteAsync(update);

        // Assert
        _telegramClientMock.Verify(
            c => c.SendMessageAsync(
                It.Is<SendMessageRequest>(r => r.ChatId == 123 && r.ParseMode == "HTML"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCallServiceWithoutNextDay()
    {
        // Arrange
        var update = CreateUpdate(123, 123, "/summary");
        var user = new User { Id = 1, TelegramUserId = 123, Name = "Test" };

        _userServiceMock
            .Setup(s => s.GetByTelegramIdAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _dailySummaryServiceMock
            .Setup(s => s.GenerateSummaryTextAsync(1, It.IsAny<DateOnly>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Сводка");

        // Act
        await _summaryCommand.ExecuteAsync(update);

        // Assert
        _dailySummaryServiceMock.Verify(
            s => s.GenerateSummaryTextAsync(1, It.IsAny<DateOnly>(), false, It.IsAny<CancellationToken>()),
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
