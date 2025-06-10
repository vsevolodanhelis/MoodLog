namespace MoodLog.Core.Entities;

public class MoodEntryTag
{
    public int MoodEntryId { get; set; }
    public int MoodTagId { get; set; }
    
    // Navigation properties
    public virtual MoodEntry MoodEntry { get; set; } = null!;
    public virtual MoodTag MoodTag { get; set; } = null!;
}
