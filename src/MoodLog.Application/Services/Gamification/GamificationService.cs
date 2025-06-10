using MoodLog.Core.Entities;
using MoodLog.Core.Interfaces;

namespace MoodLog.Application.Services.Gamification;

public interface IGamificationService
{
    Task<List<UserAchievement>> CheckAndAwardAchievementsAsync(int userId);
    Task<UserStats> GetUserStatsAsync(int userId);
    Task UpdateStreaksAsync(int userId, DateTime entryDate);
    Task<List<Achievement>> GetAvailableAchievementsAsync();
    Task<List<UserAchievement>> GetUserAchievementsAsync(int userId);
    Task<int> CalculateUserPointsAsync(int userId);
}

public class GamificationService : IGamificationService
{
    private readonly IUnitOfWork _unitOfWork;
    
    // Achievement event delegates
    public delegate Task AchievementEarnedHandler(int userId, Achievement achievement);
    public delegate Task StreakUpdatedHandler(int userId, UserStreak streak);
    public delegate Task MilestoneReachedHandler(int userId, string milestone, int value);
    
    public event AchievementEarnedHandler? AchievementEarned;
    public event StreakUpdatedHandler? StreakUpdated;
    public event MilestoneReachedHandler? MilestoneReached;

    public GamificationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<UserAchievement>> CheckAndAwardAchievementsAsync(int userId)
    {
        var newAchievements = new List<UserAchievement>();
        var availableAchievements = await GetAvailableAchievementsAsync();
        var userAchievements = await GetUserAchievementsAsync(userId);
        var completedAchievementIds = userAchievements.Where(ua => ua.IsCompleted).Select(ua => ua.AchievementId).ToHashSet();
        
        var userEntries = await _unitOfWork.MoodEntries.GetByUserIdAsync(userId);
        var userStats = await GetUserStatsAsync(userId);

        foreach (var achievement in availableAchievements.Where(a => !completedAchievementIds.Contains(a.Id)))
        {
            var progress = await CalculateAchievementProgress(userId, achievement, userEntries.ToList(), userStats);
            var existingUserAchievement = userAchievements.FirstOrDefault(ua => ua.AchievementId == achievement.Id);

            if (progress >= achievement.RequiredValue)
            {
                // Award achievement
                if (existingUserAchievement == null)
                {
                    var newUserAchievement = new UserAchievement
                    {
                        UserId = userId,
                        AchievementId = achievement.Id,
                        EarnedAt = DateTime.UtcNow,
                        Progress = progress,
                        IsCompleted = true,
                        Achievement = achievement
                    };
                    
                    await _unitOfWork.UserAchievements.AddAsync(newUserAchievement);
                    newAchievements.Add(newUserAchievement);
                    
                    // Fire achievement earned event
                    if (AchievementEarned != null)
                        await AchievementEarned(userId, achievement);
                }
                else if (!existingUserAchievement.IsCompleted)
                {
                    existingUserAchievement.IsCompleted = true;
                    existingUserAchievement.EarnedAt = DateTime.UtcNow;
                    existingUserAchievement.Progress = progress;
                    
                    await _unitOfWork.UserAchievements.UpdateAsync(existingUserAchievement);
                    newAchievements.Add(existingUserAchievement);
                    
                    if (AchievementEarned != null)
                        await AchievementEarned(userId, achievement);
                }
            }
            else
            {
                // Update progress
                if (existingUserAchievement == null)
                {
                    var progressUserAchievement = new UserAchievement
                    {
                        UserId = userId,
                        AchievementId = achievement.Id,
                        Progress = progress,
                        IsCompleted = false,
                        Achievement = achievement
                    };
                    
                    await _unitOfWork.UserAchievements.AddAsync(progressUserAchievement);
                }
                else if (existingUserAchievement.Progress != progress)
                {
                    existingUserAchievement.Progress = progress;
                    await _unitOfWork.UserAchievements.UpdateAsync(existingUserAchievement);
                }
            }
        }

        await _unitOfWork.SaveChangesAsync();
        return newAchievements;
    }

    public async Task<UserStats> GetUserStatsAsync(int userId)
    {
        var existingStats = await _unitOfWork.UserStats.GetByUserIdAsync(userId);
        if (existingStats != null && existingStats.LastUpdated > DateTime.UtcNow.AddHours(-1))
        {
            return existingStats; // Return cached stats if updated within last hour
        }

        var entries = await _unitOfWork.MoodEntries.GetByUserIdAsync(userId);
        var entriesList = entries.ToList();
        var achievements = await GetUserAchievementsAsync(userId);
        var streaks = await _unitOfWork.UserStreaks.GetByUserIdAsync(userId);

        var stats = new UserStats
        {
            UserId = userId,
            TotalEntries = entriesList.Count,
            TotalPoints = await CalculateUserPointsAsync(userId),
            AchievementsEarned = achievements.Count(a => a.IsCompleted),
            CurrentDailyStreak = streaks.FirstOrDefault(s => s.StreakType == "daily_logging")?.CurrentStreak ?? 0,
            LongestDailyStreak = streaks.FirstOrDefault(s => s.StreakType == "daily_logging")?.LongestStreak ?? 0,
            LastUpdated = DateTime.UtcNow
        };

        if (entriesList.Any())
        {
            stats.AverageMoodAllTime = (float)entriesList.Average(e => e.MoodLevel);
            stats.FirstEntryDate = entriesList.Min(e => e.EntryDate);
            stats.LastEntryDate = entriesList.Max(e => e.EntryDate);

            var last30Days = entriesList.Where(e => e.EntryDate >= DateTime.Today.AddDays(-30)).ToList();
            if (last30Days.Any())
                stats.AverageMoodLast30Days = (float)last30Days.Average(e => e.MoodLevel);

            var last7Days = entriesList.Where(e => e.EntryDate >= DateTime.Today.AddDays(-7)).ToList();
            if (last7Days.Any())
                stats.AverageMoodLast7Days = (float)last7Days.Average(e => e.MoodLevel);

            // Calculate favorite tag
            var allTags = entriesList.SelectMany(e => e.MoodEntryTags.Select(t => t.MoodTag.Name)).ToList();
            if (allTags.Any())
            {
                stats.FavoriteTag = allTags.GroupBy(t => t)
                    .OrderByDescending(g => g.Count())
                    .First().Key;
            }

            // Calculate consistency score (0-100)
            stats.ConsistencyScore = CalculateConsistencyScore(entriesList);
        }

        if (existingStats == null)
        {
            await _unitOfWork.UserStats.AddAsync(stats);
        }
        else
        {
            existingStats.TotalEntries = stats.TotalEntries;
            existingStats.TotalPoints = stats.TotalPoints;
            existingStats.AchievementsEarned = stats.AchievementsEarned;
            existingStats.CurrentDailyStreak = stats.CurrentDailyStreak;
            existingStats.LongestDailyStreak = stats.LongestDailyStreak;
            existingStats.AverageMoodAllTime = stats.AverageMoodAllTime;
            existingStats.AverageMoodLast30Days = stats.AverageMoodLast30Days;
            existingStats.AverageMoodLast7Days = stats.AverageMoodLast7Days;
            existingStats.LastEntryDate = stats.LastEntryDate;
            existingStats.FavoriteTag = stats.FavoriteTag;
            existingStats.ConsistencyScore = stats.ConsistencyScore;
            existingStats.LastUpdated = stats.LastUpdated;
            
            await _unitOfWork.UserStats.UpdateAsync(existingStats);
            stats = existingStats;
        }

        await _unitOfWork.SaveChangesAsync();
        return stats;
    }

    public async Task UpdateStreaksAsync(int userId, DateTime entryDate)
    {
        var dailyStreak = await GetOrCreateStreakAsync(userId, "daily_logging");
        
        var yesterday = entryDate.AddDays(-1).Date;
        var today = entryDate.Date;

        if (dailyStreak.LastActivityDate.Date == yesterday)
        {
            // Continue streak
            dailyStreak.CurrentStreak++;
            dailyStreak.LastActivityDate = today;
            dailyStreak.IsActive = true;
        }
        else if (dailyStreak.LastActivityDate.Date == today)
        {
            // Same day entry, no change to streak
            return;
        }
        else if (dailyStreak.LastActivityDate.Date < yesterday)
        {
            // Streak broken, start new one
            dailyStreak.CurrentStreak = 1;
            dailyStreak.StreakStartDate = today;
            dailyStreak.LastActivityDate = today;
            dailyStreak.IsActive = true;
        }

        // Update longest streak if current is higher
        if (dailyStreak.CurrentStreak > dailyStreak.LongestStreak)
        {
            dailyStreak.LongestStreak = dailyStreak.CurrentStreak;
            
            // Fire milestone event for streak milestones
            if (MilestoneReached != null && dailyStreak.CurrentStreak % 7 == 0)
                await MilestoneReached(userId, "daily_streak", dailyStreak.CurrentStreak);
        }

        dailyStreak.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.UserStreaks.UpdateAsync(dailyStreak);

        // Fire streak updated event
        if (StreakUpdated != null)
            await StreakUpdated(userId, dailyStreak);

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<List<Achievement>> GetAvailableAchievementsAsync()
    {
        var achievements = await _unitOfWork.Achievements.GetAllAsync();
        return achievements.ToList();
    }

    public async Task<List<UserAchievement>> GetUserAchievementsAsync(int userId)
    {
        return await _unitOfWork.UserAchievements.GetByUserIdAsync(userId);
    }

    public async Task<int> CalculateUserPointsAsync(int userId)
    {
        var userAchievements = await GetUserAchievementsAsync(userId);
        var completedAchievements = userAchievements.Where(ua => ua.IsCompleted).ToList();
        
        var achievementPoints = completedAchievements.Sum(ua => ua.Achievement.PointsValue);
        
        // Bonus points for entries and streaks
        var entries = await _unitOfWork.MoodEntries.GetByUserIdAsync(userId);
        var entryPoints = entries.Count() * 10; // 10 points per entry
        
        var streaks = await _unitOfWork.UserStreaks.GetByUserIdAsync(userId);
        var streakPoints = streaks.Sum(s => s.LongestStreak * 5); // 5 points per day in longest streaks
        
        return achievementPoints + entryPoints + streakPoints;
    }

    private async Task<UserStreak> GetOrCreateStreakAsync(int userId, string streakType)
    {
        var streaks = await _unitOfWork.UserStreaks.GetByUserIdAsync(userId);
        var streak = streaks.FirstOrDefault(s => s.StreakType == streakType);
        
        if (streak == null)
        {
            streak = new UserStreak
            {
                UserId = userId,
                StreakType = streakType,
                CurrentStreak = 0,
                LongestStreak = 0,
                LastActivityDate = DateTime.MinValue,
                StreakStartDate = DateTime.UtcNow,
                IsActive = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            await _unitOfWork.UserStreaks.AddAsync(streak);
            await _unitOfWork.SaveChangesAsync();
        }
        
        return streak;
    }

    private async Task<int> CalculateAchievementProgress(int userId, Achievement achievement, List<MoodEntry> entries, UserStats stats)
    {
        return achievement.CriteriaType switch
        {
            "streak" => await CalculateStreakProgress(userId, achievement),
            "count" => CalculateCountProgress(entries, achievement),
            "average" => CalculateAverageProgress(entries, achievement),
            "improvement" => CalculateImprovementProgress(entries, achievement),
            _ => 0
        };
    }

    private async Task<int> CalculateStreakProgress(int userId, Achievement achievement)
    {
        var streaks = await _unitOfWork.UserStreaks.GetByUserIdAsync(userId);
        var dailyStreak = streaks.FirstOrDefault(s => s.StreakType == "daily_logging");
        return dailyStreak?.CurrentStreak ?? 0;
    }

    private int CalculateCountProgress(List<MoodEntry> entries, Achievement achievement)
    {
        if (achievement.TimeframeDays == 0)
            return entries.Count;
        
        var cutoffDate = DateTime.Today.AddDays(-achievement.TimeframeDays);
        return entries.Count(e => e.EntryDate >= cutoffDate);
    }

    private int CalculateAverageProgress(List<MoodEntry> entries, Achievement achievement)
    {
        if (!entries.Any()) return 0;
        
        var relevantEntries = achievement.TimeframeDays == 0 
            ? entries 
            : entries.Where(e => e.EntryDate >= DateTime.Today.AddDays(-achievement.TimeframeDays)).ToList();
        
        if (!relevantEntries.Any()) return 0;
        
        var average = relevantEntries.Average(e => e.MoodLevel);
        return (int)Math.Round(average);
    }

    private int CalculateImprovementProgress(List<MoodEntry> entries, Achievement achievement)
    {
        if (entries.Count < 14) return 0; // Need at least 2 weeks of data
        
        var orderedEntries = entries.OrderBy(e => e.EntryDate).ToList();
        var midpoint = orderedEntries.Count / 2;
        
        var firstHalfAverage = orderedEntries.Take(midpoint).Average(e => e.MoodLevel);
        var secondHalfAverage = orderedEntries.Skip(midpoint).Average(e => e.MoodLevel);
        
        var improvement = secondHalfAverage - firstHalfAverage;
        return (int)Math.Max(0, Math.Round(improvement * 10)); // Scale to 0-30 range
    }

    private int CalculateConsistencyScore(List<MoodEntry> entries)
    {
        if (entries.Count < 7) return 0;
        
        var orderedEntries = entries.OrderBy(e => e.EntryDate).ToList();
        var totalDays = (orderedEntries.Last().EntryDate - orderedEntries.First().EntryDate).Days + 1;
        var entryDays = entries.Count;
        
        var consistencyRatio = (double)entryDays / totalDays;
        
        // Bonus for recent activity
        var recentEntries = entries.Count(e => e.EntryDate >= DateTime.Today.AddDays(-7));
        var recentBonus = Math.Min(0.2, recentEntries / 7.0 * 0.2);
        
        return (int)Math.Round((consistencyRatio + recentBonus) * 100);
    }
}
