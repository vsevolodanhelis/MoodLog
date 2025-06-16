using MoodLog.Core.DTOs;

namespace MoodLog.Application.Interfaces;

public interface IMoodAnalyticsService
{
    Task<MoodAnalyticsDto> GetMoodAnalyticsAsync(int userId, DateTime? startDate = null, DateTime? endDate = null);
    Task<List<MoodStreakDto>> GetMoodStreaksAsync(int userId);
    Task<List<MoodPatternDto>> GetMoodPatternsAsync(int userId);
}
