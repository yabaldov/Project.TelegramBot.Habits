using FluentAssertions;
using FluentValidation;
using HabiBot.Application.DTOs;
using HabiBot.Application.Services;
using HabiBot.Domain.Entities;
using HabiBot.Domain.Enums;
using HabiBot.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace HabiBot.Tests.Application;

public class HabitServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IHabitRepository> _habitRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ILogger<HabitService>> _loggerMock;
    private readonly Mock<IValidator<CreateHabitDto>> _createValidatorMock;
    private readonly Mock<IValidator<UpdateHabitDto>> _updateValidatorMock;
    private readonly HabitService _habitService;

    public HabitServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _habitRepositoryMock = new Mock<IHabitRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _loggerMock = new Mock<ILogger<HabitService>>();
        _createValidatorMock = new Mock<IValidator<CreateHabitDto>>();
        _updateValidatorMock = new Mock<IValidator<UpdateHabitDto>>();

        _unitOfWorkMock.Setup(u => u.Habits).Returns(_habitRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Users).Returns(_userRepositoryMock.Object);

        _habitService = new HabitService(
            _unitOfWorkMock.Object,
            _createValidatorMock.Object,
            _updateValidatorMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CreateHabitAsync_ShouldCreateHabit_WhenValidDto()
    {
        // Arrange
        var dto = new CreateHabitDto
        {
            UserId = 1,
            Name = "Медитация",
            ReminderTime = "08:00",
            Frequency = HabitFrequency.Daily
        };

        _createValidatorMock
            .Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _userRepositoryMock
            .Setup(r => r.ExistsAsync(dto.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _habitRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Habit>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Habit h, CancellationToken ct) => h);

        // Act
        var result = await _habitService.CreateHabitAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Медитация");
        result.ReminderTime.Should().Be("08:00");
        result.Frequency.Should().Be(HabitFrequency.Daily);
        result.UserId.Should().Be(1);

        _habitRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Habit>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateHabitAsync_ShouldThrowException_WhenUserNotFound()
    {
        // Arrange
        var dto = new CreateHabitDto
        {
            UserId = 999,
            Name = "Медитация",
            ReminderTime = "08:00",
            Frequency = HabitFrequency.Daily
        };

        _createValidatorMock
            .Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _userRepositoryMock
            .Setup(r => r.ExistsAsync(dto.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        Func<Task> act = async () => await _habitService.CreateHabitAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*не найден*");

        _habitRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Habit>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetUserHabitsAsync_ShouldReturnHabits_WhenUserHasHabits()
    {
        // Arrange
        var userId = 1L;
        var expectedHabits = new List<Habit>
        {
            new() { Id = 1, Name = "Медитация", UserId = userId },
            new() { Id = 2, Name = "Упражнения", UserId = userId }
        };

        _habitRepositoryMock
            .Setup(r => r.GetUserHabitsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedHabits);

        // Act
        var result = await _habitService.GetUserHabitsAsync(userId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(h => h.Name == "Медитация");
        result.Should().Contain(h => h.Name == "Упражнения");
    }

    [Fact]
    public async Task UpdateHabitAsync_ShouldUpdateHabit_WhenValidDto()
    {
        // Arrange
        var habitId = 1L;
        var existingHabit = new Habit
        {
            Id = habitId,
            Name = "Старое название",
            ReminderTime = "08:00",
            Frequency = HabitFrequency.Daily,
            UserId = 1
        };

        var dto = new UpdateHabitDto
        {
            Id = habitId,
            Name = "Новое название",
            ReminderTime = "09:00"
        };

        _updateValidatorMock
            .Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _habitRepositoryMock
            .Setup(r => r.GetByIdAsync(habitId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingHabit);

        _habitRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Habit>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _habitService.UpdateHabitAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Новое название");
        result.ReminderTime.Should().Be("09:00");

        _habitRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Habit>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
