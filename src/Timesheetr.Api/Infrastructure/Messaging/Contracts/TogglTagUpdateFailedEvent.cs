namespace Timesheetr.Api.Infrastructure.Messaging.Contracts;

public record TogglTagUpdateFailedEvent(
    long TogglId,
    long WorkspaceId,
    long TempoWorklogId,
    string Reason,
    int AttemptNumber);
