using Microsoft.EntityFrameworkCore;
using MoodLog.Core.Entities;
using MoodLog.Core.Interfaces;
using MoodLog.Infrastructure.Data;

namespace MoodLog.Infrastructure.Repositories;

public class MoodEntryRepository : Repository<MoodEntry>, IMoodEntryRepository
{
    public MoodEntryRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<MoodEntry>> GetByUserIdAsync(int userId)
    {
        return await _dbSet
            .Include(me => me.MoodEntryTags)
            .ThenInclude(met => met.MoodTag)
            .Where(me => me.UserId == userId)
            .OrderByDescending(me => me.EntryDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<MoodEntry>> GetByUserIdAndDateRangeAsync(int userId, DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Include(me => me.MoodEntryTags)
            .ThenInclude(met => met.MoodTag)
            .Where(me => me.UserId == userId && 
                        me.EntryDate >= startDate.Date && 
                        me.EntryDate <= endDate.Date)
            .OrderByDescending(me => me.EntryDate)
            .ToListAsync();
    }

    public async Task<MoodEntry?> GetByUserIdAndDateAsync(int userId, DateTime date)
    {
        return await _dbSet
            .Include(me => me.MoodEntryTags)
            .ThenInclude(met => met.MoodTag)
            .FirstOrDefaultAsync(me => me.UserId == userId && me.EntryDate.Date == date.Date);
    }

    public async Task<IEnumerable<MoodEntry>> GetRecentEntriesAsync(int userId, int count = 10)
    {
        return await _dbSet
            .Include(me => me.MoodEntryTags)
            .ThenInclude(met => met.MoodTag)
            .Where(me => me.UserId == userId)
            .OrderByDescending(me => me.EntryDate)
            .Take(count)
            .ToListAsync();
    }

    public async Task<double> GetAverageMoodForUserAsync(int userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _dbSet.Where(me => me.UserId == userId);

        if (startDate.HasValue)
            query = query.Where(me => me.EntryDate >= startDate.Value.Date);

        if (endDate.HasValue)
            query = query.Where(me => me.EntryDate <= endDate.Value.Date);

        var entries = await query.ToListAsync();
        
        return entries.Any() ? entries.Average(me => me.MoodLevel) : 0;
    }

    public override async Task<MoodEntry?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(me => me.MoodEntryTags)
            .ThenInclude(met => met.MoodTag)
            .FirstOrDefaultAsync(me => me.Id == id);
    }
}
