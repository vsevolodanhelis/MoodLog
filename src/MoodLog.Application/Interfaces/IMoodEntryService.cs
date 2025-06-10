using MoodLog.Core.DTOs;

namespace MoodLog.Application.Interfaces;

public interface IMoodEntryService
{
    Task<MoodEntryDto?> GetByIdAsync(int id, int userId);
    Task<IEnumerable<MoodEntryDto>> GetByUserIdAsync(int userId);
    Task<IEnumerable<MoodEntryDto>> GetByDateRangeAsync(int userId, DateTime startDate, DateTime endDate);
    Task<MoodEntryDto?> GetByDateAsync(int userId, DateTime date);
    Task<IEnumerable<MoodEntryDto>> GetRecentEntriesAsync(int userId, int count = 10);
    Task<MoodEntryDto> CreateAsync(MoodEntryCreateDto dto, int userId);
    Task<MoodEntryDto> UpdateAsync(MoodEntryUpdateDto dto, int userId);
    Task<bool> DeleteAsync(int id, int userId);
    Task<double> GetAverageMoodAsync(int userId, DateTime? startDate = null, DateTime? endDate = null);
    Task<bool> CanUserAccessEntryAsync(int entryId, int userId);
}
