using MoodLog.Core.DTOs;
using MoodLog.Application.Interfaces;

namespace MoodLog.Application.Services
{
    public class MockDataService
    {
        private readonly IMoodEntryService _moodEntryService;
        private readonly IMoodTagService _moodTagService;
        private readonly Random _random;

        public MockDataService(IMoodEntryService moodEntryService, IMoodTagService moodTagService)
        {
            _moodEntryService = moodEntryService;
            _moodTagService = moodTagService;
            _random = new Random(42); // Fixed seed for consistent demo data
        }

        public async Task<bool> SeedMockDataAsync(string userId)
        {
            try
            {
                var userIdInt = int.Parse(userId);

                // Clear existing data for this user
                await ClearUserDataAsync(userIdInt);

                // Create comprehensive tag system
                var tags = await CreateMockTagsAsync();

                // Create realistic mood entries
                await CreateMockMoodEntriesAsync(userIdInt, tags);

                return true;
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                Console.WriteLine($"MockDataService Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return false;
            }
        }

        private async Task ClearUserDataAsync(int userId)
        {
            // Get existing entries for this user
            var existingEntries = await _moodEntryService.GetByUserIdAsync(userId);

            // Delete existing entries (this will be handled by the service layer)
            foreach (var entry in existingEntries)
            {
                await _moodEntryService.DeleteAsync(entry.Id, userId);
            }
        }

        private async Task<List<MoodTagDto>> CreateMockTagsAsync()
        {
            var tagData = new[]
            {
                // Emotional States
                ("Happy", "#28a745", "Feeling joyful and content"),
                ("Sad", "#6c757d", "Feeling down or melancholy"),
                ("Anxious", "#ffc107", "Feeling worried or nervous"),
                ("Angry", "#dc3545", "Feeling frustrated or irritated"),
                ("Excited", "#fd7e14", "Feeling enthusiastic and energetic"),
                ("Calm", "#20c997", "Feeling peaceful and relaxed"),
                ("Stressed", "#e83e8c", "Feeling overwhelmed or pressured"),
                ("Tired", "#6f42c1", "Feeling exhausted or drained"),
                ("Grateful", "#17a2b8", "Feeling thankful and appreciative"),
                ("Confident", "#007bff", "Feeling self-assured and capable"),

                // Activities
                ("Work", "#495057", "Work-related activities"),
                ("Exercise", "#28a745", "Physical activity and fitness"),
                ("Social", "#fd7e14", "Time with friends and family"),
                ("Family", "#e83e8c", "Family time and activities"),
                ("Travel", "#17a2b8", "Travel and exploration"),
                ("Hobbies", "#6f42c1", "Personal interests and hobbies"),
                ("Reading", "#20c997", "Reading books or articles"),
                ("Music", "#ffc107", "Listening to or playing music"),
                ("Cooking", "#fd7e14", "Preparing or enjoying food"),
                ("Gaming", "#007bff", "Playing video games"),

                // Triggers & Context
                ("Weather", "#6c757d", "Weather-related mood impact"),
                ("Sleep", "#495057", "Sleep quality impact"),
                ("Caffeine", "#dc3545", "Coffee or caffeine consumption"),
                ("Meetings", "#ffc107", "Work meetings and calls"),
                ("Morning", "#28a745", "Morning time period"),
                ("Evening", "#6f42c1", "Evening time period"),
                ("Weekend", "#17a2b8", "Weekend activities"),
                ("Weekday", "#495057", "Weekday routine"),
                ("Meditation", "#20c997", "Mindfulness and meditation"),
                ("Nature", "#28a745", "Time spent outdoors")
            };

            var tags = new List<MoodTagDto>();
            var existingTags = await _moodTagService.GetAllActiveAsync();

            foreach (var (name, color, description) in tagData)
            {
                var existingTag = existingTags.FirstOrDefault(t => t.Name == name);

                if (existingTag == null)
                {
                    var createDto = new MoodTagCreateDto
                    {
                        Name = name,
                        Color = color,
                        Description = description
                    };

                    var newTag = await _moodTagService.CreateAsync(createDto, true); // true = system tag
                    tags.Add(newTag);
                }
                else
                {
                    tags.Add(existingTag);
                }
            }

            return tags;
        }

        private async Task CreateMockMoodEntriesAsync(int userId, List<MoodTagDto> tags)
        {
            var startDate = DateTime.Now.AddDays(-90); // 3 months ago

            // Create realistic mood patterns
            var moodPatterns = GenerateRealisticMoodPatterns(startDate, 90);

            for (int i = 0; i < moodPatterns.Count; i++)
            {
                var pattern = moodPatterns[i];
                var entryDate = startDate.AddDays(i);

                // Skip some days randomly (not everyone logs daily)
                if (_random.NextDouble() < 0.25) continue; // 25% chance to skip

                // Select appropriate tags for this mood level
                var selectedTags = SelectTagsForMood(pattern.MoodLevel, tags, entryDate);

                try
                {
                    var createDto = new MoodEntryCreateDto
                    {
                        MoodLevel = pattern.MoodLevel,
                        Notes = pattern.Notes,
                        EntryDate = entryDate.Date.AddHours(pattern.Hour).AddMinutes(_random.Next(0, 60)),
                        TagIds = selectedTags.Select(t => t.Id).ToList()
                    };

                    await _moodEntryService.CreateAsync(createDto, userId);
                }
                catch (InvalidOperationException)
                {
                    // Skip if entry already exists for this date
                    continue;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating mood entry for {entryDate:yyyy-MM-dd}: {ex.Message}");
                    continue;
                }
            }
        }

        private List<MoodPattern> GenerateRealisticMoodPatterns(DateTime startDate, int days)
        {
            var patterns = new List<MoodPattern>();
            var baseMood = 6.0; // Neutral starting point
            var stressLevel = 0.3; // Overall stress factor

            for (int day = 0; day < days; day++)
            {
                var currentDate = startDate.AddDays(day);
                var dayOfWeek = currentDate.DayOfWeek;
                
                // Weekly patterns
                var weekendBoost = (dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday) ? 1 : 0;
                var mondayDip = dayOfWeek == DayOfWeek.Monday ? -1 : 0;
                var fridayBoost = dayOfWeek == DayOfWeek.Friday ? 1 : 0;

                // Monthly patterns (simulate life events)
                var monthlyVariation = GetMonthlyVariation(day);
                
                // Random daily variation
                var dailyVariation = (_random.NextDouble() - 0.5) * 2; // -1 to +1

                // Calculate mood with realistic constraints
                var rawMood = baseMood + weekendBoost + mondayDip + fridayBoost + monthlyVariation + dailyVariation;
                var moodLevel = Math.Max(1, Math.Min(10, (int)Math.Round(rawMood)));

                // Determine time of day based on mood and day type
                var hour = GetRealisticHour(dayOfWeek, moodLevel);

                patterns.Add(new MoodPattern
                {
                    MoodLevel = moodLevel,
                    Hour = hour,
                    Notes = GenerateRealisticNotes(moodLevel, dayOfWeek, day)
                });

                // Gradual mood evolution
                baseMood = baseMood * 0.9 + moodLevel * 0.1; // Slow adaptation
            }

            return patterns;
        }

        private double GetMonthlyVariation(int day)
        {
            // Simulate life events and seasonal changes
            if (day >= 20 && day <= 35) return -1.5; // Stressful period
            if (day >= 45 && day <= 55) return 1.5;  // Good period
            if (day >= 70 && day <= 75) return -1;   // Minor setback
            return 0;
        }

        private int GetRealisticHour(DayOfWeek dayOfWeek, int moodLevel)
        {
            if (dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday)
            {
                // Weekends: more varied times
                return _random.Next(8, 22);
            }
            else
            {
                // Weekdays: morning check-ins for planning, evening for reflection
                return _random.NextDouble() < 0.6 ? _random.Next(7, 10) : _random.Next(17, 22);
            }
        }

        private string GenerateRealisticNotes(int moodLevel, DayOfWeek dayOfWeek, int dayIndex)
        {
            var notes = new List<string>();

            // Mood-based notes
            switch (moodLevel)
            {
                case 1:
                case 2:
                    notes.AddRange(new[] {
                        "Having a really tough day. Everything feels overwhelming.",
                        "Struggling with motivation today. Need to be gentle with myself.",
                        "Feeling quite low. Maybe tomorrow will be better.",
                        "Not my best day. Taking things one step at a time."
                    });
                    break;
                case 3:
                case 4:
                    notes.AddRange(new[] {
                        "Feeling a bit down but managing. Small steps forward.",
                        "Not great but not terrible either. Just getting through today.",
                        "Mood is low but I'm trying to stay positive.",
                        "Challenging day but I'm coping. Self-care is important."
                    });
                    break;
                case 5:
                case 6:
                    notes.AddRange(new[] {
                        "Feeling pretty neutral today. Nothing special but stable.",
                        "Average day. Some ups and downs but overall okay.",
                        "Steady mood today. Focusing on routine and basics.",
                        "Balanced day. Taking care of responsibilities."
                    });
                    break;
                case 7:
                case 8:
                    notes.AddRange(new[] {
                        "Good day! Feeling productive and positive.",
                        "Really enjoying today. Energy levels are great.",
                        "Feeling accomplished and content with progress.",
                        "Positive mood today. Things are going well."
                    });
                    break;
                case 9:
                case 10:
                    notes.AddRange(new[] {
                        "Amazing day! Everything is clicking into place.",
                        "Feeling fantastic! So grateful for this positive energy.",
                        "Incredible mood today. Life feels wonderful right now.",
                        "Perfect day! Feeling blessed and energized."
                    });
                    break;
            }

            // Day-specific additions
            if (dayOfWeek == DayOfWeek.Monday)
                notes.Add("Monday motivation is real today!");
            if (dayOfWeek == DayOfWeek.Friday)
                notes.Add("TGIF! Looking forward to the weekend.");
            if (dayOfWeek == DayOfWeek.Sunday)
                notes.Add("Sunday reflection time. Preparing for the week ahead.");

            // Special events based on day index
            if (dayIndex == 30) notes.Add("Had a great workout session this morning!");
            if (dayIndex == 45) notes.Add("Spent quality time with family today.");
            if (dayIndex == 60) notes.Add("Finished a big project at work. Feeling accomplished!");

            return notes[_random.Next(notes.Count)];
        }

        private List<MoodTagDto> SelectTagsForMood(int moodLevel, List<MoodTagDto> tags, DateTime entryDate)
        {
            var selectedTags = new List<MoodTagDto>();

            var emotionalTags = tags.Where(t => new[] { "Happy", "Sad", "Anxious", "Angry", "Excited", "Calm", "Stressed", "Tired", "Grateful", "Confident" }.Contains(t.Name)).ToList();
            var activityTags = tags.Where(t => new[] { "Work", "Exercise", "Social", "Family", "Travel", "Hobbies" }.Contains(t.Name)).ToList();
            var contextTags = tags.Where(t => new[] { "Morning", "Evening", "Weekend", "Weekday", "Weather", "Sleep" }.Contains(t.Name)).ToList();

            // Add emotional tag based on mood level
            if (moodLevel <= 3)
            {
                var lowMoodTags = emotionalTags.Where(t => new[] { "Sad", "Anxious", "Stressed", "Tired" }.Contains(t.Name)).ToList();
                if (lowMoodTags.Any())
                    selectedTags.Add(lowMoodTags[_random.Next(lowMoodTags.Count)]);
            }
            else if (moodLevel <= 6)
            {
                var neutralMoodTags = emotionalTags.Where(t => new[] { "Calm", "Tired" }.Contains(t.Name)).ToList();
                if (neutralMoodTags.Any())
                    selectedTags.Add(neutralMoodTags[_random.Next(neutralMoodTags.Count)]);
            }
            else
            {
                var highMoodTags = emotionalTags.Where(t => new[] { "Happy", "Excited", "Confident", "Grateful" }.Contains(t.Name)).ToList();
                if (highMoodTags.Any())
                    selectedTags.Add(highMoodTags[_random.Next(highMoodTags.Count)]);
            }

            // Add activity tags (50% chance)
            if (_random.NextDouble() < 0.5 && activityTags.Any())
            {
                selectedTags.Add(activityTags[_random.Next(activityTags.Count)]);
            }

            // Add context tags
            var dayOfWeek = entryDate.DayOfWeek;
            var weekendTag = contextTags.FirstOrDefault(t => t.Name == "Weekend");
            var weekdayTag = contextTags.FirstOrDefault(t => t.Name == "Weekday");

            if (dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday)
            {
                if (weekendTag != null) selectedTags.Add(weekendTag);
            }
            else
            {
                if (weekdayTag != null) selectedTags.Add(weekdayTag);
            }

            var morningTag = contextTags.FirstOrDefault(t => t.Name == "Morning");
            var eveningTag = contextTags.FirstOrDefault(t => t.Name == "Evening");

            if (entryDate.Hour < 12)
            {
                if (morningTag != null) selectedTags.Add(morningTag);
            }
            else
            {
                if (eveningTag != null) selectedTags.Add(eveningTag);
            }

            return selectedTags.Distinct().ToList();
        }

        private class MoodPattern
        {
            public int MoodLevel { get; set; }
            public int Hour { get; set; }
            public string Notes { get; set; } = string.Empty;
        }
    }
}
