namespace Timesheetr.Api.Infrastructure.Messaging.Contracts;

public record WorklogLoggedToJiraEvent(
    long TogglId,
    long WorkspaceId,
    long TempoWorklogId,
    string IssueKey);
