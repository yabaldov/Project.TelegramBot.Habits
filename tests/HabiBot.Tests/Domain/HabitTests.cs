using FluentAssertions;
using HabiBot.Domain.Entities;
using HabiBot.Domain.Enums;

namespace HabiBot.Tests.Domain;

public class HabitTests
{
    [Fact]
    public void Habit_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var habit = new Habit();

        // Assert
        habit.Id.Should().Be(0);
        habit.Name.Should().BeNullOrEmpty();
        habit.ReminderTime.Should().BeNull();
        habit.Frequency.Should().Be(HabitFrequency.Daily); // Default value set in entity
        habit.IsDeleted.Should().BeFalse();
        habit.Logs.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Habit_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var habit = new Habit
        {
            Name = "Медитация",
            ReminderTime = "08:00",
            Frequency = HabitFrequency.Daily,
            UserId = 1
        };

        // Act & Assert
        habit.Name.Should().Be("Медитация");
        habit.ReminderTime.Should().Be("08:00");
        habit.Frequency.Should().Be(HabitFrequency.Daily);
        habit.UserId.Should().Be(1);
    }

    [Fact]
    public void Habit_ShouldHaveNavigationProperties()
    {
        // Arrange
        var user = new User { Id = 1, Name = "Тест", TelegramChatId = 123 };
        var habit = new Habit { UserId = user.Id, User = user };

        // Act & Assert
        habit.User.Should().NotBeNull();
        habit.User.Id.Should().Be(1);
        habit.Logs.Should().NotBeNull();
    }
}
