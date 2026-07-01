namespace Timesheetr.Api.Infrastructure.Models;

public enum SyncStatus { Pending, Syncing, Synced, Error }

public class TimesheetEntry
{
    public long TogglId { get; set; }
    public long WorkspaceId { get; set; }
    public bool Selected { get; set; } = true;
    public string IssueKey { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public int DurationSeconds { get; set; }
    public SyncStatus Status { get; set; } = SyncStatus.Pending;
    public string? ErrorMessage { get; set; }
    public bool LoggedToTempo { get; set; }
    public int TogglRetryAttempt { get; set; }
    public DateTimeOffset? NextRetryAt { get; set; }

    public bool HasValidIssueKey => !string.IsNullOrWhiteSpace(IssueKey)
        && System.Text.RegularExpressions.Regex.IsMatch(IssueKey, @"^[A-Z]+-\d+$");

    public string FormattedDuration
    {
        get
        {
            var h = DurationSeconds / 3600;
            var m = (DurationSeconds % 3600) / 60;
            return h > 0 ? $"{h}h {m:D2}m" : $"{m}m";
        }
    }
}
