namespace HabiBot.Domain.Interfaces;

/// <summary>
/// Интерфейс паттерна Unit of Work для управления транзакциями
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Репозиторий пользователей
    /// </summary>
    IUserRepository Users { get; }

    /// <summary>
    /// Репозиторий привычек
    /// </summary>
    IHabitRepository Habits { get; }

    /// <summary>
    /// Репозиторий записей выполнения привычек
    /// </summary>
    IHabitLogRepository HabitLogs { get; }

    /// <summary>
    /// Сохранить все изменения в базу данных
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Начать транзакцию базы данных
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Зафиксировать транзакцию базы данных
    /// </summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Откатить транзакцию базы данных
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
