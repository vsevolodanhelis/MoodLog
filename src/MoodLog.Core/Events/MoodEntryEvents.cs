using MoodLog.Core.DTOs;

namespace MoodLog.Core.Events;

// Event argument classes
public class MoodEntryEventArgs : EventArgs
{
    public MoodEntryDto MoodEntry { get; }
    public int UserId { get; }
    public DateTime Timestamp { get; }

    public MoodEntryEventArgs(MoodEntryDto moodEntry, int userId)
    {
        MoodEntry = moodEntry;
        UserId = userId;
        Timestamp = DateTime.UtcNow;
    }
}

public class MoodEntryDeletedEventArgs : EventArgs
{
    public int MoodEntryId { get; }
    public int UserId { get; }
    public DateTime Timestamp { get; }

    public MoodEntryDeletedEventArgs(int moodEntryId, int userId)
    {
        MoodEntryId = moodEntryId;
        UserId = userId;
        Timestamp = DateTime.UtcNow;
    }
}

// Delegate definitions
public delegate void MoodEntryCreatedHandler(object sender, MoodEntryEventArgs e);
public delegate void MoodEntryUpdatedHandler(object sender, MoodEntryEventArgs e);
public delegate void MoodEntryDeletedHandler(object sender, MoodEntryDeletedEventArgs e);
public delegate Task MoodEntryAsyncHandler(object sender, MoodEntryEventArgs e);

// Event publisher interface
public interface IMoodEntryEventPublisher
{
    event MoodEntryCreatedHandler? MoodEntryCreated;
    event MoodEntryUpdatedHandler? MoodEntryUpdated;
    event MoodEntryDeletedHandler? MoodEntryDeleted;
    event MoodEntryAsyncHandler? MoodEntryCreatedAsync;
    
    void OnMoodEntryCreated(MoodEntryDto moodEntry, int userId);
    void OnMoodEntryUpdated(MoodEntryDto moodEntry, int userId);
    void OnMoodEntryDeleted(int moodEntryId, int userId);
    Task OnMoodEntryCreatedAsync(MoodEntryDto moodEntry, int userId);
}

// Event publisher implementation
public class MoodEntryEventPublisher : IMoodEntryEventPublisher
{
    public event MoodEntryCreatedHandler? MoodEntryCreated;
    public event MoodEntryUpdatedHandler? MoodEntryUpdated;
    public event MoodEntryDeletedHandler? MoodEntryDeleted;
    public event MoodEntryAsyncHandler? MoodEntryCreatedAsync;

    public void OnMoodEntryCreated(MoodEntryDto moodEntry, int userId)
    {
        var args = new MoodEntryEventArgs(moodEntry, userId);
        MoodEntryCreated?.Invoke(this, args);
    }

    public void OnMoodEntryUpdated(MoodEntryDto moodEntry, int userId)
    {
        var args = new MoodEntryEventArgs(moodEntry, userId);
        MoodEntryUpdated?.Invoke(this, args);
    }

    public void OnMoodEntryDeleted(int moodEntryId, int userId)
    {
        var args = new MoodEntryDeletedEventArgs(moodEntryId, userId);
        MoodEntryDeleted?.Invoke(this, args);
    }

    public async Task OnMoodEntryCreatedAsync(MoodEntryDto moodEntry, int userId)
    {
        var args = new MoodEntryEventArgs(moodEntry, userId);
        if (MoodEntryCreatedAsync != null)
        {
            await MoodEntryCreatedAsync.Invoke(this, args);
        }
    }
}

// Event handlers for various operations
public class MoodEntryAuditHandler
{
    private readonly List<string> _auditLog = new();

    public void HandleMoodEntryCreated(object sender, MoodEntryEventArgs e)
    {
        var logEntry = $"[{e.Timestamp:yyyy-MM-dd HH:mm:ss}] User {e.UserId} created mood entry {e.MoodEntry.Id} with level {e.MoodEntry.MoodLevel}";
        _auditLog.Add(logEntry);
        Console.WriteLine($"AUDIT: {logEntry}");
    }

    public void HandleMoodEntryUpdated(object sender, MoodEntryEventArgs e)
    {
        var logEntry = $"[{e.Timestamp:yyyy-MM-dd HH:mm:ss}] User {e.UserId} updated mood entry {e.MoodEntry.Id} with level {e.MoodEntry.MoodLevel}";
        _auditLog.Add(logEntry);
        Console.WriteLine($"AUDIT: {logEntry}");
    }

    public void HandleMoodEntryDeleted(object sender, MoodEntryDeletedEventArgs e)
    {
        var logEntry = $"[{e.Timestamp:yyyy-MM-dd HH:mm:ss}] User {e.UserId} deleted mood entry {e.MoodEntryId}";
        _auditLog.Add(logEntry);
        Console.WriteLine($"AUDIT: {logEntry}");
    }

    public IReadOnlyList<string> GetAuditLog() => _auditLog.AsReadOnly();
}

public class MoodEntryNotificationHandler
{
    public async Task HandleMoodEntryCreatedAsync(object sender, MoodEntryEventArgs e)
    {
        // Simulate async notification processing
        await Task.Delay(100);
        
        if (e.MoodEntry.MoodLevel <= 3)
        {
            Console.WriteLine($"NOTIFICATION: Low mood detected for user {e.UserId}. Consider reaching out for support.");
        }
        else if (e.MoodEntry.MoodLevel >= 8)
        {
            Console.WriteLine($"NOTIFICATION: High mood detected for user {e.UserId}. Great day!");
        }
    }
}
