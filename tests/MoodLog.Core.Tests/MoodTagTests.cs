using FluentAssertions;
using MoodLog.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace MoodLog.Core.Tests;

public class MoodTagTests
{
    [Fact]
    public void MoodTag_ShouldHaveValidDefaults()
    {
        // Arrange & Act
        var tag = new MoodTag();

        // Assert
        Assert.Equal("#007bff", tag.Color);
        Assert.False(tag.IsSystemTag);
        Assert.True(tag.IsActive);
        Assert.True(tag.CreatedAt <= DateTime.UtcNow);
        Assert.Equal(string.Empty, tag.Name);
        Assert.Equal(string.Empty, tag.Description);
        Assert.NotNull(tag.MoodEntryTags);
        Assert.Empty(tag.MoodEntryTags);
    }

    [Theory]
    [InlineData("Happy")]
    [InlineData("Sad")]
    [InlineData("A")]
    [InlineData("This is a valid tag name with 50 characters!!")]
    public void MoodTag_ShouldAcceptValidNames(string name)
    {
        // Arrange
        var tag = new MoodTag { Name = name };
        var context = new ValidationContext(tag);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(tag, context, results, true);

        // Assert
        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Fact]
    public void MoodTag_ShouldRejectEmptyName()
    {
        // Arrange
        var tag = new MoodTag { Name = "" };
        var context = new ValidationContext(tag);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(tag, context, results, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(MoodTag.Name)));
    }

    [Fact]
    public void MoodTag_ShouldRejectTooLongName()
    {
        // Arrange
        var tag = new MoodTag { Name = new string('a', 51) };
        var context = new ValidationContext(tag);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(tag, context, results, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(MoodTag.Name)));
    }

    [Fact]
    public void MoodTag_ShouldRejectTooLongDescription()
    {
        // Arrange
        var tag = new MoodTag { Name = "Valid", Description = new string('a', 201) };
        var context = new ValidationContext(tag);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(tag, context, results, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(MoodTag.Description)));
    }

    [Theory]
    [InlineData("#FF5733")]
    [InlineData("#000000")]
    [InlineData("#FFFFFF")]
    [InlineData("#123abc")]
    public void MoodTag_ShouldAcceptValidColors(string color)
    {
        // Arrange
        var tag = new MoodTag { Name = "Test", Color = color };
        var context = new ValidationContext(tag);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(tag, context, results, true);

        // Assert
        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Theory]
    [InlineData("FF5733")]  // Missing #
    [InlineData("#FF573")]  // Too short
    [InlineData("#FF57333")] // Too long
    [InlineData("#GG5733")]  // Invalid characters
    [InlineData("red")]      // Not hex
    public void MoodTag_ShouldRejectInvalidColors(string color)
    {
        // Arrange
        var tag = new MoodTag { Name = "Test", Color = color };
        var context = new ValidationContext(tag);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(tag, context, results, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(MoodTag.Color)));
    }
}
