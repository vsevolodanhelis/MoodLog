using FluentAssertions;
using MoodLog.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace MoodLog.Core.Tests;

public class MoodEntryTests
{
    [Fact]
    public void MoodEntry_ShouldHaveValidDefaults()
    {
        // Arrange & Act
        var entry = new MoodEntry();

        // Assert
        Assert.Equal(DateTime.UtcNow.Date, entry.EntryDate.Date);
        Assert.True(entry.CreatedAt <= DateTime.UtcNow);
        Assert.Equal(string.Empty, entry.Notes);
        Assert.Equal(string.Empty, entry.Symptoms);
        Assert.NotNull(entry.MoodEntryTags);
        Assert.Empty(entry.MoodEntryTags);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void MoodEntry_ShouldAcceptValidMoodLevels(int moodLevel)
    {
        // Arrange
        var entry = new MoodEntry { MoodLevel = moodLevel };
        var context = new ValidationContext(entry);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(entry, context, results, true);

        // Assert
        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    [InlineData(-1)]
    public void MoodEntry_ShouldRejectInvalidMoodLevels(int moodLevel)
    {
        // Arrange
        var entry = new MoodEntry { MoodLevel = moodLevel };
        var context = new ValidationContext(entry);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(entry, context, results, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(MoodEntry.MoodLevel)));
    }

    [Fact]
    public void MoodEntry_ShouldRejectTooLongNotes()
    {
        // Arrange
        var entry = new MoodEntry { Notes = new string('a', 1001) };
        var context = new ValidationContext(entry);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(entry, context, results, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(MoodEntry.Notes)));
    }

    [Fact]
    public void MoodEntry_ShouldRejectTooLongSymptoms()
    {
        // Arrange
        var entry = new MoodEntry { Symptoms = new string('a', 501) };
        var context = new ValidationContext(entry);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(entry, context, results, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(MoodEntry.Symptoms)));
    }
}
