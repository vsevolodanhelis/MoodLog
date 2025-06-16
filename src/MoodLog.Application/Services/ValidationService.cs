using MoodLog.Application.Interfaces;
using MoodLog.Core.DTOs;
using System.ComponentModel.DataAnnotations;

namespace MoodLog.Application.Services;

public class ValidationService : IValidationService
{
    public ValidationResult ValidateMoodEntry(MoodEntryCreateDto dto)
    {
        var errors = new List<string>();

        // Validate mood level
        if (dto.MoodLevel < 1 || dto.MoodLevel > 10)
        {
            errors.Add("Mood level must be between 1 and 10.");
        }

        // Validate entry date
        if (dto.EntryDate > DateTime.Today)
        {
            errors.Add("Entry date cannot be in the future.");
        }

        if (dto.EntryDate < DateTime.Today.AddYears(-1))
        {
            errors.Add("Entry date cannot be more than 1 year in the past.");
        }

        // Validate notes length
        if (!string.IsNullOrEmpty(dto.Notes) && dto.Notes.Length > 1000)
        {
            errors.Add("Notes cannot exceed 1000 characters.");
        }

        // Validate symptoms length
        if (!string.IsNullOrEmpty(dto.Symptoms) && dto.Symptoms.Length > 500)
        {
            errors.Add("Symptoms cannot exceed 500 characters.");
        }

        // Check for potentially harmful content
        if (ContainsPotentiallyHarmfulContent(dto.Notes) || 
            ContainsPotentiallyHarmfulContent(dto.Symptoms))
        {
            errors.Add("Your entry contains content that may indicate you need immediate support. Please consider reaching out to a mental health professional or crisis helpline.");
        }

        return new ValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors
        };
    }

    public ValidationResult ValidateMoodEntryUpdate(MoodEntryUpdateDto dto)
    {
        var errors = new List<string>();

        // Validate ID
        if (dto.Id <= 0)
        {
            errors.Add("Invalid entry ID.");
        }

        // Validate mood level
        if (dto.MoodLevel < 1 || dto.MoodLevel > 10)
        {
            errors.Add("Mood level must be between 1 and 10.");
        }

        // Validate notes length
        if (!string.IsNullOrEmpty(dto.Notes) && dto.Notes.Length > 1000)
        {
            errors.Add("Notes cannot exceed 1000 characters.");
        }

        // Validate symptoms length
        if (!string.IsNullOrEmpty(dto.Symptoms) && dto.Symptoms.Length > 500)
        {
            errors.Add("Symptoms cannot exceed 500 characters.");
        }

        // Check for potentially harmful content
        if (ContainsPotentiallyHarmfulContent(dto.Notes) || 
            ContainsPotentiallyHarmfulContent(dto.Symptoms))
        {
            errors.Add("Your entry contains content that may indicate you need immediate support. Please consider reaching out to a mental health professional or crisis helpline.");
        }

        return new ValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors
        };
    }

    public ValidationResult ValidateMoodTag(MoodTagCreateDto dto)
    {
        var errors = new List<string>();

        // Validate name
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            errors.Add("Tag name is required.");
        }
        else if (dto.Name.Length > 50)
        {
            errors.Add("Tag name cannot exceed 50 characters.");
        }
        else if (dto.Name.Length < 2)
        {
            errors.Add("Tag name must be at least 2 characters long.");
        }

        // Validate color (if provided)
        if (!string.IsNullOrEmpty(dto.Color) && !IsValidHexColor(dto.Color))
        {
            errors.Add("Color must be a valid hex color code (e.g., #FF5733).");
        }

        // Check for inappropriate content
        if (ContainsInappropriateContent(dto.Name))
        {
            errors.Add("Tag name contains inappropriate content.");
        }

        return new ValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors
        };
    }

    public bool IsValidDateRange(DateTime startDate, DateTime endDate)
    {
        return startDate <= endDate && 
               startDate >= DateTime.Today.AddYears(-5) && 
               endDate <= DateTime.Today;
    }

    public bool IsReasonableDataRequest(DateTime startDate, DateTime endDate)
    {
        var daysDifference = (endDate - startDate).TotalDays;
        return daysDifference <= 365; // Max 1 year of data at once
    }

    private bool ContainsPotentiallyHarmfulContent(string? content)
    {
        if (string.IsNullOrEmpty(content)) return false;

        var harmfulKeywords = new[]
        {
            "suicide", "kill myself", "end it all", "not worth living",
            "hurt myself", "self harm", "cutting", "overdose"
        };

        var lowerContent = content.ToLowerInvariant();
        return harmfulKeywords.Any(keyword => lowerContent.Contains(keyword));
    }

    private bool ContainsInappropriateContent(string content)
    {
        if (string.IsNullOrEmpty(content)) return false;

        var inappropriateWords = new[]
        {
            // Add inappropriate words as needed
            "spam", "test123", "asdf"
        };

        var lowerContent = content.ToLowerInvariant();
        return inappropriateWords.Any(word => lowerContent.Contains(word));
    }

    private bool IsValidHexColor(string color)
    {
        if (string.IsNullOrEmpty(color)) return false;
        
        return System.Text.RegularExpressions.Regex.IsMatch(color, 
            @"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$");
    }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
