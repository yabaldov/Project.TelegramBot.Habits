using FluentAssertions;
using HabiBot.Application.Services;
using HabiBot.Domain.Entities;
using HabiBot.Domain.Enums;
using HabiBot.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace HabiBot.Tests.Application;

public class DailySummaryServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IHabitRepository> _habitRepositoryMock;
    private readonly Mock<ILogger<DailySummaryService>> _loggerMock;
    private readonly DailySummaryService _service;

    public DailySummaryServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _habitRepositoryMock = new Mock<IHabitRepository>();
        _loggerMock = new Mock<ILogger<DailySummaryService>>();

        _unitOfWorkMock.Setup(u => u.Users).Returns(_userRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Habits).Returns(_habitRepositoryMock.Object);

        _service = new DailySummaryService(_unitOfWorkMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetDailySummaryAsync_ShouldReturnEmptySummary_WhenUserNotFound()
    {
        // Arrange
        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _service.GetDailySummaryAsync(1, DateOnly.FromDateTime(DateTime.UtcNow));

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(1);
        result.PlannedHabitsCount.Should().Be(0);
        result.CompletedHabitsCount.Should().Be(0);
    }

    [Fact]
    public async Task GetDailySummaryAsync_ShouldReturnEmptySummary_WhenNoHabits()
    {
        // Arrange
        var user = new User { Id = 1, Name = "Test", TelegramUserId = 123 };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _habitRepositoryMock
            .Setup(r => r.GetUserHabitsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Habit>());

        // Act
        var result = await _service.GetDailySummaryAsync(1, DateOnly.FromDateTime(DateTime.UtcNow));

        // Assert
        result.PlannedHabitsCount.Should().Be(0);
        result.CompletedHabitsCount.Should().Be(0);
        result.CompletedHabits.Should().BeEmpty();
        result.UncompletedHabits.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDailySummaryAsync_ShouldCountCompletedHabits_WhenLogsExist()
    {
        // Arrange
        var user = new User { Id = 1, Name = "Test", TelegramUserId = 123 };
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var todayStart = today.ToDateTime(TimeOnly.MinValue).ToUniversalTime();

        var habit = new Habit
        {
            Id = 10,
            UserId = 1,
            Name = "Медитация",
            Frequency = HabitFrequency.Daily,
            ReminderTime = "08:00",
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };

        var habitWithLogs = new Habit
        {
            Id = 10,
            UserId = 1,
            Name = "Медитация",
            Frequency = HabitFrequency.Daily,
            ReminderTime = "08:00",
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            Logs = new List<HabitLog>
            {
                new() { Id = 1, HabitId = 10, CompletedAt = todayStart.AddHours(9) }
            }
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _habitRepositoryMock
            .Setup(r => r.GetUserHabitsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Habit> { habit });

        _habitRepositoryMock
            .Setup(r => r.GetByIdWithLogsAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(habitWithLogs);

        // Act
        var result = await _service.GetDailySummaryAsync(1, today);

        // Assert
        result.PlannedHabitsCount.Should().Be(1);
        result.CompletedHabitsCount.Should().Be(1);
        result.CompletedHabits.Should().HaveCount(1);
        result.CompletedHabits[0].HabitName.Should().Be("Медитация");
        result.UncompletedHabits.Should().BeEmpty();
        result.CompletionPercentage.Should().Be(100);
    }

    [Fact]
    public async Task GetDailySummaryAsync_ShouldListUncompletedHabits_WhenNoLogs()
    {
        // Arrange
        var user = new User { Id = 1, Name = "Test", TelegramUserId = 123 };
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var habit = new Habit
        {
            Id = 10,
            UserId = 1,
            Name = "Чтение",
            Frequency = HabitFrequency.Daily,
            ReminderTime = "20:00",
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };

        var habitWithLogs = new Habit
        {
            Id = 10,
            UserId = 1,
            Name = "Чтение",
            Frequency = HabitFrequency.Daily,
            ReminderTime = "20:00",
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            Logs = new List<HabitLog>()
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _habitRepositoryMock
            .Setup(r => r.GetUserHabitsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Habit> { habit });

        _habitRepositoryMock
            .Setup(r => r.GetByIdWithLogsAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(habitWithLogs);

        // Act
        var result = await _service.GetDailySummaryAsync(1, today);

        // Assert
        result.PlannedHabitsCount.Should().Be(1);
        result.CompletedHabitsCount.Should().Be(0);
        result.UncompletedHabits.Should().HaveCount(1);
        result.UncompletedHabits[0].HabitName.Should().Be("Чтение");
        result.CompletionPercentage.Should().Be(0);
    }

    [Fact]
    public async Task GetDailySummaryAsync_ShouldCalculatePercentage_WithMixedHabits()
    {
        // Arrange
        var user = new User { Id = 1, Name = "Test", TelegramUserId = 123 };
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var todayStart = today.ToDateTime(TimeOnly.MinValue).ToUniversalTime();

        var habit1 = new Habit { Id = 10, UserId = 1, Name = "Медитация", Frequency = HabitFrequency.Daily, ReminderTime = "08:00", CreatedAt = DateTime.UtcNow.AddDays(-10) };
        var habit2 = new Habit { Id = 11, UserId = 1, Name = "Чтение", Frequency = HabitFrequency.Daily, ReminderTime = "20:00", CreatedAt = DateTime.UtcNow.AddDays(-10) };
        var habit3 = new Habit { Id = 12, UserId = 1, Name = "Спорт", Frequency = HabitFrequency.Daily, ReminderTime = "18:00", CreatedAt = DateTime.UtcNow.AddDays(-10) };

        var habit1WithLogs = new Habit { Id = 10, UserId = 1, Name = "Медитация", Frequency = HabitFrequency.Daily, CreatedAt = DateTime.UtcNow.AddDays(-10), Logs = new List<HabitLog> { new() { CompletedAt = todayStart.AddHours(9) } } };
        var habit2WithLogs = new Habit { Id = 11, UserId = 1, Name = "Чтение", Frequency = HabitFrequency.Daily, CreatedAt = DateTime.UtcNow.AddDays(-10), Logs = new List<HabitLog>() };
        var habit3WithLogs = new Habit { Id = 12, UserId = 1, Name = "Спорт", Frequency = HabitFrequency.Daily, CreatedAt = DateTime.UtcNow.AddDays(-10), Logs = new List<HabitLog> { new() { CompletedAt = todayStart.AddHours(18) } } };

        _userRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _habitRepositoryMock.Setup(r => r.GetUserHabitsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Habit> { habit1, habit2, habit3 });
        _habitRepositoryMock.Setup(r => r.GetByIdWithLogsAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(habit1WithLogs);
        _habitRepositoryMock.Setup(r => r.GetByIdWithLogsAsync(11, It.IsAny<CancellationToken>())).ReturnsAsync(habit2WithLogs);
        _habitRepositoryMock.Setup(r => r.GetByIdWithLogsAsync(12, It.IsAny<CancellationToken>())).ReturnsAsync(habit3WithLogs);

        // Act
        var result = await _service.GetDailySummaryAsync(1, today);

        // Assert
        result.PlannedHabitsCount.Should().Be(3);
        result.CompletedHabitsCount.Should().Be(2);
        result.CompletionPercentage.Should().Be(66); // 2/3 = 66%
        result.CompletedHabits.Should().HaveCount(2);
        result.UncompletedHabits.Should().HaveCount(1);
        result.UncompletedHabits[0].HabitName.Should().Be("Чтение");
    }

    [Fact]
    public async Task GetDailySummaryAsync_ShouldExcludeDeletedHabits()
    {
        // Arrange
        var user = new User { Id = 1, Name = "Test", TelegramUserId = 123 };
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var deletedHabit = new Habit
        {
            Id = 10,
            UserId = 1,
            Name = "Удалённая",
            Frequency = HabitFrequency.Daily,
            IsDeleted = true,
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };

        _userRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _habitRepositoryMock.Setup(r => r.GetUserHabitsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Habit> { deletedHabit });

        // Act
        var result = await _service.GetDailySummaryAsync(1, today);

        // Assert
        result.PlannedHabitsCount.Should().Be(0);
    }

    [Fact]
    public async Task GetDailySummaryAsync_ShouldIncludeNextDayHabits()
    {
        // Arrange
        var user = new User { Id = 1, Name = "Test", TelegramUserId = 123 };
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var dailyHabit = new Habit
        {
            Id = 10,
            UserId = 1,
            Name = "Ежедневная",
            Frequency = HabitFrequency.Daily,
            ReminderTime = "09:00",
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };

        var habitWithLogs = new Habit
        {
            Id = 10,
            UserId = 1,
            Name = "Ежедневная",
            Frequency = HabitFrequency.Daily,
            ReminderTime = "09:00",
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            Logs = new List<HabitLog>()
        };

        _userRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _habitRepositoryMock.Setup(r => r.GetUserHabitsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Habit> { dailyHabit });
        _habitRepositoryMock.Setup(r => r.GetByIdWithLogsAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(habitWithLogs);

        // Act
        var result = await _service.GetDailySummaryAsync(1, today);

        // Assert
        result.NextDayHabits.Should().HaveCount(1);
        result.NextDayHabits[0].HabitName.Should().Be("Ежедневная");
        result.NextDayHabits[0].ScheduledTime.Should().Be("09:00");
    }

    [Fact]
    public async Task GenerateSummaryTextAsync_ShouldContainDate()
    {
        // Arrange
        var user = new User { Id = 1, Name = "Test", TelegramUserId = 123 };
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        _userRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _habitRepositoryMock.Setup(r => r.GetUserHabitsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Habit>());

        // Act
        var result = await _service.GenerateSummaryTextAsync(1, today);

        // Assert
        result.Should().Contain("Сводка за");
    }

    [Fact]
    public async Task GenerateSummaryTextAsync_ShouldContainNoHabitsMessage_WhenEmpty()
    {
        // Arrange
        var user = new User { Id = 1, Name = "Test", TelegramUserId = 123 };
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        _userRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _habitRepositoryMock.Setup(r => r.GetUserHabitsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Habit>());

        // Act
        var result = await _service.GenerateSummaryTextAsync(1, today);

        // Assert
        result.Should().Contain("не было запланировано");
    }

    [Fact]
    public async Task GenerateSummaryTextAsync_ShouldNotIncludeNextDay_WhenFlagIsFalse()
    {
        // Arrange
        var user = new User { Id = 1, Name = "Test", TelegramUserId = 123 };
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var todayStart = today.ToDateTime(TimeOnly.MinValue).ToUniversalTime();

        var habit = new Habit { Id = 10, UserId = 1, Name = "Тест", Frequency = HabitFrequency.Daily, ReminderTime = "08:00", CreatedAt = DateTime.UtcNow.AddDays(-10) };
        var habitWithLogs = new Habit { Id = 10, UserId = 1, Name = "Тест", Frequency = HabitFrequency.Daily, ReminderTime = "08:00", CreatedAt = DateTime.UtcNow.AddDays(-10), Logs = new List<HabitLog>() };

        _userRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _habitRepositoryMock.Setup(r => r.GetUserHabitsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Habit> { habit });
        _habitRepositoryMock.Setup(r => r.GetByIdWithLogsAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(habitWithLogs);

        // Act
        var result = await _service.GenerateSummaryTextAsync(1, today, includeNextDay: false);

        // Assert
        result.Should().NotContain("Завтра у тебя запланированы");
    }

    [Fact]
    public async Task GenerateSummaryTextAsync_ShouldIncludeNextDay_WhenFlagIsTrue()
    {
        // Arrange
        var user = new User { Id = 1, Name = "Test", TelegramUserId = 123 };
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var habit = new Habit { Id = 10, UserId = 1, Name = "Тест", Frequency = HabitFrequency.Daily, ReminderTime = "08:00", CreatedAt = DateTime.UtcNow.AddDays(-10) };
        var habitWithLogs = new Habit { Id = 10, UserId = 1, Name = "Тест", Frequency = HabitFrequency.Daily, ReminderTime = "08:00", CreatedAt = DateTime.UtcNow.AddDays(-10), Logs = new List<HabitLog>() };

        _userRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _habitRepositoryMock.Setup(r => r.GetUserHabitsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Habit> { habit });
        _habitRepositoryMock.Setup(r => r.GetByIdWithLogsAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(habitWithLogs);

        // Act
        var result = await _service.GenerateSummaryTextAsync(1, today, includeNextDay: true);

        // Assert
        result.Should().Contain("Завтра у тебя запланированы");
        result.Should().Contain("Тест");
    }
}
