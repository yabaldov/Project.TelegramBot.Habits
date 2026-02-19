using FluentAssertions;
using FluentValidation;
using HabiBot.Application.DTOs;
using HabiBot.Application.Services;
using HabiBot.Application.Validators;
using HabiBot.Domain.Enums;
using HabiBot.Infrastructure.Data;
using HabiBot.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HabiBot.Tests.Integration;

/// <summary>
/// Integration тесты для сервисов с реальным DbContext
/// </summary>
public class ServiceIntegrationTests : IDisposable
{
    private readonly HabiBotDbContext _context;
    private readonly UnitOfWork _unitOfWork;
    private readonly IUserService _userService;
    private readonly IHabitService _habitService;
    private readonly IHabitLogService _habitLogService;

    public ServiceIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<HabiBotDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new HabiBotDbContext(options);
        
        // Создаем репозитории
        var userRepository = new UserRepository(_context);
        var habitRepository = new HabitRepository(_context);
        var habitLogRepository = new HabitLogRepository(_context);
        
        _unitOfWork = new UnitOfWork(_context, userRepository, habitRepository, habitLogRepository);

        // Создаем реальные валидаторы
        var createUserValidator = new CreateUserDtoValidator();
        var createHabitValidator = new CreateHabitDtoValidator();
        var updateHabitValidator = new UpdateHabitDtoValidator();
        var createHabitLogValidator = new CreateHabitLogDtoValidator();

        // Используем NullLogger для тестов
        _userService = new UserService(_unitOfWork, createUserValidator, NullLogger<UserService>.Instance);
        _habitService = new HabitService(_unitOfWork, createHabitValidator, updateHabitValidator, NullLogger<HabitService>.Instance);
        _habitLogService = new HabitLogService(_unitOfWork, createHabitLogValidator, NullLogger<HabitLogService>.Instance);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _unitOfWork.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task UserService_CreateUser_SavesCorrectly()
    {
        // Arrange
        var dto = new CreateUserDto
        {
            TelegramUserId = 123456,
            TelegramChatId = 123456,
            TelegramFirstName = "Иван",
            TelegramLastName = "Иванов",
            TelegramUserName = "ivanov",
            Name = "Иван"
        };

        // Act
        var user = await _userService.CreateUserAsync(dto);

        // Assert
        user.Should().NotBeNull();
        user.Id.Should().BeGreaterThan(0);
        user.Name.Should().Be("Иван");

        // Проверяем, что пользователь сохранен в БД
        var savedUser = await _unitOfWork.Users.GetByIdAsync(user.Id);
        savedUser.Should().NotBeNull();
    }

    [Fact]
    public async Task HabitService_CreateHabit_WithValidation_WorksCorrectly()
    {
        // Arrange - создаем пользователя
        var userDto = new CreateUserDto
        {
            TelegramUserId = 111222,
            TelegramChatId = 111222,
            Name = "Test User"
        };
        var user = await _userService.CreateUserAsync(userDto);

        var habitDto = new CreateHabitDto
        {
            UserId = user.Id,
            Name = "Медитация утром",
            ReminderTime = "08:00",
            Frequency = HabitFrequency.Daily
        };

        // Act
        var habit = await _habitService.CreateHabitAsync(habitDto);

        // Assert
        habit.Should().NotBeNull();
        habit.Name.Should().Be("Медитация утром");
        habit.ReminderTime.Should().Be("08:00");
        habit.Frequency.Should().Be(HabitFrequency.Daily);
    }

    [Fact]
    public async Task HabitService_CreateHabit_WithInvalidData_ThrowsValidationException()
    {
        // Arrange
        var habitDto = new CreateHabitDto
        {
            UserId = 999, // несуществующий пользователь
            Name = "", // пустое название
            ReminderTime = "25:99" // невалидное время
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await _habitService.CreateHabitAsync(habitDto));
    }

    [Fact]
    public async Task HabitService_UpdateHabit_ChangesValues()
    {
        // Arrange
        var userDto = new CreateUserDto
        {
            TelegramUserId = 222333,
            TelegramChatId = 222333,
            Name = "Update User"
        };
        var user = await _userService.CreateUserAsync(userDto);

        var habitDto = new CreateHabitDto
        {
            UserId = user.Id,
            Name = "Старое название",
            ReminderTime = "09:00"
        };
        var habit = await _habitService.CreateHabitAsync(habitDto);

        // Act
        var updateDto = new UpdateHabitDto
        {
            Id = habit.Id,
            Name = "Новое название",
            ReminderTime = "10:00",
            Frequency = HabitFrequency.Weekly
        };
        var updated = await _habitService.UpdateHabitAsync(updateDto);

        // Assert
        updated.Name.Should().Be("Новое название");
        updated.ReminderTime.Should().Be("10:00");
        updated.Frequency.Should().Be(HabitFrequency.Weekly);

        // Проверяем в БД
        var fromDb = await _unitOfWork.Habits.GetByIdAsync(habit.Id);
        fromDb!.Name.Should().Be("Новое название");
    }

    [Fact]
    public async Task HabitService_DeleteHabit_SoftDeletes()
    {
        // Arrange
        var userDto = new CreateUserDto
        {
            TelegramUserId = 333444,
            TelegramChatId = 333444,
            Name = "Delete User"
        };
        var user = await _userService.CreateUserAsync(userDto);

        var habitDto = new CreateHabitDto
        {
            UserId = user.Id,
            Name = "Удаляемая привычка",
            ReminderTime = "11:00"
        };
        var habit = await _habitService.CreateHabitAsync(habitDto);

        // Act
        await _habitService.DeleteHabitAsync(habit.Id);

        // Очищаем Change Tracker для корректной работы глобального фильтра в InMemory DB
        _context.ChangeTracker.Clear();

        // Assert
        var deleted = await _habitService.GetByIdAsync(habit.Id);
        deleted.Should().BeNull(); // глобальный фильтр скрывает удаленные

        // Проверяем физическое наличие в БД
        var rawHabit = await _context.Habits
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(h => h.Id == habit.Id);
        rawHabit.Should().NotBeNull();
        rawHabit!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task HabitLogService_CreateLog_CreatesLog()
    {
        // Arrange
        var userDto = new CreateUserDto
        {
            TelegramUserId = 444555,
            TelegramChatId = 444555,
            Name = "Log User"
        };
        var user = await _userService.CreateUserAsync(userDto);

        var habitDto = new CreateHabitDto
        {
            UserId = user.Id,
            Name = "Логируемая привычка",
            ReminderTime = "12:00"
        };
        var habit = await _habitService.CreateHabitAsync(habitDto);

        // Act
        var completedAt = DateTime.UtcNow;
        var logDto = new CreateHabitLogDto
        {
            HabitId = habit.Id,
            CompletedAt = completedAt
        };
        var log = await _habitLogService.CreateLogAsync(logDto);

        // Assert
        log.Should().NotBeNull();
        log.HabitId.Should().Be(habit.Id);
        log.CompletedAt.Should().BeCloseTo(completedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task HabitLogService_IsCompletedTodayAsync_WorksCorrectly()
    {
        // Arrange
        var userDto = new CreateUserDto
        {
            TelegramUserId = 555666,
            TelegramChatId = 555666,
            Name = "Today User"
        };
        var user = await _userService.CreateUserAsync(userDto);

        var habitDto = new CreateHabitDto
        {
            UserId = user.Id,
            Name = "Ежедневная привычка",
            ReminderTime = "08:00"
        };
        var habit = await _habitService.CreateHabitAsync(habitDto);

        // Создаем лог за сегодня
        await _habitLogService.CreateLogAsync(new CreateHabitLogDto
        {
            HabitId = habit.Id,
            CompletedAt = DateTime.UtcNow
        });

        // Act
        var isCompleted = await _habitLogService.IsCompletedTodayAsync(habit.Id);

        // Assert
        isCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteWorkflow_CreateUserHabitAndLogs_WorksEndToEnd()
    {
        // Arrange & Act
        // 1. Создаем пользователя
        var user = await _userService.CreateUserAsync(new CreateUserDto
        {
            TelegramUserId = 777888,
            TelegramChatId = 777888,
            Name = "Workflow User"
        });

        // 2. Создаем несколько привычек
        var meditation = await _habitService.CreateHabitAsync(new CreateHabitDto
        {
            UserId = user.Id,
            Name = "Медитация",
            ReminderTime = "07:00"
        });

        var exercise = await _habitService.CreateHabitAsync(new CreateHabitDto
        {
            UserId = user.Id,
            Name = "Зарядка",
            ReminderTime = "08:00"
        });

        // 3. Логируем выполнение
        await _habitLogService.CreateLogAsync(new CreateHabitLogDto
        {
            HabitId = meditation.Id,
            CompletedAt = DateTime.UtcNow
        });

        await _habitLogService.CreateLogAsync(new CreateHabitLogDto
        {
            HabitId = exercise.Id,
            CompletedAt = DateTime.UtcNow
        });

        // 4. Получаем все привычки пользователя
        var habits = await _habitService.GetUserHabitsAsync(user.Id);

        // 5. Проверяем выполнение за сегодня
        var meditationCompleted = await _habitLogService.IsCompletedTodayAsync(meditation.Id);
        var exerciseCompleted = await _habitLogService.IsCompletedTodayAsync(exercise.Id);

        // Assert
        user.Should().NotBeNull();
        habits.Should().HaveCount(2);
        meditationCompleted.Should().BeTrue();
        exerciseCompleted.Should().BeTrue();
        habits.Should().Contain(h => h.Name == "Медитация");
        habits.Should().Contain(h => h.Name == "Зарядка");
    }
}
