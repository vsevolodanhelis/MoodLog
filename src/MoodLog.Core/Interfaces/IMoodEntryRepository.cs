using MoodLog.Core.Entities;

namespace MoodLog.Core.Interfaces;

public interface IMoodEntryRepository : IRepository<MoodEntry>
{
    Task<IEnumerable<MoodEntry>> GetByUserIdAsync(int userId);
    Task<IEnumerable<MoodEntry>> GetByUserIdAndDateRangeAsync(int userId, DateTime startDate, DateTime endDate);
    Task<MoodEntry?> GetByUserIdAndDateAsync(int userId, DateTime date);
    Task<IEnumerable<MoodEntry>> GetRecentEntriesAsync(int userId, int count = 10);
    Task<double> GetAverageMoodForUserAsync(int userId, DateTime? startDate = null, DateTime? endDate = null);
}
