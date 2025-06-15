using FluentAssertions;
using Moq;
using MoodLog.Application.Services;
using MoodLog.Core.DTOs;
using MoodLog.Core.Entities;
using MoodLog.Core.Interfaces;

namespace MoodLog.Application.Tests.Services;

public class MoodTagServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMoodTagRepository> _mockMoodTagRepository;
    private readonly MoodTagService _service;

    public MoodTagServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMoodTagRepository = new Mock<IMoodTagRepository>();

        _mockUnitOfWork.Setup(x => x.MoodTags).Returns(_mockMoodTagRepository.Object);

        _service = new MoodTagService(_mockUnitOfWork.Object);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ShouldCreateMoodTag()
    {
        // Arrange
        var createDto = new MoodTagCreateDto
        {
            Name = "Happy",
            Description = "Feeling happy and joyful",
            Color = "#00FF00"
        };

        _mockMoodTagRepository
            .Setup(x => x.GetByNameAsync(createDto.Name))
            .ReturnsAsync((MoodTag?)null);

        _mockMoodTagRepository
            .Setup(x => x.AddAsync(It.IsAny<MoodTag>()))
            .ReturnsAsync((MoodTag tag) => tag);

        // Act
        var result = await _service.CreateAsync(createDto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Happy");
        result.Description.Should().Be("Feeling happy and joyful");
        result.Color.Should().Be("#00FF00");
        result.IsActive.Should().BeTrue(); // Service sets this to true by default

        _mockMoodTagRepository.Verify(x => x.AddAsync(It.IsAny<MoodTag>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var createDto = new MoodTagCreateDto
        {
            Name = "Happy",
            Description = "Test",
            Color = "#00FF00"
        };

        var existingTag = new MoodTag
        {
            Id = 1,
            Name = "Happy",
            Description = "Existing tag",
            Color = "#FF0000",
            IsActive = true
        };

        _mockMoodTagRepository
            .Setup(x => x.GetByNameAsync(createDto.Name))
            .ReturnsAsync(existingTag);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(createDto));

        exception.Message.Should().Contain("already exists");
        _mockMoodTagRepository.Verify(x => x.AddAsync(It.IsAny<MoodTag>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_InvalidName_ShouldThrowArgumentException(string invalidName)
    {
        // Arrange
        var createDto = new MoodTagCreateDto
        {
            Name = invalidName,
            Description = "Test",
            Color = "#00FF00"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateAsync(createDto));

        exception.Message.Should().Contain("Tag name cannot be empty");
    }

    [Fact]
    public async Task CreateAsync_NullName_ShouldThrowArgumentException()
    {
        // Arrange
        var createDto = new MoodTagCreateDto
        {
            Name = null!,
            Description = "Test",
            Color = "#00FF00"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateAsync(createDto));

        exception.Message.Should().Contain("Tag name cannot be empty");
    }

    [Theory]
    [InlineData("#GGGGGG")]
    [InlineData("FF0000")]
    [InlineData("#FF00")]
    [InlineData("invalid")]
    public async Task CreateAsync_InvalidColor_ShouldThrowArgumentException(string invalidColor)
    {
        // Arrange
        var createDto = new MoodTagCreateDto
        {
            Name = "Test",
            Description = "Test",
            Color = invalidColor
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateAsync(createDto));

        exception.Message.Should().Contain("Invalid color format");
    }

    [Fact]
    public async Task UpdateAsync_ValidDto_ShouldUpdateMoodTag()
    {
        // Arrange
        var tagId = 1;
        var updateDto = new MoodTagUpdateDto
        {
            Id = tagId,
            Name = "Updated Happy",
            Description = "Updated description",
            Color = "#0000FF",
            IsActive = false
        };

        var existingTag = new MoodTag
        {
            Id = tagId,
            Name = "Happy",
            Description = "Old description",
            Color = "#00FF00",
            IsActive = true
        };

        _mockMoodTagRepository
            .Setup(x => x.GetByIdAsync(tagId))
            .ReturnsAsync(existingTag);

        _mockMoodTagRepository
            .Setup(x => x.GetByNameAsync(updateDto.Name))
            .ReturnsAsync((MoodTag?)null);

        // Act
        var result = await _service.UpdateAsync(updateDto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Updated Happy");
        result.Description.Should().Be("Updated description");
        result.Color.Should().Be("#0000FF");
        result.IsActive.Should().BeFalse();

        existingTag.Name.Should().Be("Updated Happy");
        existingTag.Description.Should().Be("Updated description");
        existingTag.Color.Should().Be("#0000FF");
        existingTag.IsActive.Should().BeFalse();

        _mockMoodTagRepository.Verify(x => x.UpdateAsync(existingTag), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_TagNotFound_ShouldThrowArgumentException()
    {
        // Arrange
        var updateDto = new MoodTagUpdateDto
        {
            Id = 999,
            Name = "Test",
            Description = "Test",
            Color = "#00FF00",
            IsActive = true
        };

        _mockMoodTagRepository
            .Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((MoodTag?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdateAsync(updateDto));

        exception.Message.Should().Contain("Tag not found");
    }

    [Fact]
    public async Task UpdateAsync_DuplicateName_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var tagId = 1;
        var updateDto = new MoodTagUpdateDto
        {
            Id = tagId,
            Name = "Existing Tag",
            Description = "Test",
            Color = "#00FF00",
            IsActive = true
        };

        var existingTag = new MoodTag
        {
            Id = tagId,
            Name = "Happy",
            Description = "Test",
            Color = "#00FF00",
            IsActive = true
        };

        var duplicateTag = new MoodTag
        {
            Id = 2,
            Name = "Existing Tag",
            Description = "Another tag",
            Color = "#FF0000",
            IsActive = true
        };

        _mockMoodTagRepository
            .Setup(x => x.GetByIdAsync(tagId))
            .ReturnsAsync(existingTag);

        _mockMoodTagRepository
            .Setup(x => x.GetByNameAsync(updateDto.Name))
            .ReturnsAsync(duplicateTag);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAsync(updateDto));

        exception.Message.Should().Contain("already exists");
    }

    [Fact]
    public async Task DeleteAsync_ValidId_ShouldDeleteAndReturnTrue()
    {
        // Arrange
        var tagId = 1;
        var existingTag = new MoodTag
        {
            Id = tagId,
            Name = "Happy",
            Description = "Test",
            Color = "#00FF00",
            IsActive = true
        };

        _mockMoodTagRepository
            .Setup(x => x.GetByIdAsync(tagId))
            .ReturnsAsync(existingTag);

        // Act
        var result = await _service.DeleteAsync(tagId);

        // Assert
        result.Should().BeTrue();
        _mockMoodTagRepository.Verify(x => x.DeleteAsync(existingTag), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_TagNotFound_ShouldReturnFalse()
    {
        // Arrange
        var tagId = 999;

        _mockMoodTagRepository
            .Setup(x => x.GetByIdAsync(tagId))
            .ReturnsAsync((MoodTag?)null);

        // Act
        var result = await _service.DeleteAsync(tagId);

        // Assert
        result.Should().BeFalse();
        _mockMoodTagRepository.Verify(x => x.DeleteAsync(It.IsAny<MoodTag>()), Times.Never);
    }

    [Fact]
    public async Task GetAllActiveAsync_ShouldReturnActiveTags()
    {
        // Arrange
        var activeTags = new List<MoodTag>
        {
            new() { Id = 1, Name = "Happy", IsActive = true },
            new() { Id = 2, Name = "Sad", IsActive = true }
        };

        _mockMoodTagRepository
            .Setup(x => x.GetActiveTagsAsync())
            .ReturnsAsync(activeTags);

        // Act
        var result = await _service.GetAllActiveAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(tag => tag.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_ValidId_ShouldReturnTag()
    {
        // Arrange
        var tagId = 1;
        var tag = new MoodTag
        {
            Id = tagId,
            Name = "Happy",
            Description = "Test",
            Color = "#00FF00",
            IsActive = true
        };

        _mockMoodTagRepository
            .Setup(x => x.GetByIdAsync(tagId))
            .ReturnsAsync(tag);

        // Act
        var result = await _service.GetByIdAsync(tagId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(tagId);
        result.Name.Should().Be("Happy");
    }

    [Fact]
    public async Task GetByIdAsync_InvalidId_ShouldReturnNull()
    {
        // Arrange
        var tagId = 999;

        _mockMoodTagRepository
            .Setup(x => x.GetByIdAsync(tagId))
            .ReturnsAsync((MoodTag?)null);

        // Act
        var result = await _service.GetByIdAsync(tagId);

        // Assert
        result.Should().BeNull();
    }
}
