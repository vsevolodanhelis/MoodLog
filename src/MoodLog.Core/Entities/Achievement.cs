namespace MoodLog.Core.Entities;

public class Achievement
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconClass { get; set; } = string.Empty; // FontAwesome icon class
    public string Category { get; set; } = string.Empty; // "consistency", "progress", "milestone", "self-care"
    public int PointsValue { get; set; }
    public string BadgeColor { get; set; } = string.Empty; // CSS color for badge
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    
    // Achievement criteria
    public string CriteriaType { get; set; } = string.Empty; // "streak", "count", "average", "improvement"
    public int RequiredValue { get; set; }
    public int TimeframeDays { get; set; } // 0 = all time, 7 = weekly, 30 = monthly
    
    public virtual ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();
}

public class UserAchievement
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int AchievementId { get; set; }
    public DateTime EarnedAt { get; set; }
    public int Progress { get; set; } // Current progress towards achievement
    public bool IsCompleted { get; set; }
    public string? CompletionNote { get; set; }
    
    public virtual Achievement Achievement { get; set; } = null!;
}

public class UserStreak
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string StreakType { get; set; } = string.Empty; // "daily_logging", "mood_improvement", "consistency"
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public DateTime LastActivityDate { get; set; }
    public DateTime StreakStartDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UserStats
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int TotalEntries { get; set; }
    public int TotalPoints { get; set; }
    public int AchievementsEarned { get; set; }
    public int CurrentDailyStreak { get; set; }
    public int LongestDailyStreak { get; set; }
    public float AverageMoodAllTime { get; set; }
    public float AverageMoodLast30Days { get; set; }
    public float AverageMoodLast7Days { get; set; }
    public DateTime FirstEntryDate { get; set; }
    public DateTime LastEntryDate { get; set; }
    public DateTime LastUpdated { get; set; }
    
    // Engagement metrics
    public int WeeklyGoalStreak { get; set; } // Consecutive weeks meeting entry goals
    public int MoodImprovementStreak { get; set; } // Consecutive periods of mood improvement
    public string FavoriteTag { get; set; } = string.Empty;
    public int ConsistencyScore { get; set; } // 0-100 based on regular logging
}
