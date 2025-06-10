using MoodLog.Core.DTOs;

namespace MoodLog.Application.Interfaces;

public interface IMoodTagService
{
    Task<IEnumerable<MoodTagDto>> GetAllActiveAsync();
    Task<IEnumerable<MoodTagDto>> GetSystemTagsAsync();
    Task<IEnumerable<MoodTagDto>> GetUserTagsAsync();
    Task<MoodTagDto?> GetByIdAsync(int id);
    Task<MoodTagDto> CreateAsync(MoodTagCreateDto dto, bool isSystemTag = false);
    Task<MoodTagDto> UpdateAsync(MoodTagUpdateDto dto);
    Task<bool> DeleteAsync(int id);
    Task<bool> TagExistsAsync(string name);
}
