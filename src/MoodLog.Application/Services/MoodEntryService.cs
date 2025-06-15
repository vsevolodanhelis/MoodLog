using MoodLog.Application.Interfaces;
using MoodLog.Core.DTOs;
using MoodLog.Core.Entities;
using MoodLog.Core.Interfaces;
using MoodLog.Core.Events;

namespace MoodLog.Application.Services;

public class MoodEntryService : IMoodEntryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMoodEntryEventPublisher? _eventPublisher;

    public MoodEntryService(IUnitOfWork unitOfWork, IMoodEntryEventPublisher? eventPublisher = null)
    {
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
    }

    public async Task<MoodEntryDto?> GetByIdAsync(int id, int userId)
    {
        var entry = await _unitOfWork.MoodEntries.GetByIdAsync(id);
        
        if (entry == null || entry.UserId != userId)
            return null;

        return MapToDto(entry);
    }

    public async Task<IEnumerable<MoodEntryDto>> GetByUserIdAsync(int userId)
    {
        var entries = await _unitOfWork.MoodEntries.GetByUserIdAsync(userId);
        return entries.Select(MapToDto);
    }

    public async Task<IEnumerable<MoodEntryDto>> GetByDateRangeAsync(int userId, DateTime startDate, DateTime endDate)
    {
        var entries = await _unitOfWork.MoodEntries.GetByUserIdAndDateRangeAsync(userId, startDate, endDate);
        return entries.Select(MapToDto);
    }

    public async Task<MoodEntryDto?> GetByDateAsync(int userId, DateTime date)
    {
        var entry = await _unitOfWork.MoodEntries.GetByUserIdAndDateAsync(userId, date);
        return entry != null ? MapToDto(entry) : null;
    }

    public async Task<IEnumerable<MoodEntryDto>> GetRecentEntriesAsync(int userId, int count = 10)
    {
        var entries = await _unitOfWork.MoodEntries.GetRecentEntriesAsync(userId, count);
        return entries.Select(MapToDto);
    }

    public async Task<MoodEntryDto> CreateAsync(MoodEntryCreateDto dto, int userId)
    {
        // Validate mood level
        if (dto.MoodLevel < 1 || dto.MoodLevel > 10)
        {
            throw new ArgumentException("Mood level must be between 1 and 10");
        }

        // Check if entry already exists for this date
        var existingEntry = await _unitOfWork.MoodEntries.GetByUserIdAndDateAsync(userId, dto.EntryDate);
        if (existingEntry != null)
        {
            throw new InvalidOperationException($"A mood entry already exists for {dto.EntryDate:yyyy-MM-dd}");
        }

        var entry = new MoodEntry
        {
            UserId = userId,
            MoodLevel = dto.MoodLevel,
            Notes = dto.Notes,
            Symptoms = dto.Symptoms,
            EntryDate = dto.EntryDate.Date,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.MoodEntries.AddAsync(entry);
        await _unitOfWork.SaveChangesAsync();

        // Add tags if any
        if (dto.TagIds.Any())
        {
            await AddTagsToEntry(entry.Id, dto.TagIds);
        }

        // Reload entry with tags
        var createdEntry = await _unitOfWork.MoodEntries.GetByIdAsync(entry.Id);
        return MapToDto(createdEntry!);
    }

    public async Task<MoodEntryDto> UpdateAsync(MoodEntryUpdateDto dto, int userId)
    {
        // Validate mood level
        if (dto.MoodLevel < 1 || dto.MoodLevel > 10)
        {
            throw new ArgumentException("Mood level must be between 1 and 10");
        }

        var entry = await _unitOfWork.MoodEntries.GetByIdAsync(dto.Id);

        if (entry == null || entry.UserId != userId)
            throw new UnauthorizedAccessException("Entry not found or access denied");

        entry.MoodLevel = dto.MoodLevel;
        entry.Notes = dto.Notes;
        entry.Symptoms = dto.Symptoms;
        entry.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.MoodEntries.UpdateAsync(entry);

        // Update tags
        await UpdateEntryTags(entry.Id, dto.TagIds);
        
        await _unitOfWork.SaveChangesAsync();

        // Reload entry with updated tags
        var updatedEntry = await _unitOfWork.MoodEntries.GetByIdAsync(entry.Id);
        return MapToDto(updatedEntry!);
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var entry = await _unitOfWork.MoodEntries.GetByIdAsync(id);
        
        if (entry == null || entry.UserId != userId)
            return false;

        await _unitOfWork.MoodEntries.DeleteAsync(entry);
        await _unitOfWork.SaveChangesAsync();
        
        return true;
    }

    public async Task<double> GetAverageMoodAsync(int userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        return await _unitOfWork.MoodEntries.GetAverageMoodForUserAsync(userId, startDate, endDate);
    }

    public async Task<bool> CanUserAccessEntryAsync(int entryId, int userId)
    {
        var entry = await _unitOfWork.MoodEntries.GetByIdAsync(entryId);
        return entry != null && entry.UserId == userId;
    }

    private async Task AddTagsToEntry(int entryId, List<int> tagIds)
    {
        foreach (var tagId in tagIds)
        {
            var entryTag = new MoodEntryTag
            {
                MoodEntryId = entryId,
                MoodTagId = tagId
            };
            await _unitOfWork.Repository<MoodEntryTag>().AddAsync(entryTag);
        }
        await _unitOfWork.SaveChangesAsync();
    }

    private async Task UpdateEntryTags(int entryId, List<int> tagIds)
    {
        // Remove existing tags
        var existingTags = await _unitOfWork.Repository<MoodEntryTag>()
            .FindAsync(met => met.MoodEntryId == entryId);
        
        foreach (var tag in existingTags)
        {
            await _unitOfWork.Repository<MoodEntryTag>().DeleteAsync(tag);
        }

        // Add new tags
        foreach (var tagId in tagIds)
        {
            var entryTag = new MoodEntryTag
            {
                MoodEntryId = entryId,
                MoodTagId = tagId
            };
            await _unitOfWork.Repository<MoodEntryTag>().AddAsync(entryTag);
        }
    }

    private static MoodEntryDto MapToDto(MoodEntry entry)
    {
        return new MoodEntryDto
        {
            Id = entry.Id,
            MoodLevel = entry.MoodLevel,
            Notes = entry.Notes ?? string.Empty,
            Symptoms = entry.Symptoms ?? string.Empty,
            EntryDate = entry.EntryDate,
            TagIds = entry.MoodEntryTags?.Select(met => met.MoodTagId).ToList() ?? new List<int>(),
            TagNames = entry.MoodEntryTags?.Where(met => met.MoodTag != null)
                .Select(met => met.MoodTag.Name).ToList() ?? new List<string>()
        };
    }
}
