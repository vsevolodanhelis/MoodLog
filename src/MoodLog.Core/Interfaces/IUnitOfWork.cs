namespace MoodLog.Core.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IMoodEntryRepository MoodEntries { get; }
    IMoodTagRepository MoodTags { get; }
    IAchievementRepository Achievements { get; }
    IUserAchievementRepository UserAchievements { get; }
    IUserStreakRepository UserStreaks { get; }
    IUserStatsRepository UserStats { get; }
    IRepository<T> Repository<T>() where T : class;
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
