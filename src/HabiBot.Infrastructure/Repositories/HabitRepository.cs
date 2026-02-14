using HabiBot.Domain.Entities;
using HabiBot.Domain.Interfaces;
using HabiBot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HabiBot.Infrastructure.Repositories;

/// <summary>
/// Реализация репозитория привычек
/// </summary>
public class HabitRepository : Repository<Habit>, IHabitRepository
{
    public HabitRepository(HabiBotDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Habit>> GetUserHabitsAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(h => h.UserId == userId)
            .OrderBy(h => h.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Habit?> GetByIdWithLogsAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(h => h.Logs)
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);
    }
}
