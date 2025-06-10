using MoodLog.Application.Interfaces;
using MoodLog.Core.Entities;
using MoodLog.Core.Interfaces;

namespace MoodLog.Application.Services;

public class DataSeedingService : IDataSeedingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly Random _random;

    public DataSeedingService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _random = new Random(42); // Fixed seed for reproducible data
    }

    public async Task SeedMoodDataForUserAsync(int userId, int entryCount = 45)
    {
        // Check if user already has data
        var existingEntries = await _unitOfWork.MoodEntries.GetByUserIdAsync(userId);
        if (existingEntries.Any())
        {
            return; // Don't seed if data already exists
        }

        // Get existing mood tags from database
        var allTags = await _unitOfWork.MoodTags.GetAllAsync();
        var tagLookup = allTags.ToDictionary(t => t.Name, t => t);

        var moodEntries = await GenerateRealisticMoodEntriesAsync(userId, entryCount, tagLookup);

        foreach (var entry in moodEntries)
        {
            await _unitOfWork.MoodEntries.AddAsync(entry);
        }

        await _unitOfWork.SaveChangesAsync();
    }

    private Task<List<MoodEntry>> GenerateRealisticMoodEntriesAsync(int userId, int entryCount, Dictionary<string, MoodTag> tagLookup)
    {
        var entries = new List<MoodEntry>();
        var startDate = DateTime.Today.AddDays(-90); // 3 months ago
        var endDate = DateTime.Today.AddDays(-1); // Yesterday

        // Generate realistic scenarios
        var scenarios = GetRealisticScenarios();

        // Create date range with some gaps (realistic usage)
        var dates = GenerateRealisticDates(startDate, endDate, entryCount);

        foreach (var date in dates)
        {
            var entry = GenerateMoodEntryForDate(userId, date, scenarios, tagLookup);
            entries.Add(entry);
        }

        return Task.FromResult(entries.OrderBy(e => e.EntryDate).ToList());
    }

    private List<DateTime> GenerateRealisticDates(DateTime start, DateTime end, int targetCount)
    {
        var dates = new List<DateTime>();
        var current = start;
        var totalDays = (end - start).Days;
        var skipProbability = Math.Max(0.1, 1.0 - (double)targetCount / totalDays);

        while (current <= end && dates.Count < targetCount)
        {
            // Higher chance of logging on weekends and lower on Mondays
            var dayOfWeek = current.DayOfWeek;
            var logProbability = dayOfWeek switch
            {
                DayOfWeek.Monday => 0.6,
                DayOfWeek.Tuesday => 0.8,
                DayOfWeek.Wednesday => 0.8,
                DayOfWeek.Thursday => 0.8,
                DayOfWeek.Friday => 0.9,
                DayOfWeek.Saturday => 0.9,
                DayOfWeek.Sunday => 0.85,
                _ => 0.8
            };

            if (_random.NextDouble() > skipProbability * (1 - logProbability))
            {
                dates.Add(current);
            }

            current = current.AddDays(1);
        }

        return dates;
    }

    private MoodEntry GenerateMoodEntryForDate(int userId, DateTime date, List<MoodScenario> scenarios, Dictionary<string, MoodTag> tagLookup)
    {
        var scenario = SelectScenarioForDate(date, scenarios);
        var moodLevel = GenerateMoodLevel(date, scenario);
        var selectedTagNames = SelectTagsForScenario(scenario, tagLookup.Keys.ToList());

        var entry = new MoodEntry
        {
            UserId = userId,
            MoodLevel = moodLevel,
            Notes = scenario.Note,
            EntryDate = date,
            CreatedAt = date.AddHours(_random.Next(8, 22)).AddMinutes(_random.Next(0, 60)),
            MoodEntryTags = new List<MoodEntryTag>()
        };

        // Create MoodEntryTag relationships for existing tags
        foreach (var tagName in selectedTagNames)
        {
            if (tagLookup.ContainsKey(tagName))
            {
                entry.MoodEntryTags.Add(new MoodEntryTag
                {
                    MoodEntry = entry,
                    MoodTagId = tagLookup[tagName].Id
                });
            }
        }

        return entry;
    }

    private MoodScenario SelectScenarioForDate(DateTime date, List<MoodScenario> scenarios)
    {
        var dayOfWeek = date.DayOfWeek;
        var isWeekend = dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday;
        var isMonday = dayOfWeek == DayOfWeek.Monday;
        var isFriday = dayOfWeek == DayOfWeek.Friday;

        // Filter scenarios based on day type
        var relevantScenarios = scenarios.Where(s => 
            (isWeekend && s.IsWeekendAppropriate) ||
            (isMonday && s.IsMondayAppropriate) ||
            (isFriday && s.IsFridayAppropriate) ||
            (!isWeekend && !isMonday && !isFriday && s.IsWeekdayAppropriate)
        ).ToList();

        return relevantScenarios[_random.Next(relevantScenarios.Count)];
    }

    private int GenerateMoodLevel(DateTime date, MoodScenario scenario)
    {
        var baseMood = scenario.BaseMoodLevel;
        var dayOfWeek = date.DayOfWeek;
        
        // Weekend boost
        if (dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday)
        {
            baseMood += _random.Next(0, 2);
        }
        
        // Monday blues
        if (dayOfWeek == DayOfWeek.Monday)
        {
            baseMood -= _random.Next(0, 2);
        }
        
        // Friday excitement
        if (dayOfWeek == DayOfWeek.Friday)
        {
            baseMood += _random.Next(0, 1);
        }
        
        // Add some randomness
        baseMood += _random.Next(-1, 2);
        
        // Ensure within bounds (1-10 scale)
        return Math.Max(1, Math.Min(10, baseMood));
    }

    private List<string> SelectTagsForScenario(MoodScenario scenario, List<string> availableTags)
    {
        var selectedTags = new List<string>();
        
        // Add primary tags from scenario
        selectedTags.AddRange(scenario.PrimaryTags);
        
        // Add 1-2 random additional tags
        var remainingTags = availableTags.Except(selectedTags).ToList();
        var additionalTagCount = _random.Next(0, 3);
        
        for (int i = 0; i < additionalTagCount && remainingTags.Any(); i++)
        {
            var randomTag = remainingTags[_random.Next(remainingTags.Count)];
            selectedTags.Add(randomTag);
            remainingTags.Remove(randomTag);
        }
        
        return selectedTags.Take(4).ToList(); // Limit to 4 tags max
    }



    private List<MoodScenario> GetRealisticScenarios()
    {
        return new List<MoodScenario>
        {
            // Work-related scenarios
            new MoodScenario
            {
                Note = "Had a productive day at work. Completed all my tasks and felt accomplished.",
                BaseMoodLevel = 7,
                PrimaryTags = new[] { "Happy", "Calm" },
                IsWeekdayAppropriate = true,
                IsWeekendAppropriate = false,
                IsMondayAppropriate = true,
                IsFridayAppropriate = true
            },
            new MoodScenario
            {
                Note = "Stressful day with tight deadlines. Feeling overwhelmed but managed to push through.",
                BaseMoodLevel = 4,
                PrimaryTags = new[] { "Stressed", "Tired" },
                IsWeekdayAppropriate = true,
                IsWeekendAppropriate = false,
                IsMondayAppropriate = true,
                IsFridayAppropriate = false
            },
            new MoodScenario
            {
                Note = "Great team meeting today. Excited about the new project we're starting.",
                BaseMoodLevel = 8,
                PrimaryTags = new[] { "Excited", "Happy" },
                IsWeekdayAppropriate = true,
                IsWeekendAppropriate = false,
                IsMondayAppropriate = false,
                IsFridayAppropriate = true
            },
            new MoodScenario
            {
                Note = "Monday blues hitting hard. Struggling to get motivated after the weekend.",
                BaseMoodLevel = 3,
                PrimaryTags = new[] { "Sad", "Tired" },
                IsWeekdayAppropriate = false,
                IsWeekendAppropriate = false,
                IsMondayAppropriate = true,
                IsFridayAppropriate = false
            },
            
            // Weekend scenarios
            new MoodScenario
            {
                Note = "Wonderful weekend with family. Went for a hike and had a great barbecue.",
                BaseMoodLevel = 9,
                PrimaryTags = new[] { "Happy", "Excited" },
                IsWeekdayAppropriate = false,
                IsWeekendAppropriate = true,
                IsMondayAppropriate = false,
                IsFridayAppropriate = false
            },
            new MoodScenario
            {
                Note = "Relaxing Sunday. Read a good book and took a long bath. Feeling recharged.",
                BaseMoodLevel = 8,
                PrimaryTags = new[] { "Calm", "Happy" },
                IsWeekdayAppropriate = false,
                IsWeekendAppropriate = true,
                IsMondayAppropriate = false,
                IsFridayAppropriate = false
            },
            new MoodScenario
            {
                Note = "Lazy weekend day. Didn't accomplish much but sometimes that's okay.",
                BaseMoodLevel = 6,
                PrimaryTags = new[] { "Calm", "Tired" },
                IsWeekdayAppropriate = false,
                IsWeekendAppropriate = true,
                IsMondayAppropriate = false,
                IsFridayAppropriate = false
            },

            // Exercise and health scenarios
            new MoodScenario
            {
                Note = "Great workout at the gym today! Feeling energized and strong.",
                BaseMoodLevel = 8,
                PrimaryTags = new[] { "Excited", "Happy" },
                IsWeekdayAppropriate = true,
                IsWeekendAppropriate = true,
                IsMondayAppropriate = true,
                IsFridayAppropriate = true
            },
            new MoodScenario
            {
                Note = "Went for a morning run. Fresh air and exercise always boost my mood.",
                BaseMoodLevel = 7,
                PrimaryTags = new[] { "Happy", "Calm" },
                IsWeekdayAppropriate = true,
                IsWeekendAppropriate = true,
                IsMondayAppropriate = true,
                IsFridayAppropriate = true
            },
            new MoodScenario
            {
                Note = "Feeling under the weather today. Low energy and just want to rest.",
                BaseMoodLevel = 3,
                PrimaryTags = new[] { "Tired", "Sad" },
                IsWeekdayAppropriate = true,
                IsWeekendAppropriate = true,
                IsMondayAppropriate = true,
                IsFridayAppropriate = true
            },

            // Social scenarios
            new MoodScenario
            {
                Note = "Had dinner with friends tonight. Great conversation and lots of laughs!",
                BaseMoodLevel = 9,
                PrimaryTags = new[] { "Happy", "Excited" },
                IsWeekdayAppropriate = true,
                IsWeekendAppropriate = true,
                IsMondayAppropriate = false,
                IsFridayAppropriate = true
            },
            new MoodScenario
            {
                Note = "Feeling lonely today. Missing my friends and family who live far away.",
                BaseMoodLevel = 4,
                PrimaryTags = new[] { "Sad", "Anxious" },
                IsWeekdayAppropriate = true,
                IsWeekendAppropriate = true,
                IsMondayAppropriate = true,
                IsFridayAppropriate = false
            },
            new MoodScenario
            {
                Note = "Video call with family was so nice. Technology helps us stay connected.",
                BaseMoodLevel = 7,
                PrimaryTags = new[] { "Happy", "Calm" },
                IsWeekdayAppropriate = true,
                IsWeekendAppropriate = true,
                IsMondayAppropriate = true,
                IsFridayAppropriate = true
            },

            // Personal growth scenarios
            new MoodScenario
            {
                Note = "Finished reading an inspiring book. Feeling motivated to make positive changes.",
                BaseMoodLevel = 8,
                PrimaryTags = new[] { "Excited", "Happy" },
                IsWeekdayAppropriate = true,
                IsWeekendAppropriate = true,
                IsMondayAppropriate = true,
                IsFridayAppropriate = true
            },
            new MoodScenario
            {
                Note = "Meditation session went well. Feeling centered and peaceful.",
                BaseMoodLevel = 7,
                PrimaryTags = new[] { "Calm", "Happy" },
                IsWeekdayAppropriate = true,
                IsWeekendAppropriate = true,
                IsMondayAppropriate = true,
                IsFridayAppropriate = true
            },
            new MoodScenario
            {
                Note = "Struggling with self-doubt today. Need to be kinder to myself.",
                BaseMoodLevel = 4,
                PrimaryTags = new[] { "Anxious", "Sad" },
                IsWeekdayAppropriate = true,
                IsWeekendAppropriate = true,
                IsMondayAppropriate = true,
                IsFridayAppropriate = true
            },

            // Daily life scenarios
            new MoodScenario
            {
                Note = "Beautiful sunny day! Spent time in the garden and felt so peaceful.",
                BaseMoodLevel = 8,
                PrimaryTags = new[] { "Happy", "Calm" },
                IsWeekdayAppropriate = true,
                IsWeekendAppropriate = true,
                IsMondayAppropriate = true,
                IsFridayAppropriate = true
            },
            new MoodScenario
            {
                Note = "Rainy day got me feeling a bit down. Sometimes weather affects my mood.",
                BaseMoodLevel = 5,
                PrimaryTags = new[] { "Sad", "Tired" },
                IsWeekdayAppropriate = true,
                IsWeekendAppropriate = true,
                IsMondayAppropriate = true,
                IsFridayAppropriate = true
            },
            new MoodScenario
            {
                Note = "Cooked a new recipe today and it turned out amazing! Small wins matter.",
                BaseMoodLevel = 7,
                PrimaryTags = new[] { "Happy", "Excited" },
                IsWeekdayAppropriate = true,
                IsWeekendAppropriate = true,
                IsMondayAppropriate = true,
                IsFridayAppropriate = true
            },

            // Challenging scenarios
            new MoodScenario
            {
                Note = "Had an argument with a colleague. Feeling frustrated and need to cool down.",
                BaseMoodLevel = 3,
                PrimaryTags = new[] { "Angry", "Stressed" },
                IsWeekdayAppropriate = true,
                IsWeekendAppropriate = false,
                IsMondayAppropriate = true,
                IsFridayAppropriate = false
            },
            new MoodScenario
            {
                Note = "Received some disappointing news today. Trying to stay positive but it's hard.",
                BaseMoodLevel = 4,
                PrimaryTags = new[] { "Sad", "Anxious" },
                IsWeekdayAppropriate = true,
                IsWeekendAppropriate = true,
                IsMondayAppropriate = true,
                IsFridayAppropriate = true
            },
            new MoodScenario
            {
                Note = "Overcame a fear today by trying something new. Proud of myself!",
                BaseMoodLevel = 8,
                PrimaryTags = new[] { "Excited", "Happy" },
                IsWeekdayAppropriate = true,
                IsWeekendAppropriate = true,
                IsMondayAppropriate = true,
                IsFridayAppropriate = true
            }
        };
    }

    private class MoodScenario
    {
        public string Note { get; set; } = string.Empty;
        public int BaseMoodLevel { get; set; }
        public string[] PrimaryTags { get; set; } = Array.Empty<string>();
        public bool IsWeekdayAppropriate { get; set; }
        public bool IsWeekendAppropriate { get; set; }
        public bool IsMondayAppropriate { get; set; }
        public bool IsFridayAppropriate { get; set; }
    }
}
