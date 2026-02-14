using HabiBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HabiBot.Infrastructure.Data.Configurations;

/// <summary>
/// Конфигурация записей выполнения привычек
/// </summary>
public class HabitLogConfiguration : IEntityTypeConfiguration<HabitLog>
{
    public void Configure(EntityTypeBuilder<HabitLog> builder)
    {
        builder.ToTable("HabitLogs");

        builder.HasKey(hl => hl.Id);

        builder.Property(hl => hl.HabitId)
            .IsRequired();

        builder.Property(hl => hl.CompletedAt)
            .IsRequired();

        builder.Property(hl => hl.Notes)
            .HasMaxLength(1000);

        builder.Property(hl => hl.CreatedAt)
            .IsRequired();

        builder.Property(hl => hl.UpdatedAt)
            .IsRequired();

        builder.Property(hl => hl.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Индекс для быстрого поиска логов привычки
        builder.HasIndex(hl => hl.HabitId);

        // Индекс для поиска по дате выполнения
        builder.HasIndex(hl => hl.CompletedAt);

        // Композитный индекс для проверки выполнения в конкретную дату
        builder.HasIndex(hl => new { hl.HabitId, hl.CompletedAt });

        // Связь многие-к-одному с привычкой
        builder.HasOne(hl => hl.Habit)
            .WithMany(h => h.Logs)
            .HasForeignKey(hl => hl.HabitId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
