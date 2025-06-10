namespace MoodLog.Application.Interfaces;

public interface IDataSeedingService
{
    Task SeedMoodDataForUserAsync(int userId, int entryCount = 45);
}
