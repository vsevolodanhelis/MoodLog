using MoodLog.Core.DTOs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MoodLog.Application.Collections
{
    /// <summary>
    /// Custom collection demonstrating IEnumerable and IDisposable implementation.
    /// Educational focus: Custom collections, IEnumerable, IDisposable, iterator patterns.
    /// </summary>
    public class MoodEntryCollection : IEnumerable<MoodEntryDto>, IDisposable
    {
        private readonly List<MoodEntryDto> _entries;
        private readonly Dictionary<DateTime, MoodEntryDto> _dateIndex;
        private bool _disposed = false;

        public MoodEntryCollection()
        {
            _entries = new List<MoodEntryDto>();
            _dateIndex = new Dictionary<DateTime, MoodEntryDto>();
        }

        public MoodEntryCollection(IEnumerable<MoodEntryDto> entries) : this()
        {
            AddRange(entries);
        }

        public int Count => _entries.Count;
        public bool IsEmpty => _entries.Count == 0;

        // Indexer for date-based access
        public MoodEntryDto? this[DateTime date]
        {
            get
            {
                _dateIndex.TryGetValue(date.Date, out var entry);
                return entry;
            }
        }

        // Indexer for position-based access
        public MoodEntryDto this[int index]
        {
            get
            {
                if (index < 0 || index >= _entries.Count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return _entries[index];
            }
        }

        public void Add(MoodEntryDto entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            
            _entries.Add(entry);
            _dateIndex[entry.EntryDate.Date] = entry;
        }

        public void AddRange(IEnumerable<MoodEntryDto> entries)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));

            foreach (var entry in entries)
            {
                Add(entry);
            }
        }

        public bool Remove(MoodEntryDto entry)
        {
            if (entry == null) return false;

            var removed = _entries.Remove(entry);
            if (removed)
            {
                _dateIndex.Remove(entry.EntryDate.Date);
            }
            return removed;
        }

        public void Clear()
        {
            _entries.Clear();
            _dateIndex.Clear();
        }

        public bool Contains(MoodEntryDto entry)
        {
            return entry != null && _entries.Contains(entry);
        }

        public bool ContainsDate(DateTime date)
        {
            return _dateIndex.ContainsKey(date.Date);
        }

        // Custom filtering methods demonstrating LINQ integration
        public MoodEntryCollection FilterByMoodRange(int minMood, int maxMood)
        {
            var filtered = _entries.Where(e => e.MoodLevel >= minMood && e.MoodLevel <= maxMood);
            return new MoodEntryCollection(filtered);
        }

        public MoodEntryCollection FilterByDateRange(DateTime startDate, DateTime endDate)
        {
            var filtered = _entries.Where(e => e.EntryDate.Date >= startDate.Date && e.EntryDate.Date <= endDate.Date);
            return new MoodEntryCollection(filtered);
        }

        public MoodEntryCollection FilterByTags(params string[] tags)
        {
            if (tags == null || tags.Length == 0) return new MoodEntryCollection(_entries);

            var filtered = _entries.Where(e => 
                e.TagNames.Any(tag => tags.Contains(tag, StringComparer.OrdinalIgnoreCase)));
            return new MoodEntryCollection(filtered);
        }

        // Statistical methods
        public double GetAverageMood()
        {
            return _entries.Any() ? _entries.Average(e => e.MoodLevel) : 0;
        }

        public MoodEntryDto? GetHighestMoodEntry()
        {
            return _entries.OrderByDescending(e => e.MoodLevel).FirstOrDefault();
        }

        public MoodEntryDto? GetLowestMoodEntry()
        {
            return _entries.OrderBy(e => e.MoodLevel).FirstOrDefault();
        }

        public Dictionary<int, int> GetMoodDistribution()
        {
            return _entries.GroupBy(e => e.MoodLevel)
                          .ToDictionary(g => g.Key, g => g.Count());
        }

        // IEnumerable<T> implementation
        public IEnumerator<MoodEntryDto> GetEnumerator()
        {
            // Return a custom enumerator that sorts by date
            return _entries.OrderBy(e => e.EntryDate).GetEnumerator();
        }

        // Non-generic IEnumerable implementation
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        // Custom enumerator for reverse chronological order
        public IEnumerable<MoodEntryDto> GetReverseChronological()
        {
            return _entries.OrderByDescending(e => e.EntryDate);
        }

        // Custom enumerator for mood-based ordering
        public IEnumerable<MoodEntryDto> GetByMoodDescending()
        {
            return _entries.OrderByDescending(e => e.MoodLevel);
        }

        // Yield-based iterator demonstrating custom enumeration patterns
        public IEnumerable<MoodEntryDto> GetEntriesWithNotes()
        {
            foreach (var entry in _entries.OrderBy(e => e.EntryDate))
            {
                if (!string.IsNullOrWhiteSpace(entry.Notes))
                {
                    yield return entry;
                }
            }
        }

        // Yield-based iterator for mood streaks
        public IEnumerable<MoodStreak> GetMoodStreaks(int minimumMood = 7)
        {
            var sortedEntries = _entries.OrderBy(e => e.EntryDate).ToList();
            var currentStreak = new List<MoodEntryDto>();

            foreach (var entry in sortedEntries)
            {
                if (entry.MoodLevel >= minimumMood)
                {
                    currentStreak.Add(entry);
                }
                else
                {
                    if (currentStreak.Count > 0)
                    {
                        yield return new MoodStreak
                        {
                            StartDate = currentStreak.First().EntryDate,
                            EndDate = currentStreak.Last().EntryDate,
                            Length = currentStreak.Count,
                            AverageMood = currentStreak.Average(e => e.MoodLevel),
                            Entries = new List<MoodEntryDto>(currentStreak)
                        };
                        currentStreak.Clear();
                    }
                }
            }

            // Handle final streak if it exists
            if (currentStreak.Count > 0)
            {
                yield return new MoodStreak
                {
                    StartDate = currentStreak.First().EntryDate,
                    EndDate = currentStreak.Last().EntryDate,
                    Length = currentStreak.Count,
                    AverageMood = currentStreak.Average(e => e.MoodLevel),
                    Entries = new List<MoodEntryDto>(currentStreak)
                };
            }
        }

        // IDisposable implementation
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _entries.Clear();
                _dateIndex.Clear();
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    // Supporting class for mood streak analysis
    public class MoodStreak
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Length { get; set; }
        public double AverageMood { get; set; }
        public List<MoodEntryDto> Entries { get; set; } = new();
    }
}
