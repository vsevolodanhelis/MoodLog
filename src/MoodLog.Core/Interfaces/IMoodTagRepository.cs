using MoodLog.Core.Entities;

namespace MoodLog.Core.Interfaces;

public interface IMoodTagRepository : IRepository<MoodTag>
{
    Task<IEnumerable<MoodTag>> GetActiveTagsAsync();
    Task<IEnumerable<MoodTag>> GetSystemTagsAsync();
    Task<IEnumerable<MoodTag>> GetUserTagsAsync();
    Task<MoodTag?> GetByNameAsync(string name);
}
