using Microsoft.EntityFrameworkCore.Storage;
using MoodLog.Core.Interfaces;
using MoodLog.Infrastructure.Data;

namespace MoodLog.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly Dictionary<Type, object> _repositories;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
        _repositories = new Dictionary<Type, object>();

        MoodEntries = new MoodEntryRepository(_context);
        MoodTags = new MoodTagRepository(_context);
        Achievements = new AchievementRepository(_context);
        UserAchievements = new UserAchievementRepository(_context);
        UserStreaks = new UserStreakRepository(_context);
        UserStats = new UserStatsRepository(_context);
    }

    public IMoodEntryRepository MoodEntries { get; }
    public IMoodTagRepository MoodTags { get; }
    public IAchievementRepository Achievements { get; }
    public IUserAchievementRepository UserAchievements { get; }
    public IUserStreakRepository UserStreaks { get; }
    public IUserStatsRepository UserStats { get; }

    public IRepository<T> Repository<T>() where T : class
    {
        var type = typeof(T);
        
        if (!_repositories.ContainsKey(type))
        {
            _repositories[type] = new Repository<T>(_context);
        }
        
        return (IRepository<T>)_repositories[type];
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
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
