using HabiBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HabiBot.Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация сущности пользователя
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.TelegramUserId)
            .IsRequired();

        builder.HasIndex(u => u.TelegramUserId)
            .IsUnique();

        builder.Property(u => u.TelegramChatId)
            .IsRequired();

        builder.Property(u => u.TelegramFirstName)
            .HasMaxLength(100);

        builder.Property(u => u.TelegramLastName)
            .HasMaxLength(100);

        builder.Property(u => u.TelegramUserName)
            .HasMaxLength(100);

        builder.Property(u => u.RegisteredAt)
            .IsRequired();

        builder.Property(u => u.TimeZone)
            .HasMaxLength(100);

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Property(u => u.UpdatedAt)
            .IsRequired();

        builder.Property(u => u.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Связь один-ко-многим с привычками
        builder.HasMany(u => u.Habits)
            .WithOne(h => h.User)
            .HasForeignKey(h => h.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
