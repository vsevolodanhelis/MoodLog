using MoodLog.Core.Entities;

namespace MoodLog.Core.Interfaces;

public interface IAchievementRepository : IRepository<Achievement>
{
    Task<List<Achievement>> GetActiveByCategoryAsync(string category);
}

public interface IUserAchievementRepository : IRepository<UserAchievement>
{
    Task<List<UserAchievement>> GetByUserIdAsync(int userId);
    Task<List<UserAchievement>> GetCompletedByUserIdAsync(int userId);
    Task<UserAchievement?> GetByUserAndAchievementAsync(int userId, int achievementId);
}

public interface IUserStreakRepository : IRepository<UserStreak>
{
    Task<List<UserStreak>> GetByUserIdAsync(int userId);
    Task<UserStreak?> GetByUserAndTypeAsync(int userId, string streakType);
    Task<List<UserStreak>> GetActiveStreaksAsync();
}

public interface IUserStatsRepository : IRepository<UserStats>
{
    Task<UserStats?> GetByUserIdAsync(int userId);
    Task<List<UserStats>> GetTopUsersByPointsAsync(int count = 10);
    Task<List<UserStats>> GetTopUsersByStreakAsync(int count = 10);
}
