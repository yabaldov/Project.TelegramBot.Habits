using HabiBot.Domain.Entities;
using HabiBot.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HabiBot.Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация сущности привычки
/// </summary>
public class HabitConfiguration : IEntityTypeConfiguration<Habit>
{
    public void Configure(EntityTypeBuilder<Habit> builder)
    {
        builder.ToTable("Habits");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Name)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(h => h.UserId)
            .IsRequired();

        builder.Property(h => h.ReminderTime)
            .HasMaxLength(5); // "HH:mm"

        builder.Property(h => h.Frequency)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(HabitFrequency.Daily);

        builder.Property(h => h.CreatedAt)
            .IsRequired();

        builder.Property(h => h.UpdatedAt)
            .IsRequired();

        builder.Property(h => h.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Индекс для быстрого поиска привычек пользователя
        builder.HasIndex(h => h.UserId);

        // Связь многие-к-одному с пользователем
        builder.HasOne(h => h.User)
            .WithMany(u => u.Habits)
            .HasForeignKey(h => h.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Связь один-ко-многим с логами
        builder.HasMany(h => h.Logs)
            .WithOne(hl => hl.Habit)
            .HasForeignKey(hl => hl.HabitId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
