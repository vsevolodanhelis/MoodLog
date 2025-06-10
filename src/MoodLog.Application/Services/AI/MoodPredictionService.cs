using Microsoft.ML;
using Microsoft.ML.Data;
using MoodLog.Application.Models.ML;
using MoodLog.Core.Entities;
using MoodLog.Core.Interfaces;

namespace MoodLog.Application.Services.AI;

public interface IMoodPredictionService
{
    Task<MoodInsight> GetMoodPredictionAsync(int userId);
    Task<List<PatternDetectionResult>> DetectPatternsAsync(int userId);
    Task<WeeklySummaryData> GenerateWeeklySummaryAsync(int userId, DateTime weekStart);
    Task TrainModelAsync(int userId);
    Task<float> GetPredictionAccuracyAsync(int userId);
}

public class MoodPredictionService : IMoodPredictionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly MLContext _mlContext;
    private readonly string _modelPath;

    public MoodPredictionService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _mlContext = new MLContext(seed: 42);
        _modelPath = Path.Combine(Directory.GetCurrentDirectory(), "Models", "mood_prediction_model.zip");
    }

    public async Task<MoodInsight> GetMoodPredictionAsync(int userId)
    {
        try
        {
            var entries = await _unitOfWork.MoodEntries.GetByUserIdAsync(userId);
            if (entries.Count() < 7) // Need at least a week of data
            {
                return new MoodInsight
                {
                    InsightText = "Keep logging your mood for a few more days to get personalized predictions!",
                    Confidence = 0.0f,
                    Category = "prediction",
                    GeneratedAt = DateTime.UtcNow
                };
            }

            var predictionData = await PrepareCurrentPredictionDataAsync(userId);
            var prediction = await PredictMoodAsync(predictionData);
            
            var insight = GeneratePredictionInsight(prediction, predictionData);
            return insight;
        }
        catch (Exception ex)
        {
            return new MoodInsight
            {
                InsightText = "Unable to generate prediction at this time. Please try again later.",
                Confidence = 0.0f,
                Category = "prediction",
                GeneratedAt = DateTime.UtcNow
            };
        }
    }

    public async Task<List<PatternDetectionResult>> DetectPatternsAsync(int userId)
    {
        var patterns = new List<PatternDetectionResult>();
        var entries = await _unitOfWork.MoodEntries.GetByUserIdAsync(userId);
        
        if (entries.Count() < 14) // Need at least 2 weeks of data
        {
            return patterns;
        }

        var entriesList = entries.OrderBy(e => e.EntryDate).ToList();

        // Detect day-of-week patterns
        var dayOfWeekPattern = DetectDayOfWeekPatterns(entriesList);
        if (dayOfWeekPattern != null) patterns.Add(dayOfWeekPattern);

        // Detect tag-based triggers
        var tagPatterns = DetectTagPatterns(entriesList);
        patterns.AddRange(tagPatterns);

        // Detect mood trends
        var trendPattern = DetectMoodTrends(entriesList);
        if (trendPattern != null) patterns.Add(trendPattern);

        return patterns;
    }

    public async Task<WeeklySummaryData> GenerateWeeklySummaryAsync(int userId, DateTime weekStart)
    {
        var weekEnd = weekStart.AddDays(7);
        var entries = await _unitOfWork.MoodEntries.GetByUserIdAsync(userId);
        var weekEntries = entries.Where(e => e.EntryDate >= weekStart && e.EntryDate < weekEnd).ToList();

        if (!weekEntries.Any())
        {
            return new WeeklySummaryData
            {
                WeekStartDate = weekStart,
                WeekEndDate = weekEnd,
                TotalEntries = 0
            };
        }

        var avgMood = weekEntries.Average(e => e.MoodLevel);
        var moodVariability = CalculateVariability(weekEntries.Select(e => (float)e.MoodLevel));
        
        var allTags = weekEntries.SelectMany(e => e.MoodEntryTags.Select(t => t.MoodTag.Name)).ToList();
        var dominantTags = allTags.GroupBy(t => t)
            .OrderByDescending(g => g.Count())
            .Take(3)
            .Select(g => g.Key)
            .ToArray();

        var trend = CalculateMoodTrend(weekEntries);

        return new WeeklySummaryData
        {
            WeekStartDate = weekStart,
            WeekEndDate = weekEnd,
            AverageMood = (float)avgMood,
            MoodVariability = moodVariability,
            DominantTags = dominantTags,
            TotalEntries = weekEntries.Count,
            MoodTrend = trend
        };
    }

    public async Task TrainModelAsync(int userId)
    {
        var entries = await _unitOfWork.MoodEntries.GetByUserIdAsync(userId);
        if (entries.Count() < 30) return; // Need sufficient data for training

        var trainingData = await PrepareTrainingDataAsync(userId);
        
        // Create ML pipeline
        var pipeline = _mlContext.Transforms.Concatenate("Features", 
                "DayOfWeek", "HourOfDay", "DaysSinceLastEntry", "AverageMoodLast7Days",
                "AverageMoodLast30Days", "HasStressTag", "HasHappyTag", "HasAnxiousTag",
                "HasTiredTag", "WeekendFactor", "MoodTrend", "SeasonalFactor")
            .Append(_mlContext.Regression.Trainers.Sdca(labelColumnName: "Label", maximumNumberOfIterations: 100));

        // Train the model
        var model = pipeline.Fit(trainingData);

        // Save the model
        Directory.CreateDirectory(Path.GetDirectoryName(_modelPath)!);
        _mlContext.Model.Save(model, trainingData.Schema, _modelPath);
    }

    public async Task<float> GetPredictionAccuracyAsync(int userId)
    {
        // Implementation for tracking prediction accuracy
        // This would compare past predictions with actual mood entries
        return 0.75f; // Placeholder - implement actual accuracy calculation
    }

    private async Task<MoodPredictionData> PrepareCurrentPredictionDataAsync(int userId)
    {
        var entries = await _unitOfWork.MoodEntries.GetByUserIdAsync(userId);
        var recentEntries = entries.OrderByDescending(e => e.EntryDate).Take(30).ToList();
        
        var now = DateTime.Now;
        var last7Days = recentEntries.Where(e => e.EntryDate >= now.AddDays(-7)).ToList();
        var last30Days = recentEntries.Where(e => e.EntryDate >= now.AddDays(-30)).ToList();

        return new MoodPredictionData
        {
            DayOfWeek = (float)now.DayOfWeek,
            HourOfDay = now.Hour,
            DaysSinceLastEntry = recentEntries.Any() ? (float)(now - recentEntries.First().EntryDate).TotalDays : 0,
            AverageMoodLast7Days = last7Days.Any() ? (float)last7Days.Average(e => e.MoodLevel) : 5.0f,
            AverageMoodLast30Days = last30Days.Any() ? (float)last30Days.Average(e => e.MoodLevel) : 5.0f,
            HasStressTag = last7Days.Any(e => e.MoodEntryTags.Any(t => t.MoodTag.Name == "Stressed")) ? 1.0f : 0.0f,
            HasHappyTag = last7Days.Any(e => e.MoodEntryTags.Any(t => t.MoodTag.Name == "Happy")) ? 1.0f : 0.0f,
            HasAnxiousTag = last7Days.Any(e => e.MoodEntryTags.Any(t => t.MoodTag.Name == "Anxious")) ? 1.0f : 0.0f,
            HasTiredTag = last7Days.Any(e => e.MoodEntryTags.Any(t => t.MoodTag.Name == "Tired")) ? 1.0f : 0.0f,
            WeekendFactor = (now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday) ? 1.0f : 0.0f,
            MoodTrend = CalculateMoodTrendValue(last7Days),
            SeasonalFactor = (float)now.Month / 12.0f
        };
    }

    private async Task<IDataView> PrepareTrainingDataAsync(int userId)
    {
        var entries = await _unitOfWork.MoodEntries.GetByUserIdAsync(userId);
        var trainingDataList = new List<MoodPredictionData>();

        foreach (var entry in entries.OrderBy(e => e.EntryDate))
        {
            var priorEntries = entries.Where(e => e.EntryDate < entry.EntryDate).OrderByDescending(e => e.EntryDate).ToList();
            if (priorEntries.Count < 3) continue; // Need some history

            var last7Days = priorEntries.Where(e => e.EntryDate >= entry.EntryDate.AddDays(-7)).ToList();
            var last30Days = priorEntries.Where(e => e.EntryDate >= entry.EntryDate.AddDays(-30)).ToList();

            var trainingPoint = new MoodPredictionData
            {
                DayOfWeek = (float)entry.EntryDate.DayOfWeek,
                HourOfDay = entry.CreatedAt.Hour,
                DaysSinceLastEntry = priorEntries.Any() ? (float)(entry.EntryDate - priorEntries.First().EntryDate).TotalDays : 0,
                AverageMoodLast7Days = last7Days.Any() ? (float)last7Days.Average(e => e.MoodLevel) : 5.0f,
                AverageMoodLast30Days = last30Days.Any() ? (float)last30Days.Average(e => e.MoodLevel) : 5.0f,
                HasStressTag = entry.MoodEntryTags.Any(t => t.MoodTag.Name == "Stressed") ? 1.0f : 0.0f,
                HasHappyTag = entry.MoodEntryTags.Any(t => t.MoodTag.Name == "Happy") ? 1.0f : 0.0f,
                HasAnxiousTag = entry.MoodEntryTags.Any(t => t.MoodTag.Name == "Anxious") ? 1.0f : 0.0f,
                HasTiredTag = entry.MoodEntryTags.Any(t => t.MoodTag.Name == "Tired") ? 1.0f : 0.0f,
                WeekendFactor = (entry.EntryDate.DayOfWeek == DayOfWeek.Saturday || entry.EntryDate.DayOfWeek == DayOfWeek.Sunday) ? 1.0f : 0.0f,
                MoodTrend = CalculateMoodTrendValue(last7Days),
                SeasonalFactor = (float)entry.EntryDate.Month / 12.0f,
                MoodLevel = entry.MoodLevel
            };

            trainingDataList.Add(trainingPoint);
        }

        return _mlContext.Data.LoadFromEnumerable(trainingDataList);
    }

    private async Task<MoodPredictionResult> PredictMoodAsync(MoodPredictionData data)
    {
        if (!File.Exists(_modelPath))
        {
            // Return a simple heuristic-based prediction if no trained model exists
            var heuristicPrediction = data.AverageMoodLast7Days * 0.7f + data.AverageMoodLast30Days * 0.3f;
            if (data.WeekendFactor > 0) heuristicPrediction += 0.5f;
            if (data.HasStressTag > 0) heuristicPrediction -= 1.0f;
            if (data.HasHappyTag > 0) heuristicPrediction += 0.5f;
            
            return new MoodPredictionResult { PredictedMoodLevel = Math.Max(1, Math.Min(10, heuristicPrediction)) };
        }

        var model = _mlContext.Model.Load(_modelPath, out var schema);
        var predictionEngine = _mlContext.Model.CreatePredictionEngine<MoodPredictionData, MoodPredictionResult>(model);
        
        return predictionEngine.Predict(data);
    }

    private MoodInsight GeneratePredictionInsight(MoodPredictionResult prediction, MoodPredictionData data)
    {
        var predictedMood = Math.Round(prediction.PredictedMoodLevel, 1);
        var confidence = CalculateConfidence(data);
        
        var moodDescription = predictedMood switch
        {
            >= 8 => "great",
            >= 6 => "good",
            >= 4 => "okay",
            >= 2 => "challenging",
            _ => "difficult"
        };

        var insightText = $"Based on your patterns, you might feel {moodDescription} today (predicted mood: {predictedMood}/10).";
        
        // Add contextual factors
        if (data.WeekendFactor > 0)
            insightText += " Weekend vibes might boost your mood!";
        else if (data.DayOfWeek == 1) // Monday
            insightText += " Monday blues might affect your mood.";
            
        if (data.HasStressTag > 0)
            insightText += " Consider stress management techniques.";

        return new MoodInsight
        {
            InsightText = insightText,
            Confidence = confidence,
            Category = "prediction",
            GeneratedAt = DateTime.UtcNow,
            SupportingData = new[] { $"7-day average: {data.AverageMoodLast7Days:F1}", $"30-day average: {data.AverageMoodLast30Days:F1}" }
        };
    }

    private float CalculateConfidence(MoodPredictionData data)
    {
        var confidence = 0.5f; // Base confidence
        
        // More data = higher confidence
        if (data.AverageMoodLast30Days > 0) confidence += 0.2f;
        if (data.DaysSinceLastEntry < 2) confidence += 0.1f;
        
        // Consistent patterns = higher confidence
        var moodStability = Math.Abs(data.AverageMoodLast7Days - data.AverageMoodLast30Days);
        if (moodStability < 1) confidence += 0.2f;
        
        return Math.Min(1.0f, confidence);
    }

    private PatternDetectionResult? DetectDayOfWeekPatterns(List<MoodEntry> entries)
    {
        var dayGroups = entries.GroupBy(e => e.EntryDate.DayOfWeek)
            .ToDictionary(g => g.Key, g => g.Average(e => e.MoodLevel));

        if (dayGroups.Count < 5) return null; // Need data for most days

        var mondayMood = dayGroups.GetValueOrDefault(DayOfWeek.Monday, 5.0);
        var weekendMood = (dayGroups.GetValueOrDefault(DayOfWeek.Saturday, 5.0) + 
                          dayGroups.GetValueOrDefault(DayOfWeek.Sunday, 5.0)) / 2.0;

        if (weekendMood - mondayMood > 1.5)
        {
            return new PatternDetectionResult
            {
                PatternType = "weekly_cycle",
                Description = "Your mood tends to be higher on weekends and lower on Mondays",
                Confidence = 0.8f,
                Recommendation = "Consider planning enjoyable activities for Monday evenings to ease the transition",
                DetectedAt = DateTime.UtcNow
            };
        }

        return null;
    }

    private List<PatternDetectionResult> DetectTagPatterns(List<MoodEntry> entries)
    {
        var patterns = new List<PatternDetectionResult>();
        
        // Analyze tag correlations with mood levels
        var tagMoodMap = new Dictionary<string, List<int>>();
        
        foreach (var entry in entries)
        {
            foreach (var tag in entry.MoodEntryTags)
            {
                if (!tagMoodMap.ContainsKey(tag.MoodTag.Name))
                    tagMoodMap[tag.MoodTag.Name] = new List<int>();
                tagMoodMap[tag.MoodTag.Name].Add(entry.MoodLevel);
            }
        }

        foreach (var kvp in tagMoodMap.Where(x => x.Value.Count >= 3))
        {
            var avgMoodWithTag = kvp.Value.Average();
            var overallAvg = entries.Average(e => e.MoodLevel);
            var difference = avgMoodWithTag - overallAvg;

            if (Math.Abs(difference) > 1.0)
            {
                var patternType = difference > 0 ? "positive_trigger" : "negative_trigger";
                var description = difference > 0 
                    ? $"The '{kvp.Key}' tag is associated with higher mood levels"
                    : $"The '{kvp.Key}' tag is associated with lower mood levels";
                
                var recommendation = difference > 0
                    ? $"Try to incorporate more activities that make you feel '{kvp.Key.ToLower()}'"
                    : $"Consider strategies to manage situations that make you feel '{kvp.Key.ToLower()}'";

                patterns.Add(new PatternDetectionResult
                {
                    PatternType = patternType,
                    Description = description,
                    Confidence = Math.Min(0.9f, (float)Math.Abs(difference) / 3.0f),
                    Recommendation = recommendation,
                    DetectedAt = DateTime.UtcNow
                });
            }
        }

        return patterns;
    }

    private PatternDetectionResult? DetectMoodTrends(List<MoodEntry> entries)
    {
        if (entries.Count < 14) return null;

        var recent14Days = entries.TakeLast(14).ToList();
        var first7Days = recent14Days.Take(7).Average(e => e.MoodLevel);
        var last7Days = recent14Days.Skip(7).Average(e => e.MoodLevel);
        
        var trendValue = last7Days - first7Days;

        if (Math.Abs(trendValue) > 1.0)
        {
            var trendDirection = trendValue > 0 ? "improving" : "declining";
            var description = trendValue > 0 
                ? "Your mood has been improving over the past two weeks"
                : "Your mood has been declining over the past two weeks";
            
            var recommendation = trendValue > 0
                ? "Keep up the positive momentum! Consider what's been working well for you."
                : "Consider reaching out for support or trying new self-care strategies.";

            return new PatternDetectionResult
            {
                PatternType = "mood_trend",
                Description = description,
                Confidence = Math.Min(0.9f, (float)Math.Abs(trendValue) / 3.0f),
                Recommendation = recommendation,
                DetectedAt = DateTime.UtcNow
            };
        }

        return null;
    }

    private float CalculateMoodTrendValue(List<MoodEntry> entries)
    {
        if (entries.Count < 2) return 0.0f;
        
        var orderedEntries = entries.OrderBy(e => e.EntryDate).ToList();
        var firstHalf = orderedEntries.Take(orderedEntries.Count / 2).Average(e => e.MoodLevel);
        var secondHalf = orderedEntries.Skip(orderedEntries.Count / 2).Average(e => e.MoodLevel);
        
        return (float)(secondHalf - firstHalf);
    }

    private string CalculateMoodTrend(List<MoodEntry> entries)
    {
        if (entries.Count < 3) return "stable";
        
        var trendValue = CalculateMoodTrendValue(entries);
        
        return trendValue switch
        {
            > 0.5f => "improving",
            < -0.5f => "declining",
            _ => "stable"
        };
    }

    private float CalculateVariability(IEnumerable<float> values)
    {
        var valuesList = values.ToList();
        if (valuesList.Count < 2) return 0.0f;
        
        var mean = valuesList.Average();
        var variance = valuesList.Sum(v => Math.Pow(v - mean, 2)) / valuesList.Count;
        return (float)Math.Sqrt(variance);
    }
}
