using HabiBot.Application.Services;
using HabiBot.Bot.Commands;
using HabiBot.Bot.StateManagement;
using HabiBot.Domain.Entities;
using HabiBot.Infrastructure.Models.Telegram;
using HabiBot.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HabiBot.Tests.Bot;

/// <summary>
/// Тесты для команды /edit
/// </summary>
public class EditCommandTests
{
    private readonly Mock<ITelegramApiClient> _mockTelegramClient;
    private readonly Mock<IUserStateManager> _mockStateManager;
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<IHabitService> _mockHabitService;
    private readonly Mock<ILogger<EditCommand>> _mockLogger;
    private readonly EditCommand _command;

    public EditCommandTests()
    {
        _mockTelegramClient = new Mock<ITelegramApiClient>();
        _mockStateManager = new Mock<IUserStateManager>();
        _mockUserService = new Mock<IUserService>();
        _mockHabitService = new Mock<IHabitService>();
        _mockLogger = new Mock<ILogger<EditCommand>>();

        _command = new EditCommand(
            _mockTelegramClient.Object,
            _mockStateManager.Object,
            _mockUserService.Object,
            _mockHabitService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public void Name_ReturnsEdit()
    {
        // Act
        var name = _command.Name;

        // Assert
        Assert.Equal("edit", name);
    }

    [Fact]
    public void Description_ReturnsCorrectDescription()
    {
        // Act
        var description = _command.Description;

        // Assert
        Assert.Equal("Редактировать привычку", description);
    }

    [Fact]
    public async Task ExecuteAsync_UserNotRegistered_SendsRegistrationMessage()
    {
        // Arrange
        var update = new Update
        {
            Message = new Message
            {
                Chat = new Chat { Id = 12345 },
                From = new TelegramUser { Id = 67890 }
            }
        };

        _mockUserService
            .Setup(x => x.GetByTelegramIdAsync(67890, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        await _command.ExecuteAsync(update);

        // Assert
        _mockTelegramClient.Verify(
            x => x.SendMessageAsync(
                It.Is<SendMessageRequest>(r => 
                    r.ChatId == 12345 && 
                    r.Text.Contains("не зарегистрированы")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_UserHasNoHabits_SendsNoHabitsMessage()
    {
        // Arrange
        var user = new User { Id = 1, TelegramUserId = 67890 };
        var update = new Update
        {
            Message = new Message
            {
                Chat = new Chat { Id = 12345 },
                From = new TelegramUser { Id = 67890 }
            }
        };

        _mockUserService
            .Setup(x => x.GetByTelegramIdAsync(67890, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockHabitService
            .Setup(x => x.GetUserHabitsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Habit>());

        // Act
        await _command.ExecuteAsync(update);

        // Assert
        _mockTelegramClient.Verify(
            x => x.SendMessageAsync(
                It.Is<SendMessageRequest>(r => 
                    r.ChatId == 12345 && 
                    r.Text.Contains("У вас пока нет привычек")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_UserHasHabits_SendsHabitListWithInlineKeyboard()
    {
        // Arrange
        var user = new User { Id = 1, TelegramUserId = 67890 };
        var habits = new List<Habit>
        {
            new Habit { Id = 1, Name = "Медитация", UserId = 1 },
            new Habit { Id = 2, Name = "Зарядка", UserId = 1 }
        };
        var update = new Update
        {
            Message = new Message
            {
                Chat = new Chat { Id = 12345 },
                From = new TelegramUser { Id = 67890 }
            }
        };

        _mockUserService
            .Setup(x => x.GetByTelegramIdAsync(67890, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockHabitService
            .Setup(x => x.GetUserHabitsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(habits);

        // Act
        await _command.ExecuteAsync(update);

        // Assert
        _mockTelegramClient.Verify(
            x => x.SendMessageAsync(
                It.Is<SendMessageRequest>(r => 
                    r.ChatId == 12345 && 
                    r.Text.Contains("Выберите привычку") &&
                    r.ReplyMarkup != null),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
