using HabiBot.Domain.Entities;
using HabiBot.Domain.Interfaces;
using HabiBot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HabiBot.Infrastructure.Repositories;

/// <summary>
/// Реализация репозитория записей выполнения привычек
/// </summary>
public class HabitLogRepository : Repository<HabitLog>, IHabitLogRepository
{
    public HabitLogRepository(HabiBotDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<HabitLog>> GetHabitLogsAsync(long habitId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(hl => hl.HabitId == habitId)
            .OrderByDescending(hl => hl.CompletedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<HabitLog>> GetHabitLogsByDateRangeAsync(
        long habitId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(hl => hl.HabitId == habitId &&
                         hl.CompletedAt >= startDate &&
                         hl.CompletedAt <= endDate)
            .OrderBy(hl => hl.CompletedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsCompletedOnDateAsync(
        long habitId,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        return await _dbSet
            .AnyAsync(hl => hl.HabitId == habitId &&
                           hl.CompletedAt >= startOfDay &&
                           hl.CompletedAt < endOfDay,
                     cancellationToken);
    }
}
