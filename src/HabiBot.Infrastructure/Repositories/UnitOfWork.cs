using HabiBot.Domain.Interfaces;
using HabiBot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace HabiBot.Infrastructure.Repositories;

/// <summary>
/// Реализация паттерна Unit of Work
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly HabiBotDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(
        HabiBotDbContext context,
        IUserRepository users,
        IHabitRepository habits,
        IHabitLogRepository habitLogs)
    {
        _context = context;
        Users = users;
        Habits = habits;
        HabitLogs = habitLogs;
    }

    public IUserRepository Users { get; }
    public IHabitRepository Habits { get; }
    public IHabitLogRepository HabitLogs { get; }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
