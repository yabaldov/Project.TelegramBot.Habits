using HabiBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HabiBot.Infrastructure.Data;

/// <summary>
/// Контекст базы данных приложения
/// </summary>
public class HabiBotDbContext : DbContext
{
    public HabiBotDbContext(DbContextOptions<HabiBotDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Пользователи
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// Привычки
    /// </summary>
    public DbSet<Habit> Habits => Set<Habit>();

    /// <summary>
    /// Записи выполнения привычек
    /// </summary>
    public DbSet<HabitLog> HabitLogs => Set<HabitLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Применяем все конфигурации из текущей сборки
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HabiBotDbContext).Assembly);

        // Глобальный фильтр для мягкого удаления
        modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
        modelBuilder.Entity<Habit>().HasQueryFilter(h => !h.IsDeleted);
        modelBuilder.Entity<HabitLog>().HasQueryFilter(hl => !hl.IsDeleted);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Автоматически устанавливаем CreatedAt и UpdatedAt
        var entries = ChangeTracker.Entries<BaseEntity>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
