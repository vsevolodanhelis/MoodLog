using FluentAssertions;
using Moq;
using MoodLog.Application.Services;
using MoodLog.Core.DTOs;
using MoodLog.Core.Entities;
using MoodLog.Core.Interfaces;

namespace MoodLog.Application.Tests.Services;

public class MoodEntryServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMoodEntryRepository> _mockMoodEntryRepository;
    private readonly Mock<IRepository<MoodEntryTag>> _mockMoodEntryTagRepository;
    private readonly MoodEntryService _service;

    public MoodEntryServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMoodEntryRepository = new Mock<IMoodEntryRepository>();
        _mockMoodEntryTagRepository = new Mock<IRepository<MoodEntryTag>>();

        _mockUnitOfWork.Setup(x => x.MoodEntries).Returns(_mockMoodEntryRepository.Object);
        _mockUnitOfWork.Setup(x => x.Repository<MoodEntryTag>()).Returns(_mockMoodEntryTagRepository.Object);

        _service = new MoodEntryService(_mockUnitOfWork.Object);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ShouldCreateMoodEntry()
    {
        // Arrange
        var userId = 1;
        var createDto = new MoodEntryCreateDto
        {
            MoodLevel = 7,
            Notes = "Feeling good today",
            Symptoms = "None",
            EntryDate = DateTime.Today,
            TagIds = new List<int> { 1, 2 }
        };

        _mockMoodEntryRepository
            .Setup(x => x.GetByUserIdAndDateAsync(userId, createDto.EntryDate))
            .ReturnsAsync((MoodEntry?)null);

        _mockMoodEntryRepository
            .Setup(x => x.AddAsync(It.IsAny<MoodEntry>()))
            .ReturnsAsync((MoodEntry entry) =>
            {
                entry.Id = 1; // Set an ID for the created entry
                entry.MoodEntryTags = new List<MoodEntryTag>(); // Initialize the collection
                return entry;
            });

        _mockMoodEntryRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((int id) => new MoodEntry
            {
                Id = id,
                UserId = userId,
                MoodLevel = 7,
                Notes = "Feeling good today",
                Symptoms = "None",
                EntryDate = DateTime.Today,
                MoodEntryTags = new List<MoodEntryTag>()
            });

        // Act
        var result = await _service.CreateAsync(createDto, userId);

        // Assert
        result.Should().NotBeNull();
        result.MoodLevel.Should().Be(7);
        result.Notes.Should().Be("Feeling good today");
        result.Symptoms.Should().Be("None");

        _mockMoodEntryRepository.Verify(x => x.AddAsync(It.IsAny<MoodEntry>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(), Times.AtLeast(1)); // Called multiple times for entry and tags
    }

    [Fact]
    public async Task CreateAsync_DuplicateEntry_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var userId = 1;
        var createDto = new MoodEntryCreateDto
        {
            MoodLevel = 7,
            Notes = "Test",
            EntryDate = DateTime.Today,
            TagIds = new List<int>()
        };

        var existingEntry = new MoodEntry
        {
            Id = 1,
            UserId = userId,
            MoodLevel = 5,
            EntryDate = DateTime.Today
        };

        _mockMoodEntryRepository
            .Setup(x => x.GetByUserIdAndDateAsync(userId, createDto.EntryDate))
            .ReturnsAsync(existingEntry);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(createDto, userId));

        exception.Message.Should().Contain("already exists");
        _mockMoodEntryRepository.Verify(x => x.AddAsync(It.IsAny<MoodEntry>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    [InlineData(-1)]
    public async Task CreateAsync_InvalidMoodLevel_ShouldThrowArgumentException(int invalidMoodLevel)
    {
        // Arrange
        var userId = 1;
        var createDto = new MoodEntryCreateDto
        {
            MoodLevel = invalidMoodLevel,
            Notes = "Test",
            EntryDate = DateTime.Today,
            TagIds = new List<int>()
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateAsync(createDto, userId));

        exception.Message.Should().Contain("Mood level must be between 1 and 10");
    }

    [Fact]
    public async Task UpdateAsync_ValidDto_ShouldUpdateMoodEntry()
    {
        // Arrange
        var userId = 1;
        var entryId = 1;
        var updateDto = new MoodEntryUpdateDto
        {
            Id = entryId,
            MoodLevel = 8,
            Notes = "Updated notes",
            Symptoms = "Updated symptoms",
            TagIds = new List<int> { 2, 3 }
        };

        var existingEntry = new MoodEntry
        {
            Id = entryId,
            UserId = userId,
            MoodLevel = 5,
            Notes = "Old notes",
            Symptoms = "Old symptoms",
            EntryDate = DateTime.Today,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        _mockMoodEntryRepository
            .Setup(x => x.GetByIdAsync(entryId))
            .ReturnsAsync(existingEntry);

        // Act
        var result = await _service.UpdateAsync(updateDto, userId);

        // Assert
        result.Should().NotBeNull();
        result.MoodLevel.Should().Be(8);
        result.Notes.Should().Be("Updated notes");
        result.Symptoms.Should().Be("Updated symptoms");

        existingEntry.MoodLevel.Should().Be(8);
        existingEntry.Notes.Should().Be("Updated notes");
        existingEntry.Symptoms.Should().Be("Updated symptoms");
        existingEntry.UpdatedAt.Should().NotBeNull();

        _mockMoodEntryRepository.Verify(x => x.UpdateAsync(existingEntry), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_EntryNotFound_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var userId = 1;
        var updateDto = new MoodEntryUpdateDto
        {
            Id = 999,
            MoodLevel = 8,
            Notes = "Test",
            Symptoms = "Test",
            TagIds = new List<int>()
        };

        _mockMoodEntryRepository
            .Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((MoodEntry?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.UpdateAsync(updateDto, userId));

        exception.Message.Should().Contain("Entry not found or access denied");
    }

    [Fact]
    public async Task UpdateAsync_WrongUser_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var userId = 1;
        var wrongUserId = 2;
        var entryId = 1;
        var updateDto = new MoodEntryUpdateDto
        {
            Id = entryId,
            MoodLevel = 8,
            Notes = "Test",
            Symptoms = "Test",
            TagIds = new List<int>()
        };

        var existingEntry = new MoodEntry
        {
            Id = entryId,
            UserId = wrongUserId, // Different user
            MoodLevel = 5,
            EntryDate = DateTime.Today
        };

        _mockMoodEntryRepository
            .Setup(x => x.GetByIdAsync(entryId))
            .ReturnsAsync(existingEntry);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.UpdateAsync(updateDto, userId));

        exception.Message.Should().Contain("Entry not found or access denied");
    }

    [Fact]
    public async Task DeleteAsync_ValidEntry_ShouldDeleteAndReturnTrue()
    {
        // Arrange
        var userId = 1;
        var entryId = 1;
        var existingEntry = new MoodEntry
        {
            Id = entryId,
            UserId = userId,
            MoodLevel = 5,
            EntryDate = DateTime.Today
        };

        _mockMoodEntryRepository
            .Setup(x => x.GetByIdAsync(entryId))
            .ReturnsAsync(existingEntry);

        // Act
        var result = await _service.DeleteAsync(entryId, userId);

        // Assert
        result.Should().BeTrue();
        _mockMoodEntryRepository.Verify(x => x.DeleteAsync(existingEntry), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_EntryNotFound_ShouldReturnFalse()
    {
        // Arrange
        var userId = 1;
        var entryId = 999;

        _mockMoodEntryRepository
            .Setup(x => x.GetByIdAsync(entryId))
            .ReturnsAsync((MoodEntry?)null);

        // Act
        var result = await _service.DeleteAsync(entryId, userId);

        // Assert
        result.Should().BeFalse();
        _mockMoodEntryRepository.Verify(x => x.DeleteAsync(It.IsAny<MoodEntry>()), Times.Never);
    }

    [Fact]
    public async Task GetByDateAsync_ValidDate_ShouldReturnMoodEntry()
    {
        // Arrange
        var userId = 1;
        var date = DateTime.Today;
        var moodEntry = new MoodEntry
        {
            Id = 1,
            UserId = userId,
            MoodLevel = 7,
            Notes = "Test notes",
            EntryDate = date,
            MoodEntryTags = new List<MoodEntryTag>()
        };

        _mockMoodEntryRepository
            .Setup(x => x.GetByUserIdAndDateAsync(userId, date))
            .ReturnsAsync(moodEntry);

        // Act
        var result = await _service.GetByDateAsync(userId, date);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.MoodLevel.Should().Be(7);
        result.Notes.Should().Be("Test notes");
    }

    [Fact]
    public async Task GetByDateAsync_NoEntry_ShouldReturnNull()
    {
        // Arrange
        var userId = 1;
        var date = DateTime.Today;

        _mockMoodEntryRepository
            .Setup(x => x.GetByUserIdAndDateAsync(userId, date))
            .ReturnsAsync((MoodEntry?)null);

        // Act
        var result = await _service.GetByDateAsync(userId, date);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAverageMoodAsync_ValidData_ShouldReturnCorrectAverage()
    {
        // Arrange
        var userId = 1;
        var startDate = DateTime.Today.AddDays(-7);
        var endDate = DateTime.Today;
        var expectedAverage = 6.5;

        _mockMoodEntryRepository
            .Setup(x => x.GetAverageMoodForUserAsync(userId, startDate, endDate))
            .ReturnsAsync(expectedAverage);

        // Act
        var result = await _service.GetAverageMoodAsync(userId, startDate, endDate);

        // Assert
        result.Should().Be(expectedAverage);
    }
}
