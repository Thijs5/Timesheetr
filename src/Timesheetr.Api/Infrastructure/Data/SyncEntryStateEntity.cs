using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Timesheetr.Api.Infrastructure.Data;

public enum SyncStage { Queued, LoggedToTempo, TogglTagPending, TogglRetryScheduled, Synced, Failed }

public class SyncEntryStateEntity
{
    public long TogglId { get; set; }
    public long WorkspaceId { get; set; }
    public string IssueKey { get; set; } = "";
    public string Description { get; set; } = "";
    public DateOnly Date { get; set; }
    public int DurationSeconds { get; set; }
    public SyncStage Stage { get; set; }
    public long? TempoWorklogId { get; set; }
    public string? ErrorMessage { get; set; }
    public int TogglRetryAttempt { get; set; }
    public DateTimeOffset? NextRetryAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public bool IsInFlight => Stage is SyncStage.Queued or SyncStage.LoggedToTempo
        or SyncStage.TogglTagPending or SyncStage.TogglRetryScheduled;
}

public class SyncEntryStateEntityConfiguration : IEntityTypeConfiguration<SyncEntryStateEntity>
{
    public void Configure(EntityTypeBuilder<SyncEntryStateEntity> builder)
    {
        builder.ToTable("SyncEntryStates");
        builder.HasKey(s => s.TogglId);
        builder.Property(s => s.TogglId).ValueGeneratedNever();
        builder.Property(s => s.Stage).HasConversion<string>();
    }
}
