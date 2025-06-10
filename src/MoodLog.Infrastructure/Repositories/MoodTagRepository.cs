using Microsoft.EntityFrameworkCore;
using MoodLog.Core.Entities;
using MoodLog.Core.Interfaces;
using MoodLog.Infrastructure.Data;

namespace MoodLog.Infrastructure.Repositories;

public class MoodTagRepository : Repository<MoodTag>, IMoodTagRepository
{
    public MoodTagRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<MoodTag>> GetActiveTagsAsync()
    {
        return await _dbSet
            .Where(mt => mt.IsActive)
            .OrderBy(mt => mt.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<MoodTag>> GetSystemTagsAsync()
    {
        return await _dbSet
            .Where(mt => mt.IsSystemTag && mt.IsActive)
            .OrderBy(mt => mt.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<MoodTag>> GetUserTagsAsync()
    {
        return await _dbSet
            .Where(mt => !mt.IsSystemTag && mt.IsActive)
            .OrderBy(mt => mt.Name)
            .ToListAsync();
    }

    public async Task<MoodTag?> GetByNameAsync(string name)
    {
        return await _dbSet
            .FirstOrDefaultAsync(mt => mt.Name.ToLower() == name.ToLower());
    }
}
