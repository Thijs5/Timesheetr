namespace Timesheetr.Api.Infrastructure.Messaging.Contracts;

public record SyncEntryCommand(
    long TogglId,
    long WorkspaceId,
    string IssueKey,
    string Description,
    DateOnly Date,
    TimeOnly StartTime,
    int DurationSeconds);
