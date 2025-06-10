using MoodLog.Core.DTOs;
using MoodLog.Core.Extensions;

namespace MoodLog.Core.Tests;

public class MoodEntryExtensionsTests
{
    [Theory]
    [InlineData(1, "Very Low")]
    [InlineData(2, "Very Low")]
    [InlineData(3, "Low")]
    [InlineData(4, "Low")]
    [InlineData(5, "Moderate")]
    [InlineData(6, "Moderate")]
    [InlineData(7, "Good")]
    [InlineData(8, "Good")]
    [InlineData(9, "Excellent")]
    [InlineData(10, "Excellent")]
    public void GetMoodCategory_ShouldReturnCorrectCategory(int moodLevel, string expectedCategory)
    {
        // Arrange
        var entry = new MoodEntryDto { MoodLevel = moodLevel };

        // Act
        var category = entry.GetMoodCategory();

        // Assert
        Assert.Equal(expectedCategory, category);
    }

    [Theory]
    [InlineData(1, "😢")]
    [InlineData(2, "😞")]
    [InlineData(3, "😐")]
    [InlineData(4, "🙂")]
    [InlineData(5, "😊")]
    [InlineData(6, "😄")]
    [InlineData(7, "😁")]
    [InlineData(8, "🤗")]
    [InlineData(9, "🥳")]
    [InlineData(10, "🌟")]
    [InlineData(11, "❓")]
    public void GetMoodEmoji_ShouldReturnCorrectEmoji(int moodLevel, string expectedEmoji)
    {
        // Arrange
        var entry = new MoodEntryDto { MoodLevel = moodLevel };

        // Act
        var emoji = entry.GetMoodEmoji();

        // Assert
        Assert.Equal(expectedEmoji, emoji);
    }

    [Fact]
    public void IsRecent_ShouldReturnTrueForRecentEntry()
    {
        // Arrange
        var entry = new MoodEntryDto { EntryDate = DateTime.Today.AddDays(-3) };

        // Act
        var isRecent = entry.IsRecent(7);

        // Assert
        Assert.True(isRecent);
    }

    [Fact]
    public void IsRecent_ShouldReturnFalseForOldEntry()
    {
        // Arrange
        var entry = new MoodEntryDto { EntryDate = DateTime.Today.AddDays(-10) };

        // Act
        var isRecent = entry.IsRecent(7);

        // Assert
        Assert.False(isRecent);
    }

    [Theory]
    [InlineData(6, true)]
    [InlineData(7, true)]
    [InlineData(10, true)]
    [InlineData(5, false)]
    [InlineData(1, false)]
    public void IsPositiveMood_ShouldReturnCorrectValue(int moodLevel, bool expected)
    {
        // Arrange
        var entry = new MoodEntryDto { MoodLevel = moodLevel };

        // Act
        var isPositive = entry.IsPositiveMood();

        // Assert
        Assert.Equal(expected, isPositive);
    }

    [Fact]
    public void ToDisplayString_ShouldFormatCorrectly()
    {
        // Arrange
        var entry = new MoodEntryDto
        {
            MoodLevel = 7,
            EntryDate = new DateTime(2024, 1, 15)
        };

        // Act
        var displayString = entry.ToDisplayString();

        // Assert
        Assert.Contains("2024-01-15", displayString);
        Assert.Contains("😁", displayString);
        Assert.Contains("(7/10)", displayString);
        Assert.Contains("Good", displayString);
    }
}

public class MoodEntryCollectionExtensionsTests
{
    [Fact]
    public void GetMoodTrend_ShouldReturnImprovingForUpwardTrend()
    {
        // Arrange
        var entries = new List<MoodEntryDto>
        {
            new() { MoodLevel = 3, EntryDate = DateTime.Today.AddDays(-6) },
            new() { MoodLevel = 4, EntryDate = DateTime.Today.AddDays(-5) },
            new() { MoodLevel = 5, EntryDate = DateTime.Today.AddDays(-4) },
            new() { MoodLevel = 7, EntryDate = DateTime.Today.AddDays(-3) },
            new() { MoodLevel = 8, EntryDate = DateTime.Today.AddDays(-2) },
            new() { MoodLevel = 9, EntryDate = DateTime.Today.AddDays(-1) }
        };

        // Act
        var trend = entries.GetMoodTrend();

        // Assert
        Assert.Contains("Improving", trend);
    }

    [Fact]
    public void GetMoodTrend_ShouldReturnDecliningForDownwardTrend()
    {
        // Arrange
        var entries = new List<MoodEntryDto>
        {
            new() { MoodLevel = 8, EntryDate = DateTime.Today.AddDays(-6) },
            new() { MoodLevel = 7, EntryDate = DateTime.Today.AddDays(-5) },
            new() { MoodLevel = 6, EntryDate = DateTime.Today.AddDays(-4) },
            new() { MoodLevel = 4, EntryDate = DateTime.Today.AddDays(-3) },
            new() { MoodLevel = 3, EntryDate = DateTime.Today.AddDays(-2) },
            new() { MoodLevel = 2, EntryDate = DateTime.Today.AddDays(-1) }
        };

        // Act
        var trend = entries.GetMoodTrend();

        // Assert
        Assert.Contains("Declining", trend);
    }

    [Fact]
    public void GetStatistics_ShouldCalculateCorrectly()
    {
        // Arrange
        var entries = new List<MoodEntryDto>
        {
            new() { MoodLevel = 5 },
            new() { MoodLevel = 7 },
            new() { MoodLevel = 3 },
            new() { MoodLevel = 8 },
            new() { MoodLevel = 6 }
        };

        // Act
        var stats = entries.GetStatistics();

        // Assert
        Assert.Equal(5, stats.Count);
        Assert.Equal(5.8, stats.Average);
        Assert.Equal(3, stats.Minimum);
        Assert.Equal(8, stats.Maximum);
        Assert.True(stats.StandardDeviation > 0);
        Assert.Equal(60, stats.PositiveMoodPercentage); // 3 out of 5 entries >= 6
    }

    [Fact]
    public void FilterByMoodRange_ShouldFilterCorrectly()
    {
        // Arrange
        var entries = new List<MoodEntryDto>
        {
            new() { MoodLevel = 2 },
            new() { MoodLevel = 5 },
            new() { MoodLevel = 7 },
            new() { MoodLevel = 9 },
            new() { MoodLevel = 3 }
        };

        // Act
        var filtered = entries.FilterByMoodRange(4, 8).ToList();

        // Assert
        Assert.Equal(2, filtered.Count);
        Assert.Contains(filtered, e => e.MoodLevel == 5);
        Assert.Contains(filtered, e => e.MoodLevel == 7);
    }

    [Fact]
    public void GetDayOfWeekPattern_ShouldGroupCorrectly()
    {
        // Arrange
        var entries = new List<MoodEntryDto>
        {
            new() { MoodLevel = 5, EntryDate = new DateTime(2024, 1, 1) }, // Monday
            new() { MoodLevel = 7, EntryDate = new DateTime(2024, 1, 2) }, // Tuesday
            new() { MoodLevel = 6, EntryDate = new DateTime(2024, 1, 8) }, // Monday
            new() { MoodLevel = 8, EntryDate = new DateTime(2024, 1, 9) }  // Tuesday
        };

        // Act
        var pattern = entries.GetDayOfWeekPattern();

        // Assert
        Assert.Equal(2, pattern.Count);
        Assert.Equal(5.5, pattern[DayOfWeek.Monday]); // (5 + 6) / 2
        Assert.Equal(7.5, pattern[DayOfWeek.Tuesday]); // (7 + 8) / 2
    }

    [Fact]
    public void CalculateCurrentStreak_ShouldCalculateCorrectly()
    {
        // Arrange
        var entries = new List<MoodEntryDto>
        {
            new() { EntryDate = DateTime.Today },
            new() { EntryDate = DateTime.Today.AddDays(-1) },
            new() { EntryDate = DateTime.Today.AddDays(-2) },
            new() { EntryDate = DateTime.Today.AddDays(-4) } // Gap here
        };

        // Act
        var streak = entries.CalculateCurrentStreak();

        // Assert
        Assert.Equal(3, streak);
    }
}

public class FunctionalHelpersTests
{
    [Fact]
    public void CreateMoodFilter_ShouldCreateCorrectFilter()
    {
        // Arrange
        var highMoodFilter = FunctionalHelpers.CreateMoodFilter(7, true);
        var lowMoodFilter = FunctionalHelpers.CreateMoodFilter(4, false);

        var entry1 = new MoodEntryDto { MoodLevel = 8 };
        var entry2 = new MoodEntryDto { MoodLevel = 3 };

        // Act & Assert
        Assert.True(highMoodFilter(entry1));
        Assert.False(highMoodFilter(entry2));
        Assert.False(lowMoodFilter(entry1));
        Assert.True(lowMoodFilter(entry2));
    }

    [Fact]
    public void ComposeFilters_ShouldCombineFilters()
    {
        // Arrange
        var recentFilter = FunctionalHelpers.CreateMoodFilter(5, true);
        var positiveFilter = FunctionalHelpers.CreateMoodFilter(6, true);
        var composedFilter = FunctionalHelpers.ComposeFilters(recentFilter, positiveFilter);

        var entry1 = new MoodEntryDto { MoodLevel = 7 }; // Passes both
        var entry2 = new MoodEntryDto { MoodLevel = 4 }; // Fails both
        var entry3 = new MoodEntryDto { MoodLevel = 5 }; // Passes first only

        // Act & Assert
        Assert.True(composedFilter(entry1));
        Assert.False(composedFilter(entry2));
        Assert.False(composedFilter(entry3));
    }

    [Fact]
    public void CreateAverageProcessor_ShouldCalculateAverage()
    {
        // Arrange
        var processor = FunctionalHelpers.CreateAverageProcessor();
        var entries = new List<MoodEntryDto>
        {
            new() { MoodLevel = 5 },
            new() { MoodLevel = 7 },
            new() { MoodLevel = 3 }
        };

        // Act
        var average = processor(entries);

        // Assert
        Assert.Equal(5.0, average);
    }

    [Fact]
    public void CreateTrendProcessor_ShouldAnalyzeTrend()
    {
        // Arrange
        var processor = FunctionalHelpers.CreateTrendProcessor();
        var entries = new List<MoodEntryDto>
        {
            new() { MoodLevel = 3, EntryDate = DateTime.Today.AddDays(-2) },
            new() { MoodLevel = 7, EntryDate = DateTime.Today.AddDays(-1) }
        };

        // Act
        var trend = processor(entries);

        // Assert
        Assert.Contains("Improving", trend);
    }
}
