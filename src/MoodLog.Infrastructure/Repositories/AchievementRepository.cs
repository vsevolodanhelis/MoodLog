using Microsoft.EntityFrameworkCore;
using MoodLog.Core.Entities;
using MoodLog.Core.Interfaces;
using MoodLog.Infrastructure.Data;

namespace MoodLog.Infrastructure.Repositories;

public class AchievementRepository : Repository<Achievement>, IAchievementRepository
{
    public AchievementRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<Achievement>> GetActiveByCategoryAsync(string category)
    {
        return await _context.Achievements
            .Where(a => a.IsActive && a.Category == category)
            .OrderBy(a => a.RequiredValue)
            .ToListAsync();
    }
}

public class UserAchievementRepository : Repository<UserAchievement>, IUserAchievementRepository
{
    public UserAchievementRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<UserAchievement>> GetByUserIdAsync(int userId)
    {
        return await _context.UserAchievements
            .Include(ua => ua.Achievement)
            .Where(ua => ua.UserId == userId)
            .OrderByDescending(ua => ua.EarnedAt)
            .ToListAsync();
    }

    public async Task<List<UserAchievement>> GetCompletedByUserIdAsync(int userId)
    {
        return await _context.UserAchievements
            .Include(ua => ua.Achievement)
            .Where(ua => ua.UserId == userId && ua.IsCompleted)
            .OrderByDescending(ua => ua.EarnedAt)
            .ToListAsync();
    }

    public async Task<UserAchievement?> GetByUserAndAchievementAsync(int userId, int achievementId)
    {
        return await _context.UserAchievements
            .Include(ua => ua.Achievement)
            .FirstOrDefaultAsync(ua => ua.UserId == userId && ua.AchievementId == achievementId);
    }
}

public class UserStreakRepository : Repository<UserStreak>, IUserStreakRepository
{
    public UserStreakRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<UserStreak>> GetByUserIdAsync(int userId)
    {
        return await _context.UserStreaks
            .Where(us => us.UserId == userId)
            .OrderByDescending(us => us.CurrentStreak)
            .ToListAsync();
    }

    public async Task<UserStreak?> GetByUserAndTypeAsync(int userId, string streakType)
    {
        return await _context.UserStreaks
            .FirstOrDefaultAsync(us => us.UserId == userId && us.StreakType == streakType);
    }

    public async Task<List<UserStreak>> GetActiveStreaksAsync()
    {
        return await _context.UserStreaks
            .Where(us => us.IsActive)
            .OrderByDescending(us => us.CurrentStreak)
            .ToListAsync();
    }
}

public class UserStatsRepository : Repository<UserStats>, IUserStatsRepository
{
    public UserStatsRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<UserStats?> GetByUserIdAsync(int userId)
    {
        return await _context.UserStats
            .FirstOrDefaultAsync(us => us.UserId == userId);
    }

    public async Task<List<UserStats>> GetTopUsersByPointsAsync(int count = 10)
    {
        return await _context.UserStats
            .OrderByDescending(us => us.TotalPoints)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<UserStats>> GetTopUsersByStreakAsync(int count = 10)
    {
        return await _context.UserStats
            .OrderByDescending(us => us.CurrentDailyStreak)
            .Take(count)
            .ToListAsync();
    }
}
