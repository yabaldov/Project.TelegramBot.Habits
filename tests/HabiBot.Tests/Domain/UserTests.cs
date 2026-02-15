using FluentAssertions;
using HabiBot.Domain.Entities;

namespace HabiBot.Tests.Domain;

public class UserTests
{
    [Fact]
    public void User_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        user.Id.Should().Be(0);
        user.Name.Should().BeNullOrEmpty();
        user.TelegramChatId.Should().Be(0);
        user.IsDeleted.Should().BeFalse();
        user.Habits.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void User_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var user = new User
        {
            Name = "Алексей",
            TelegramUserId = 123456789,
            TelegramChatId = 123456789,
            TelegramFirstName = "Alex",
            TelegramLastName = "Ivanov",
            TelegramUserName = "alexivanov"
        };

        // Act & Assert
        user.Name.Should().Be("Алексей");
        user.TelegramUserId.Should().Be(123456789);
        user.TelegramChatId.Should().Be(123456789);
        user.TelegramFirstName.Should().Be("Alex");
        user.TelegramLastName.Should().Be("Ivanov");
        user.TelegramUserName.Should().Be("alexivanov");
    }

    [Fact]
    public void User_ShouldHaveHabitsCollection()
    {
        // Arrange
        var user = new User { Id = 1, Name = "Тест", TelegramUserId = 123, TelegramChatId = 123 };
        var habit1 = new Habit { Name = "Привычка 1", UserId = user.Id };
        var habit2 = new Habit { Name = "Привычка 2", UserId = user.Id };

        // Act
        user.Habits.Add(habit1);
        user.Habits.Add(habit2);

        // Assert
        user.Habits.Should().HaveCount(2);
        user.Habits.Should().Contain(h => h.Name == "Привычка 1");
        user.Habits.Should().Contain(h => h.Name == "Привычка 2");
    }
}
