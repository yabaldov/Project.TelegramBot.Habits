using FluentAssertions;
using HabiBot.Application.DTOs;
using HabiBot.Application.Validators;

namespace HabiBot.Tests.Application;

public class ValidatorTests
{
    [Fact]
    public async Task CreateUserDtoValidator_ShouldPass_WhenValidData()
    {
        // Arrange
        var validator = new CreateUserDtoValidator();
        var dto = new CreateUserDto
        {
            TelegramChatId = 123456789,
            Name = "Алексей"
        };

        // Act
        var result = await validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CreateUserDtoValidator_ShouldFail_WhenNameIsEmpty()
    {
        // Arrange
        var validator = new CreateUserDtoValidator();
        var dto = new CreateUserDto
        {
            TelegramChatId = 123456789,
            Name = ""
        };

        // Act
        var result = await validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserDto.Name));
    }

    [Fact]
    public async Task CreateUserDtoValidator_ShouldFail_WhenTelegramChatIdIsZero()
    {
        // Arrange
        var validator = new CreateUserDtoValidator();
        var dto = new CreateUserDto
        {
            TelegramChatId = 0,
            Name = "Тест"
        };

        // Act
        var result = await validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserDto.TelegramChatId));
    }

    [Fact]
    public async Task CreateHabitDtoValidator_ShouldPass_WhenValidData()
    {
        // Arrange
        var validator = new CreateHabitDtoValidator();
        var dto = new CreateHabitDto
        {
            UserId = 1,
            Name = "Медитация",
            ReminderTime = "08:00",
            Frequency = HabiBot.Domain.Enums.HabitFrequency.Daily
        };

        // Act
        var result = await validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CreateHabitDtoValidator_ShouldFail_WhenNameIsEmpty()
    {
        // Arrange
        var validator = new CreateHabitDtoValidator();
        var dto = new CreateHabitDto
        {
            UserId = 1,
            Name = "",
            ReminderTime = "08:00",
            Frequency = HabiBot.Domain.Enums.HabitFrequency.Daily
        };

        // Act
        var result = await validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateHabitDto.Name));
    }

    [Fact]
    public async Task CreateHabitDtoValidator_ShouldFail_WhenReminderTimeIsInvalid()
    {
        // Arrange
        var validator = new CreateHabitDtoValidator();
        var dto = new CreateHabitDto
        {
            UserId = 1,
            Name = "Медитация",
            ReminderTime = "25:00", // Неверный формат
            Frequency = HabiBot.Domain.Enums.HabitFrequency.Daily
        };

        // Act
        var result = await validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateHabitDto.ReminderTime));
    }
}
