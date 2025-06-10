using MoodLog.Core.DTOs;
using MoodLog.Core.Events;

namespace MoodLog.Core.Tests;

public class MoodEntryEventsTests
{
    [Fact]
    public void MoodEntryEventPublisher_ShouldTriggerCreatedEvent()
    {
        // Arrange
        var publisher = new MoodEntryEventPublisher();
        var eventTriggered = false;
        MoodEntryEventArgs? capturedArgs = null;

        publisher.MoodEntryCreated += (sender, args) =>
        {
            eventTriggered = true;
            capturedArgs = args;
        };

        var moodEntry = new MoodEntryDto
        {
            Id = 1,
            MoodLevel = 7,
            Notes = "Feeling good",
            EntryDate = DateTime.Today
        };

        // Act
        publisher.OnMoodEntryCreated(moodEntry, 123);

        // Assert
        Assert.True(eventTriggered);
        Assert.NotNull(capturedArgs);
        Assert.Equal(1, capturedArgs.MoodEntry.Id);
        Assert.Equal(123, capturedArgs.UserId);
        Assert.Equal(7, capturedArgs.MoodEntry.MoodLevel);
    }

    [Fact]
    public void MoodEntryEventPublisher_ShouldTriggerUpdatedEvent()
    {
        // Arrange
        var publisher = new MoodEntryEventPublisher();
        var eventTriggered = false;

        publisher.MoodEntryUpdated += (sender, args) => eventTriggered = true;

        var moodEntry = new MoodEntryDto
        {
            Id = 1,
            MoodLevel = 8,
            Notes = "Updated mood",
            EntryDate = DateTime.Today
        };

        // Act
        publisher.OnMoodEntryUpdated(moodEntry, 123);

        // Assert
        Assert.True(eventTriggered);
    }

    [Fact]
    public void MoodEntryEventPublisher_ShouldTriggerDeletedEvent()
    {
        // Arrange
        var publisher = new MoodEntryEventPublisher();
        var eventTriggered = false;
        MoodEntryDeletedEventArgs? capturedArgs = null;

        publisher.MoodEntryDeleted += (sender, args) =>
        {
            eventTriggered = true;
            capturedArgs = args;
        };

        // Act
        publisher.OnMoodEntryDeleted(1, 123);

        // Assert
        Assert.True(eventTriggered);
        Assert.NotNull(capturedArgs);
        Assert.Equal(1, capturedArgs.MoodEntryId);
        Assert.Equal(123, capturedArgs.UserId);
    }

    [Fact]
    public async Task MoodEntryEventPublisher_ShouldTriggerAsyncEvent()
    {
        // Arrange
        var publisher = new MoodEntryEventPublisher();
        var eventTriggered = false;

        publisher.MoodEntryCreatedAsync += async (sender, args) =>
        {
            await Task.Delay(10); // Simulate async work
            eventTriggered = true;
        };

        var moodEntry = new MoodEntryDto
        {
            Id = 1,
            MoodLevel = 7,
            Notes = "Async test",
            EntryDate = DateTime.Today
        };

        // Act
        await publisher.OnMoodEntryCreatedAsync(moodEntry, 123);

        // Assert
        Assert.True(eventTriggered);
    }

    [Fact]
    public void MoodEntryAuditHandler_ShouldLogCreatedEvent()
    {
        // Arrange
        var handler = new MoodEntryAuditHandler();
        var moodEntry = new MoodEntryDto
        {
            Id = 1,
            MoodLevel = 7,
            Notes = "Test entry",
            EntryDate = DateTime.Today
        };
        var args = new MoodEntryEventArgs(moodEntry, 123);

        // Act
        handler.HandleMoodEntryCreated(this, args);

        // Assert
        var auditLog = handler.GetAuditLog();
        Assert.Single(auditLog);
        Assert.Contains("User 123 created mood entry 1", auditLog[0]);
        Assert.Contains("level 7", auditLog[0]);
    }

    [Fact]
    public void MoodEntryAuditHandler_ShouldLogUpdatedEvent()
    {
        // Arrange
        var handler = new MoodEntryAuditHandler();
        var moodEntry = new MoodEntryDto
        {
            Id = 2,
            MoodLevel = 8,
            Notes = "Updated entry",
            EntryDate = DateTime.Today
        };
        var args = new MoodEntryEventArgs(moodEntry, 456);

        // Act
        handler.HandleMoodEntryUpdated(this, args);

        // Assert
        var auditLog = handler.GetAuditLog();
        Assert.Single(auditLog);
        Assert.Contains("User 456 updated mood entry 2", auditLog[0]);
        Assert.Contains("level 8", auditLog[0]);
    }

    [Fact]
    public void MoodEntryAuditHandler_ShouldLogDeletedEvent()
    {
        // Arrange
        var handler = new MoodEntryAuditHandler();
        var args = new MoodEntryDeletedEventArgs(3, 789);

        // Act
        handler.HandleMoodEntryDeleted(this, args);

        // Assert
        var auditLog = handler.GetAuditLog();
        Assert.Single(auditLog);
        Assert.Contains("User 789 deleted mood entry 3", auditLog[0]);
    }

    [Fact]
    public async Task MoodEntryNotificationHandler_ShouldHandleLowMood()
    {
        // Arrange
        var handler = new MoodEntryNotificationHandler();
        var moodEntry = new MoodEntryDto
        {
            Id = 1,
            MoodLevel = 2, // Low mood
            Notes = "Feeling down",
            EntryDate = DateTime.Today
        };
        var args = new MoodEntryEventArgs(moodEntry, 123);

        // Act & Assert (should not throw)
        await handler.HandleMoodEntryCreatedAsync(this, args);
    }

    [Fact]
    public async Task MoodEntryNotificationHandler_ShouldHandleHighMood()
    {
        // Arrange
        var handler = new MoodEntryNotificationHandler();
        var moodEntry = new MoodEntryDto
        {
            Id = 1,
            MoodLevel = 9, // High mood
            Notes = "Feeling great!",
            EntryDate = DateTime.Today
        };
        var args = new MoodEntryEventArgs(moodEntry, 123);

        // Act & Assert (should not throw)
        await handler.HandleMoodEntryCreatedAsync(this, args);
    }

    [Fact]
    public void MultipleHandlers_ShouldAllBeTriggered()
    {
        // Arrange
        var publisher = new MoodEntryEventPublisher();
        var handler1Triggered = false;
        var handler2Triggered = false;

        publisher.MoodEntryCreated += (sender, args) => handler1Triggered = true;
        publisher.MoodEntryCreated += (sender, args) => handler2Triggered = true;

        var moodEntry = new MoodEntryDto
        {
            Id = 1,
            MoodLevel = 5,
            EntryDate = DateTime.Today
        };

        // Act
        publisher.OnMoodEntryCreated(moodEntry, 123);

        // Assert
        Assert.True(handler1Triggered);
        Assert.True(handler2Triggered);
    }
}
