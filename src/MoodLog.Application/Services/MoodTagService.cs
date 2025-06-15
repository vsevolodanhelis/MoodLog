using MoodLog.Application.Interfaces;
using MoodLog.Core.DTOs;
using MoodLog.Core.Entities;
using MoodLog.Core.Interfaces;

namespace MoodLog.Application.Services;

public class MoodTagService : IMoodTagService
{
    private readonly IUnitOfWork _unitOfWork;

    public MoodTagService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<MoodTagDto>> GetAllActiveAsync()
    {
        var tags = await _unitOfWork.MoodTags.GetActiveTagsAsync();
        return tags.Select(MapToDto);
    }

    public async Task<IEnumerable<MoodTagDto>> GetSystemTagsAsync()
    {
        var tags = await _unitOfWork.MoodTags.GetSystemTagsAsync();
        return tags.Select(MapToDto);
    }

    public async Task<IEnumerable<MoodTagDto>> GetUserTagsAsync()
    {
        var tags = await _unitOfWork.MoodTags.GetUserTagsAsync();
        return tags.Select(MapToDto);
    }

    public async Task<MoodTagDto?> GetByIdAsync(int id)
    {
        var tag = await _unitOfWork.MoodTags.GetByIdAsync(id);
        return tag != null ? MapToDto(tag) : null;
    }

    public async Task<MoodTagDto> CreateAsync(MoodTagCreateDto dto, bool isSystemTag = false)
    {
        // Validate input
        ValidateTagInput(dto.Name, dto.Color);

        // Check if tag with same name already exists
        var existingTag = await _unitOfWork.MoodTags.GetByNameAsync(dto.Name);
        if (existingTag != null)
        {
            throw new InvalidOperationException($"A tag with the name '{dto.Name}' already exists");
        }

        var tag = new MoodTag
        {
            Name = dto.Name,
            Description = dto.Description,
            Color = dto.Color,
            IsSystemTag = isSystemTag,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.MoodTags.AddAsync(tag);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(tag);
    }

    public async Task<MoodTagDto> UpdateAsync(MoodTagUpdateDto dto)
    {
        // Validate input
        ValidateTagInput(dto.Name, dto.Color);

        var tag = await _unitOfWork.MoodTags.GetByIdAsync(dto.Id);

        if (tag == null)
            throw new ArgumentException("Tag not found");

        // Check if another tag with the same name exists
        var existingTag = await _unitOfWork.MoodTags.GetByNameAsync(dto.Name);
        if (existingTag != null && existingTag.Id != dto.Id)
        {
            throw new InvalidOperationException($"A tag with the name '{dto.Name}' already exists");
        }

        tag.Name = dto.Name;
        tag.Description = dto.Description;
        tag.Color = dto.Color;
        tag.IsActive = dto.IsActive;

        await _unitOfWork.MoodTags.UpdateAsync(tag);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(tag);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var tag = await _unitOfWork.MoodTags.GetByIdAsync(id);
        
        if (tag == null)
            return false;

        // Don't allow deletion of system tags, just deactivate them
        if (tag.IsSystemTag)
        {
            tag.IsActive = false;
            await _unitOfWork.MoodTags.UpdateAsync(tag);
        }
        else
        {
            await _unitOfWork.MoodTags.DeleteAsync(tag);
        }
        
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> TagExistsAsync(string name)
    {
        var tag = await _unitOfWork.MoodTags.GetByNameAsync(name);
        return tag != null;
    }

    private static void ValidateTagInput(string name, string color)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Tag name cannot be empty");
        }

        if (!IsValidHexColor(color))
        {
            throw new ArgumentException("Invalid color format. Must be a valid hex color (e.g., #FF0000)");
        }
    }

    private static bool IsValidHexColor(string color)
    {
        if (string.IsNullOrWhiteSpace(color))
            return false;

        if (!color.StartsWith("#"))
            return false;

        if (color.Length != 7)
            return false;

        return color[1..].All(c => char.IsDigit(c) || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f'));
    }

    private static MoodTagDto MapToDto(MoodTag tag)
    {
        return new MoodTagDto
        {
            Id = tag.Id,
            Name = tag.Name,
            Description = tag.Description,
            Color = tag.Color,
            IsSystemTag = tag.IsSystemTag,
            IsActive = tag.IsActive
        };
    }
}
