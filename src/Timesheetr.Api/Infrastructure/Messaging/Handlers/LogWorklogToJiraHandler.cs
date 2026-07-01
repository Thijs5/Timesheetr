using Microsoft.EntityFrameworkCore;
using Timesheetr.Api.Infrastructure.Data;
using Timesheetr.Api.Infrastructure.Messaging.Contracts;
using Timesheetr.Api.Infrastructure.Models;
using Timesheetr.Api.Infrastructure.Services;
using Wolverine;

namespace Timesheetr.Api.Infrastructure.Messaging.Handlers;

public static class LogWorklogToJiraHandler
{
    public static async Task Handle(
        SyncEntryCommand command,
        TimesheetrDbContext db,
        SettingsService settingsService,
        JiraService jiraService,
        TempoService tempoService,
        IMessageBus bus,
        ILogger<SyncEntryCommand> logger,
        CancellationToken ct)
    {
        var state = await db.SyncEntryStates.FindAsync([command.TogglId], ct);
        if (state is null)
        {
            state = new SyncEntryStateEntity
            {
                TogglId = command.TogglId,
                WorkspaceId = command.WorkspaceId,
                IssueKey = command.IssueKey,
                Description = command.Description,
                Date = command.Date,
                DurationSeconds = command.DurationSeconds,
                Stage = SyncStage.Queued
            };
            db.SyncEntryStates.Add(state);
        }

        // Idempotency guard: if this entry already made it past the Tempo step
        // (e.g. redelivered command, or Toggl tagging is still in progress/retrying),
        // never call Tempo again — just re-announce the event.
        if (state.Stage != SyncStage.Queued)
        {
            if (state.TempoWorklogId is not null)
                await bus.PublishAsync(new WorklogLoggedToJiraEvent(state.TogglId, state.WorkspaceId, state.TempoWorklogId.Value, state.IssueKey));
            return;
        }

        var settings = settingsService.Settings;
        try
        {
            var issueIds = await jiraService.GetIssueIdsAsync(
                settings.JiraBaseUrl, settings.JiraEmail, settings.JiraApiToken, [command.IssueKey]);

            if (!issueIds.TryGetValue(command.IssueKey, out var issueId))
                throw new InvalidOperationException($"Issue key {command.IssueKey} not found in Jira.");

            var entry = new TimesheetEntry
            {
                TogglId = command.TogglId,
                WorkspaceId = command.WorkspaceId,
                IssueKey = command.IssueKey,
                Description = command.Description,
                Date = command.Date,
                StartTime = command.StartTime,
                DurationSeconds = command.DurationSeconds
            };

            var tempoWorklogId = await tempoService.LogWorkAsync(settings.TempoApiToken, settings.JiraAccountId, entry, issueId);

            state.Stage = SyncStage.LoggedToTempo;
            state.TempoWorklogId = tempoWorklogId;
            state.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);

            await bus.PublishAsync(new WorklogLoggedToJiraEvent(state.TogglId, state.WorkspaceId, tempoWorklogId, state.IssueKey));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to log Toggl entry {TogglId} to Tempo", command.TogglId);
            state.Stage = SyncStage.Failed;
            state.ErrorMessage = ex.Message;
            state.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);

            await bus.PublishAsync(new EntrySyncFailedEvent(command.TogglId, ex.Message, LoggedToTempo: false));
        }
    }
}
