using Moq;
using MoodLog.Application.Services;
using MoodLog.Core.DTOs;
using MoodLog.Core.Entities;
using MoodLog.Core.Interfaces;

namespace MoodLog.Application.Tests;

public class MoodEntryServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMoodEntryRepository> _mockMoodEntryRepo;
    private readonly MoodEntryService _service;

    public MoodEntryServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMoodEntryRepo = new Mock<IMoodEntryRepository>();
        _mockUnitOfWork.Setup(u => u.MoodEntries).Returns(_mockMoodEntryRepo.Object);
        _service = new MoodEntryService(_mockUnitOfWork.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WithValidUserAndEntry_ReturnsEntry()
    {
        // Arrange
        var userId = 1;
        var entryId = 1;
        var entry = new MoodEntry
        {
            Id = entryId,
            UserId = userId,
            MoodLevel = 7,
            Notes = "Feeling good",
            EntryDate = DateTime.Today,
            MoodEntryTags = new List<MoodEntryTag>()
        };

        _mockMoodEntryRepo.Setup(r => r.GetByIdAsync(entryId))
            .ReturnsAsync(entry);

        // Act
        var result = await _service.GetByIdAsync(entryId, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(entryId, result.Id);
        Assert.Equal(userId, entry.UserId);
        Assert.Equal(7, result.MoodLevel);
        Assert.Equal("Feeling good", result.Notes);
    }

    [Fact]
    public async Task GetByIdAsync_WithWrongUser_ReturnsNull()
    {
        // Arrange
        var userId = 1;
        var wrongUserId = 2;
        var entryId = 1;
        var entry = new MoodEntry
        {
            Id = entryId,
            UserId = wrongUserId,
            MoodLevel = 7,
            MoodEntryTags = new List<MoodEntryTag>()
        };

        _mockMoodEntryRepo.Setup(r => r.GetByIdAsync(entryId))
            .ReturnsAsync(entry);

        // Act
        var result = await _service.GetByIdAsync(entryId, userId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_WithValidData_CreatesEntry()
    {
        // Arrange
        var userId = 1;
        var dto = new MoodEntryCreateDto
        {
            MoodLevel = 8,
            Notes = "Great day!",
            Symptoms = "Energetic",
            EntryDate = DateTime.Today,
            TagIds = new List<int> { 1, 2 }
        };

        _mockMoodEntryRepo.Setup(r => r.GetByUserIdAndDateAsync(userId, dto.EntryDate))
            .ReturnsAsync((MoodEntry?)null);

        _mockMoodEntryRepo.Setup(r => r.AddAsync(It.IsAny<MoodEntry>()))
            .ReturnsAsync((MoodEntry entry) => { entry.Id = 1; return entry; });

        _mockMoodEntryRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new MoodEntry
            {
                Id = 1,
                UserId = userId,
                MoodLevel = 8,
                Notes = "Great day!",
                Symptoms = "Energetic",
                EntryDate = dto.EntryDate,
                MoodEntryTags = new List<MoodEntryTag>()
            });

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Mock the Repository<MoodEntryTag> for tag operations
        var mockMoodEntryTagRepo = new Mock<IRepository<MoodEntryTag>>();
        _mockUnitOfWork.Setup(u => u.Repository<MoodEntryTag>()).Returns(mockMoodEntryTagRepo.Object);

        // Act
        var result = await _service.CreateAsync(dto, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(8, result.MoodLevel);
        Assert.Equal("Great day!", result.Notes);
        Assert.Equal("Energetic", result.Symptoms);
        Assert.Equal(dto.EntryDate, result.EntryDate);
    }

    [Fact]
    public async Task CreateAsync_WithExistingEntryForDate_ThrowsException()
    {
        // Arrange
        var userId = 1;
        var dto = new MoodEntryCreateDto
        {
            MoodLevel = 8,
            EntryDate = DateTime.Today
        };

        var existingEntry = new MoodEntry { Id = 1, UserId = userId, EntryDate = dto.EntryDate };
        _mockMoodEntryRepo.Setup(r => r.GetByUserIdAndDateAsync(userId, dto.EntryDate))
            .ReturnsAsync(existingEntry);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(dto, userId));

        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task GetAverageMoodAsync_WithEntries_ReturnsCorrectAverage()
    {
        // Arrange
        var userId = 1;
        var expectedAverage = 7.5;

        _mockMoodEntryRepo.Setup(r => r.GetAverageMoodForUserAsync(userId, null, null))
            .ReturnsAsync(expectedAverage);

        // Act
        var result = await _service.GetAverageMoodAsync(userId);

        // Assert
        Assert.Equal(expectedAverage, result);
    }

    [Fact]
    public async Task CanUserAccessEntryAsync_WithValidUser_ReturnsTrue()
    {
        // Arrange
        var userId = 1;
        var entryId = 1;
        var entry = new MoodEntry { Id = entryId, UserId = userId };

        _mockMoodEntryRepo.Setup(r => r.GetByIdAsync(entryId))
            .ReturnsAsync(entry);

        // Act
        var result = await _service.CanUserAccessEntryAsync(entryId, userId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanUserAccessEntryAsync_WithWrongUser_ReturnsFalse()
    {
        // Arrange
        var userId = 1;
        var wrongUserId = 2;
        var entryId = 1;
        var entry = new MoodEntry { Id = entryId, UserId = wrongUserId };

        _mockMoodEntryRepo.Setup(r => r.GetByIdAsync(entryId))
            .ReturnsAsync(entry);

        // Act
        var result = await _service.CanUserAccessEntryAsync(entryId, userId);

        // Assert
        Assert.False(result);
    }
}
