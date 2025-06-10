using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MoodLog.Core.Entities;

namespace MoodLog.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<MoodEntry> MoodEntries { get; set; }
    public DbSet<MoodTag> MoodTags { get; set; }
    public DbSet<MoodEntryTag> MoodEntryTags { get; set; }
    public DbSet<Achievement> Achievements { get; set; }
    public DbSet<UserAchievement> UserAchievements { get; set; }
    public DbSet<UserStreak> UserStreaks { get; set; }
    public DbSet<UserStats> UserStats { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure MoodEntryTag as a many-to-many relationship
        modelBuilder.Entity<MoodEntryTag>()
            .HasKey(met => new { met.MoodEntryId, met.MoodTagId });

        modelBuilder.Entity<MoodEntryTag>()
            .HasOne(met => met.MoodEntry)
            .WithMany(me => me.MoodEntryTags)
            .HasForeignKey(met => met.MoodEntryId);

        modelBuilder.Entity<MoodEntryTag>()
            .HasOne(met => met.MoodTag)
            .WithMany(mt => mt.MoodEntryTags)
            .HasForeignKey(met => met.MoodTagId);

        // UserId is just an integer field, no foreign key constraint



        // Configure unique constraint for MoodTag.Name
        modelBuilder.Entity<MoodTag>()
            .HasIndex(mt => mt.Name)
            .IsUnique();

        // Configure unique constraint for MoodEntry per user per date
        modelBuilder.Entity<MoodEntry>()
            .HasIndex(me => new { me.UserId, me.EntryDate })
            .IsUnique();

        // Seed default mood tags
        SeedMoodTags(modelBuilder);
    }

    private void SeedMoodTags(ModelBuilder modelBuilder)
    {
        var seedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var defaultTags = new[]
        {
            new MoodTag { Id = 1, Name = "Happy", Description = "Feeling joyful and content", Color = "#28a745", IsSystemTag = true, CreatedAt = seedDate },
            new MoodTag { Id = 2, Name = "Sad", Description = "Feeling down or melancholy", Color = "#6c757d", IsSystemTag = true, CreatedAt = seedDate },
            new MoodTag { Id = 3, Name = "Anxious", Description = "Feeling worried or nervous", Color = "#ffc107", IsSystemTag = true, CreatedAt = seedDate },
            new MoodTag { Id = 4, Name = "Angry", Description = "Feeling frustrated or irritated", Color = "#dc3545", IsSystemTag = true, CreatedAt = seedDate },
            new MoodTag { Id = 5, Name = "Excited", Description = "Feeling energetic and enthusiastic", Color = "#fd7e14", IsSystemTag = true, CreatedAt = seedDate },
            new MoodTag { Id = 6, Name = "Calm", Description = "Feeling peaceful and relaxed", Color = "#20c997", IsSystemTag = true, CreatedAt = seedDate },
            new MoodTag { Id = 7, Name = "Stressed", Description = "Feeling overwhelmed or pressured", Color = "#e83e8c", IsSystemTag = true, CreatedAt = seedDate },
            new MoodTag { Id = 8, Name = "Tired", Description = "Feeling fatigued or low energy", Color = "#6f42c1", IsSystemTag = true, CreatedAt = seedDate }
        };

        modelBuilder.Entity<MoodTag>().HasData(defaultTags);
    }
}
