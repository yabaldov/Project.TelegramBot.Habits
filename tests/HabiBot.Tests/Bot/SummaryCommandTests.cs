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

public class SummaryCommandTests
{
    private readonly Mock<ITelegramApiClient> _telegramClientMock;
    private readonly Mock<IUserStateManager> _stateManagerMock;
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<IDailySummaryService> _dailySummaryServiceMock;
    private readonly Mock<IHabitLogService> _habitLogServiceMock;
    private readonly Mock<IHabitService> _habitServiceMock;
    private readonly Mock<ILogger<SummaryCommand>> _loggerMock;
    private readonly SummaryCommand _summaryCommand;

    public SummaryCommandTests()
    {
        _telegramClientMock = new Mock<ITelegramApiClient>();
        _stateManagerMock = new Mock<IUserStateManager>();
        _userServiceMock = new Mock<IUserService>();
        _dailySummaryServiceMock = new Mock<IDailySummaryService>();
        _habitLogServiceMock = new Mock<IHabitLogService>();
        _habitServiceMock = new Mock<IHabitService>();
        _loggerMock = new Mock<ILogger<SummaryCommand>>();

        _summaryCommand = new SummaryCommand(
            _telegramClientMock.Object,
            _stateManagerMock.Object,
            _userServiceMock.Object,
            _dailySummaryServiceMock.Object,
            _habitLogServiceMock.Object,
            _habitServiceMock.Object,
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
        var summaryData = new DailySummaryData { UserId = 1 };

        _userServiceMock
            .Setup(s => s.GetByTelegramIdAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _dailySummaryServiceMock
            .Setup(s => s.GetDailySummaryAsync(1, It.IsAny<DateOnly>(), It.IsAny<TimeOnly?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(summaryData);

        _dailySummaryServiceMock
            .Setup(s => s.GenerateSummaryTextAsync(1, It.IsAny<DateOnly>(), false, It.IsAny<TimeOnly?>(), It.IsAny<CancellationToken>()))
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
        var summaryData = new DailySummaryData { UserId = 1 };

        _userServiceMock
            .Setup(s => s.GetByTelegramIdAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _dailySummaryServiceMock
            .Setup(s => s.GetDailySummaryAsync(1, It.IsAny<DateOnly>(), It.IsAny<TimeOnly?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(summaryData);

        _dailySummaryServiceMock
            .Setup(s => s.GenerateSummaryTextAsync(1, It.IsAny<DateOnly>(), false, It.IsAny<TimeOnly?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Сводка");

        // Act
        await _summaryCommand.ExecuteAsync(update);

        // Assert
        _dailySummaryServiceMock.Verify(
            s => s.GenerateSummaryTextAsync(1, It.IsAny<DateOnly>(), false, It.IsAny<TimeOnly?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldIncludeKeyboard_WhenUncompletedHabitsExist()
    {
        // Arrange
        var update = CreateUpdate(123, 123, "/summary");
        var user = new User { Id = 1, TelegramUserId = 123, Name = "Test" };
        var summaryData = new DailySummaryData
        {
            UserId = 1,
            UncompletedHabits = new List<UncompletedHabitInfo>
            {
                new() { HabitId = 10, HabitName = "Медитация", ScheduledTime = "08:00" },
                new() { HabitId = 11, HabitName = "Чтение", ScheduledTime = "20:00" }
            },
            AvailableToCompleteToday = new List<UncompletedHabitInfo>
            {
                new() { HabitId = 10, HabitName = "Медитация", ScheduledTime = "08:00" },
                new() { HabitId = 11, HabitName = "Чтение", ScheduledTime = "20:00" }
            }
        };

        _userServiceMock
            .Setup(s => s.GetByTelegramIdAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _dailySummaryServiceMock
            .Setup(s => s.GetDailySummaryAsync(1, It.IsAny<DateOnly>(), It.IsAny<TimeOnly?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(summaryData);

        _dailySummaryServiceMock
            .Setup(s => s.GenerateSummaryTextAsync(1, It.IsAny<DateOnly>(), false, It.IsAny<TimeOnly?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Сводка");

        // Act
        await _summaryCommand.ExecuteAsync(update);

        // Assert
        _telegramClientMock.Verify(
            c => c.SendMessageAsync(
                It.Is<SendMessageRequest>(r => r.ReplyMarkup != null && ((InlineKeyboardMarkup)r.ReplyMarkup).InlineKeyboard.Count() == 2),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotIncludeKeyboard_WhenAllHabitsCompleted()
    {
        // Arrange
        var update = CreateUpdate(123, 123, "/summary");
        var user = new User { Id = 1, TelegramUserId = 123, Name = "Test" };
        var summaryData = new DailySummaryData { UserId = 1 }; // no uncompleted habits

        _userServiceMock
            .Setup(s => s.GetByTelegramIdAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _dailySummaryServiceMock
            .Setup(s => s.GetDailySummaryAsync(1, It.IsAny<DateOnly>(), It.IsAny<TimeOnly?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(summaryData);

        _dailySummaryServiceMock
            .Setup(s => s.GenerateSummaryTextAsync(1, It.IsAny<DateOnly>(), false, It.IsAny<TimeOnly?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Сводка");

        // Act
        await _summaryCommand.ExecuteAsync(update);

        // Assert
        _telegramClientMock.Verify(
            c => c.SendMessageAsync(
                It.Is<SendMessageRequest>(r => r.ReplyMarkup == null),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #region HandleCompleteCallbackAsync

    [Fact]
    public async Task HandleCompleteCallbackAsync_ShouldMarkHabitCompleted()
    {
        // Arrange
        var user = new User { Id = 1, TelegramUserId = 123, Name = "Test" };
        var habit = new Habit { Id = 10, UserId = 1, Name = "Медитация" };

        _userServiceMock
            .Setup(s => s.GetByTelegramIdAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _habitServiceMock
            .Setup(s => s.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(habit);

        _habitLogServiceMock
            .Setup(s => s.CreateLogAsync(It.IsAny<CreateHabitLogDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HabitLog { Id = 1, HabitId = 10 });

        // Act
        await _summaryCommand.HandleCompleteCallbackAsync(123, 123, 10);

        // Assert
        _habitLogServiceMock.Verify(
            s => s.CreateLogAsync(
                It.Is<CreateHabitLogDto>(d => d.HabitId == 10),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _telegramClientMock.Verify(
            c => c.SendMessageAsync(
                It.Is<SendMessageRequest>(r => r.Text.Contains("Медитация") && r.Text.Contains("🎉")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleCompleteCallbackAsync_ShouldRejectUnknownHabit()
    {
        // Arrange
        var user = new User { Id = 1, TelegramUserId = 123, Name = "Test" };

        _userServiceMock
            .Setup(s => s.GetByTelegramIdAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _habitServiceMock
            .Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Habit?)null);

        // Act
        await _summaryCommand.HandleCompleteCallbackAsync(123, 123, 999);

        // Assert
        _habitLogServiceMock.Verify(
            s => s.CreateLogAsync(It.IsAny<CreateHabitLogDto>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _telegramClientMock.Verify(
            c => c.SendMessageAsync(
                It.Is<SendMessageRequest>(r => r.Text.Contains("не найдена")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleCompleteCallbackAsync_ShouldRejectHabitOfAnotherUser()
    {
        // Arrange
        var user = new User { Id = 1, TelegramUserId = 123, Name = "Test" };
        var otherUserHabit = new Habit { Id = 10, UserId = 999, Name = "Чужая" };

        _userServiceMock
            .Setup(s => s.GetByTelegramIdAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _habitServiceMock
            .Setup(s => s.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(otherUserHabit);

        // Act
        await _summaryCommand.HandleCompleteCallbackAsync(123, 123, 10);

        // Assert
        _habitLogServiceMock.Verify(
            s => s.CreateLogAsync(It.IsAny<CreateHabitLogDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleCompleteCallbackAsync_ShouldHandleUnregisteredUser()
    {
        // Arrange
        _userServiceMock
            .Setup(s => s.GetByTelegramIdAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        await _summaryCommand.HandleCompleteCallbackAsync(123, 123, 10);

        // Assert
        _telegramClientMock.Verify(
            c => c.SendMessageAsync(
                It.Is<SendMessageRequest>(r => r.Text.Contains("не найден")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region BuildUncompletedHabitsKeyboard

    [Fact]
    public void BuildUncompletedHabitsKeyboard_ShouldReturnNull_WhenNoUncompleted()
    {
        var summary = new DailySummaryData { UserId = 1 };

        var result = SummaryCommand.BuildUncompletedHabitsKeyboard(summary);

        result.Should().BeNull();
    }

    [Fact]
    public void BuildUncompletedHabitsKeyboard_ShouldBuildButtons_ForEachUncompletedHabit()
    {
        var summary = new DailySummaryData
        {
            UserId = 1,
            UncompletedHabits = new List<UncompletedHabitInfo>
            {
                new() { HabitId = 10, HabitName = "Медитация", ScheduledTime = "08:00" },
                new() { HabitId = 11, HabitName = "Чтение", ScheduledTime = "20:00" }
            },
            AvailableToCompleteToday = new List<UncompletedHabitInfo>
            {
                new() { HabitId = 10, HabitName = "Медитация", ScheduledTime = "08:00" },
                new() { HabitId = 11, HabitName = "Чтение", ScheduledTime = "20:00" }
            }
        };

        var result = SummaryCommand.BuildUncompletedHabitsKeyboard(summary);

        result.Should().NotBeNull();
        var rows = result!.InlineKeyboard.ToList();
        rows.Should().HaveCount(2);
        rows[0].First().Text.Should().Contain("Медитация");
        rows[0].First().CallbackData.Should().Be("summarycomplete:10");
        rows[1].First().Text.Should().Contain("Чтение");
        rows[1].First().CallbackData.Should().Be("summarycomplete:11");
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
