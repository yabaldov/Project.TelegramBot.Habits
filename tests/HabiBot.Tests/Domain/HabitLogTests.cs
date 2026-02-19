using FluentAssertions;
using HabiBot.Domain.Entities;

namespace HabiBot.Tests.Domain;

public class HabitLogTests
{
    [Fact]
    public void HabitLog_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var log = new HabitLog();

        // Assert
        log.Id.Should().Be(0);
        log.HabitId.Should().Be(0);
        log.CompletedAt.Should().Be(default(DateTime));
        log.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void HabitLog_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var completedAt = DateTime.UtcNow;
        var log = new HabitLog
        {
            HabitId = 1,
            CompletedAt = completedAt
        };

        // Act & Assert
        log.HabitId.Should().Be(1);
        log.CompletedAt.Should().Be(completedAt);
    }

    [Fact]
    public void HabitLog_ShouldHaveNavigationToHabit()
    {
        // Arrange
        var habit = new Habit { Id = 1, Name = "Тест" };
        var log = new HabitLog { HabitId = habit.Id, Habit = habit };

        // Act & Assert
        log.Habit.Should().NotBeNull();
        log.Habit.Id.Should().Be(1);
        log.Habit.Name.Should().Be("Тест");
    }
}
