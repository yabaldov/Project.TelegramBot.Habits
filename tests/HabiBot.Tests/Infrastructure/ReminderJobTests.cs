using FluentAssertions;
using HabiBot.Domain.Entities;
using HabiBot.Domain.Interfaces;
using HabiBot.Infrastructure.Jobs;
using HabiBot.Infrastructure.Models.Telegram;
using HabiBot.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Quartz;
using System.Linq;

namespace HabiBot.Tests.Infrastructure;

/// <summary>
/// Unit-тесты для ReminderJob
/// </summary>
public class ReminderJobTests
{
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<IServiceScope> _scopeMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IHabitRepository> _habitRepositoryMock;
    private readonly Mock<ITelegramApiClient> _telegramClientMock;
    private readonly Mock<ILogger<ReminderJob>> _loggerMock;
    private readonly ReminderJob _job;

    public ReminderJobTests()
    {
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _scopeMock = new Mock<IServiceScope>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _habitRepositoryMock = new Mock<IHabitRepository>();
        _telegramClientMock = new Mock<ITelegramApiClient>();
        _loggerMock = new Mock<ILogger<ReminderJob>>();

        // Настройка цепочки DI
        _unitOfWorkMock.Setup(u => u.Habits).Returns(_habitRepositoryMock.Object);

        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IUnitOfWork)))
            .Returns(_unitOfWorkMock.Object);
        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(ITelegramApiClient)))
            .Returns(_telegramClientMock.Object);

        _scopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock.Object);
        _scopeFactoryMock.Setup(f => f.CreateScope()).Returns(_scopeMock.Object);

        _job = new ReminderJob(_scopeFactoryMock.Object, _loggerMock.Object);
    }

    private static IJobExecutionContext CreateJobContext()
    {
        var contextMock = new Mock<IJobExecutionContext>();
        contextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);
        return contextMock.Object;
    }

    private static User CreateUser(string? timeZone = null, long telegramId = 100, long chatId = 100)
    {
        return new User
        {
            Id = 1,
            TelegramUserId = telegramId,
            TelegramChatId = chatId,
            Name = "Test User",
            TimeZone = timeZone,
            RegisteredAt = DateTime.UtcNow
        };
    }

    private static Habit CreateHabit(User user, string reminderTime, long habitId = 1)
    {
        return new Habit
        {
            Id = habitId,
            UserId = user.Id,
            User = user,
            Name = "Тестовая привычка",
            ReminderTime = reminderTime
        };
    }

    #region Execute — нет привычек

    [Fact]
    public async Task Execute_ShouldNotSendMessages_WhenNoHabitsWithReminder()
    {
        // Arrange
        _habitRepositoryMock
            .Setup(r => r.GetHabitsWithReminderAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<Habit>());

        // Act
        await _job.Execute(CreateJobContext());

        // Assert
        _telegramClientMock.Verify(
            c => c.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Execute — отправка напоминания

    [Fact]
    public async Task Execute_ShouldSendReminder_WhenReminderTimeMatchesCurrentUtcTime()
    {
        // Arrange — привычка без временной зоны, время напоминания = текущее UTC HH:mm
        var utcNow = DateTime.UtcNow;
        var reminderTime = $"{utcNow.Hour:D2}:{utcNow.Minute:D2}";

        var user = CreateUser(timeZone: null, telegramId: 200, chatId: 200);
        var habit = CreateHabit(user, reminderTime);

        _habitRepositoryMock
            .Setup(r => r.GetHabitsWithReminderAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { habit });

        SendMessageRequest? capturedRequest = null;
        _telegramClientMock
            .Setup(c => c.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .Callback<SendMessageRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new Message());

        // Act
        await _job.Execute(CreateJobContext());

        // Assert
        _telegramClientMock.Verify(
            c => c.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.ChatId.Should().Be(200);
        capturedRequest.Text.Should().Contain("Напоминание");
        capturedRequest.Text.Should().Contain("Тестовая привычка");
        capturedRequest.ParseMode.Should().Be("HTML");
    }

    [Fact]
    public async Task Execute_ShouldIncludeInlineButton_InReminderMessage()
    {
        // Arrange
        var utcNow = DateTime.UtcNow;
        var reminderTime = $"{utcNow.Hour:D2}:{utcNow.Minute:D2}";

        var user = CreateUser(timeZone: null);
        var habit = CreateHabit(user, reminderTime, habitId: 42);

        _habitRepositoryMock
            .Setup(r => r.GetHabitsWithReminderAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { habit });

        SendMessageRequest? capturedRequest = null;
        _telegramClientMock
            .Setup(c => c.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .Callback<SendMessageRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new Message());

        // Act
        await _job.Execute(CreateJobContext());

        // Assert
        capturedRequest.Should().NotBeNull();
        var keyboard = capturedRequest!.ReplyMarkup as InlineKeyboardMarkup;
        keyboard.Should().NotBeNull();
        keyboard!.InlineKeyboard.Should().HaveCount(1);
        var firstButton = keyboard.InlineKeyboard.First().First();
        firstButton.CallbackData.Should().Be("summarycomplete:42");
        firstButton.Text.Should().Contain("Выполнено");
    }

    #endregion

    #region Execute — пропуск напоминания

    [Fact]
    public async Task Execute_ShouldNotSendReminder_WhenReminderTimeDiffersFromCurrentTime()
    {
        // Arrange — устанавливаем время на час вперёд от текущего UTC
        var utcNow = DateTime.UtcNow;
        var wrongHour = (utcNow.Hour + 1) % 24;
        var reminderTime = $"{wrongHour:D2}:{utcNow.Minute:D2}";

        var user = CreateUser(timeZone: null);
        var habit = CreateHabit(user, reminderTime);

        _habitRepositoryMock
            .Setup(r => r.GetHabitsWithReminderAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { habit });

        // Act
        await _job.Execute(CreateJobContext());

        // Assert
        _telegramClientMock.Verify(
            c => c.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Execute_ShouldNotSendReminder_WhenHabitHasNullReminderTime()
    {
        // Arrange — GetHabitsWithReminderAsync не должен возвращать такие привычки,
        // но на случай если что-то проскочит — job должен безопасно пропустить
        var user = CreateUser(timeZone: null);
        var habit = new Habit
        {
            Id = 1,
            UserId = user.Id,
            User = user,
            Name = "Без времени",
            ReminderTime = null
        };

        _habitRepositoryMock
            .Setup(r => r.GetHabitsWithReminderAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { habit });

        // Act
        await _job.Execute(CreateJobContext());

        // Assert
        _telegramClientMock.Verify(
            c => c.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Execute_ShouldNotSendReminder_WhenReminderTimeFormatIsInvalid()
    {
        // Arrange
        var user = CreateUser(timeZone: null);
        var habit = CreateHabit(user, reminderTime: "invalid");

        _habitRepositoryMock
            .Setup(r => r.GetHabitsWithReminderAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { habit });

        // Act
        await _job.Execute(CreateJobContext());

        // Assert
        _telegramClientMock.Verify(
            c => c.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Execute — временные зоны

    [Fact]
    public async Task Execute_ShouldRespectUserTimeZone_WhenSendingReminder()
    {
        // Arrange — пользователь с временной зоной Europe/Moscow (UTC+3)
        // Время напоминания = (UTC + 3) HH:mm
        var utcNow = DateTime.UtcNow;
        var moscowOffset = TimeSpan.FromHours(3);
        var moscowNow = utcNow + moscowOffset;
        var reminderTime = $"{moscowNow.Hour:D2}:{moscowNow.Minute:D2}";

        var user = CreateUser(timeZone: "Europe/Moscow", telegramId: 300, chatId: 300);
        var habit = CreateHabit(user, reminderTime);

        _habitRepositoryMock
            .Setup(r => r.GetHabitsWithReminderAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { habit });

        SendMessageRequest? capturedRequest = null;
        _telegramClientMock
            .Setup(c => c.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .Callback<SendMessageRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new Message());

        // Act
        await _job.Execute(CreateJobContext());

        // Assert — напоминание должно быть отправлено
        _telegramClientMock.Verify(
            c => c.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
        capturedRequest!.ChatId.Should().Be(300);
    }

    #endregion

    #region Execute — обработка ошибок

    [Fact]
    public async Task Execute_ShouldContinueWithOtherHabits_WhenOneThrowsException()
    {
        // Arrange — первая привычка с некорректным chatId (вызовет ошибку),
        // вторая — должна быть отправлена успешно
        var utcNow = DateTime.UtcNow;
        var reminderTime = $"{utcNow.Hour:D2}:{utcNow.Minute:D2}";

        var user1 = CreateUser(telegramId: 401, chatId: 401);
        var user2 = CreateUser(telegramId: 402, chatId: 402);
        var habit1 = CreateHabit(user1, reminderTime, habitId: 1);
        var habit2 = CreateHabit(user2, reminderTime, habitId: 2);
        user2.Id = 2;
        habit2.UserId = 2;

        _habitRepositoryMock
            .Setup(r => r.GetHabitsWithReminderAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { habit1, habit2 });

        _telegramClientMock
            .Setup(c => c.SendMessageAsync(
                It.Is<SendMessageRequest>(r => r.ChatId == 401),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Telegram API error"));

        _telegramClientMock
            .Setup(c => c.SendMessageAsync(
                It.Is<SendMessageRequest>(r => r.ChatId == 402),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Message());

        // Act
        await _job.Execute(CreateJobContext());

        // Assert — вторая привычка должна быть отправлена несмотря на ошибку первой
        _telegramClientMock.Verify(
            c => c.SendMessageAsync(
                It.Is<SendMessageRequest>(r => r.ChatId == 402),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Execute_ShouldNotThrow_WhenRepositoryThrowsException()
    {
        // Arrange
        _habitRepositoryMock
            .Setup(r => r.GetHabitsWithReminderAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        var act = () => _job.Execute(CreateJobContext());
        await act.Should().NotThrowAsync();
    }

    #endregion
}
