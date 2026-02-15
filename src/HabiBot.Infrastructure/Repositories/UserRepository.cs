using HabiBot.Domain.Entities;
using HabiBot.Domain.Interfaces;
using HabiBot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HabiBot.Infrastructure.Repositories;

/// <summary>
/// Реализация репозитория пользователей
/// </summary>
public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(HabiBotDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.TelegramUserId == telegramId, cancellationToken);
    }

    public async Task<bool> ExistsByTelegramIdAsync(long telegramId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(u => u.TelegramUserId == telegramId, cancellationToken);
    }
}
