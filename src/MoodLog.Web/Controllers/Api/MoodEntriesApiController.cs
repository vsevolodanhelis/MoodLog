using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MoodLog.Application.Interfaces;
using MoodLog.Core.DTOs;
using MoodLog.Core.Events;

namespace MoodLog.Web.Controllers.Api;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class MoodEntriesApiController : ControllerBase
{
    private readonly IMoodEntryService _moodEntryService;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IMoodEntryEventPublisher _eventPublisher;

    public MoodEntriesApiController(
        IMoodEntryService moodEntryService,
        UserManager<IdentityUser> userManager,
        IMoodEntryEventPublisher eventPublisher)
    {
        _moodEntryService = moodEntryService;
        _userManager = userManager;
        _eventPublisher = eventPublisher;
    }

    /// <summary>
    /// Get all mood entries for the current user
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MoodEntryDto>>> GetMoodEntries()
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
            return Unauthorized();

        var entries = await _moodEntryService.GetByUserIdAsync(userId.Value);
        return Ok(entries);
    }

    /// <summary>
    /// Get mood entries by date range
    /// </summary>
    [HttpGet("range")]
    public async Task<ActionResult<IEnumerable<MoodEntryDto>>> GetMoodEntriesByDateRange(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
            return Unauthorized();

        var entries = await _moodEntryService.GetByDateRangeAsync(userId.Value, startDate, endDate);
        return Ok(entries);
    }

    /// <summary>
    /// Get a specific mood entry by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<MoodEntryDto>> GetMoodEntry(int id)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
            return Unauthorized();

        var entry = await _moodEntryService.GetByIdAsync(id, userId.Value);
        if (entry == null)
            return NotFound();

        return Ok(entry);
    }

    /// <summary>
    /// Create a new mood entry
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<MoodEntryDto>> CreateMoodEntry([FromBody] MoodEntryCreateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
            return Unauthorized();

        try
        {
            var entry = await _moodEntryService.CreateAsync(dto, userId.Value);
            
            // Trigger events using delegates
            _eventPublisher.OnMoodEntryCreated(entry, userId.Value);
            await _eventPublisher.OnMoodEntryCreatedAsync(entry, userId.Value);

            return CreatedAtAction(nameof(GetMoodEntry), new { id = entry.Id }, entry);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Update an existing mood entry
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<MoodEntryDto>> UpdateMoodEntry(int id, [FromBody] MoodEntryUpdateDto dto)
    {
        if (id != dto.Id)
            return BadRequest("ID mismatch");

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
            return Unauthorized();

        try
        {
            var entry = await _moodEntryService.UpdateAsync(dto, userId.Value);
            
            // Trigger event using delegate
            _eventPublisher.OnMoodEntryUpdated(entry, userId.Value);

            return Ok(entry);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Delete a mood entry
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMoodEntry(int id)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
            return Unauthorized();

        try
        {
            var success = await _moodEntryService.DeleteAsync(id, userId.Value);
            if (!success)
                return NotFound();

            // Trigger event using delegate
            _eventPublisher.OnMoodEntryDeleted(id, userId.Value);

            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// Get mood statistics for the current user
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<object>> GetMoodStatistics([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
            return Unauthorized();

        var entries = startDate.HasValue && endDate.HasValue
            ? await _moodEntryService.GetByDateRangeAsync(userId.Value, startDate.Value, endDate.Value)
            : await _moodEntryService.GetByUserIdAsync(userId.Value);

        var entriesList = entries.ToList();
        var averageMood = await _moodEntryService.GetAverageMoodAsync(userId.Value, startDate, endDate);

        var statistics = new
        {
            TotalEntries = entriesList.Count,
            AverageMood = averageMood,
            HighestMood = entriesList.Any() ? entriesList.Max(e => e.MoodLevel) : 0,
            LowestMood = entriesList.Any() ? entriesList.Min(e => e.MoodLevel) : 0,
            MoodDistribution = entriesList.GroupBy(e => e.MoodLevel)
                .ToDictionary(g => g.Key, g => g.Count()),
            DateRange = new
            {
                StartDate = startDate ?? (entriesList.Any() ? entriesList.Min(e => e.EntryDate) : DateTime.Today),
                EndDate = endDate ?? (entriesList.Any() ? entriesList.Max(e => e.EntryDate) : DateTime.Today)
            }
        };

        return Ok(statistics);
    }

    private async Task<int?> GetCurrentUserIdAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return null;

        // Create a consistent integer ID from the string GUID
        return GetConsistentIntegerFromGuid(user.Id);
    }

    private static int GetConsistentIntegerFromGuid(string guidString)
    {
        // Parse the GUID and use a consistent method to convert to integer
        if (Guid.TryParse(guidString, out var guid))
        {
            // Use the first 4 bytes of the GUID to create a consistent integer
            var bytes = guid.ToByteArray();
            return Math.Abs(BitConverter.ToInt32(bytes, 0));
        }

        // Fallback to a consistent hash if not a valid GUID
        return Math.Abs(guidString.GetHashCode(StringComparison.Ordinal));
    }
}
