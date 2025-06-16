using MoodLog.Application.Services;
using MoodLog.Core.DTOs;

namespace MoodLog.Application.Interfaces;

public interface IValidationService
{
    ValidationResult ValidateMoodEntry(MoodEntryCreateDto dto);
    ValidationResult ValidateMoodEntryUpdate(MoodEntryUpdateDto dto);
    ValidationResult ValidateMoodTag(MoodTagCreateDto dto);
    bool IsValidDateRange(DateTime startDate, DateTime endDate);
    bool IsReasonableDataRequest(DateTime startDate, DateTime endDate);
}
