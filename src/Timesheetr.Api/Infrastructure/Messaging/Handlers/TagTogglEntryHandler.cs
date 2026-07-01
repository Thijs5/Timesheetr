using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Timesheetr.Api.Infrastructure.Data;
using Timesheetr.Api.Infrastructure.Messaging.Contracts;
using Timesheetr.Api.Infrastructure.Models;
using Timesheetr.Api.Infrastructure.Services;
using Wolverine;

namespace Timesheetr.Api.Infrastructure.Messaging.Handlers;

public class MessagingOptions
{
    public int TogglRetryDelayMinutes { get; set; } = 3;
}

public static class TagTogglEntryHandler
{
    public static async Task Handle(
        WorklogLoggedToJiraEvent message,
        TimesheetrDbContext db,
        SettingsService settingsService,
        TogglService togglService,
        IOptions<MessagingOptions> options,
        IMessageBus bus,
        ILogger<WorklogLoggedToJiraEvent> logger,
        CancellationToken ct)
    {
        var state = await db.SyncEntryStates.FindAsync([message.TogglId], ct);
        if (state is null) return;

        state.Stage = SyncStage.TogglTagPending;
        await db.SaveChangesAsync(ct);

        var settings = settingsService.Settings;
        try
        {
            await togglService.TagAsSyncedAsync(settings.TogglApiToken, message.WorkspaceId, message.TogglId);

            state.Stage = SyncStage.Synced;
            state.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);

            await bus.PublishAsync(new EntrySyncedEvent(message.TogglId));
        }
        catch (RateLimitException ex)
        {
            state.TogglRetryAttempt++;
            var delay = ex.RetryAfter > TimeSpan.Zero
                ? ex.RetryAfter
                : TimeSpan.FromMinutes(options.Value.TogglRetryDelayMinutes);

            state.Stage = SyncStage.TogglRetryScheduled;
            state.NextRetryAt = DateTimeOffset.UtcNow + delay;
            state.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);

            await bus.PublishAsync(new TogglTagUpdateFailedEvent(
                message.TogglId, message.WorkspaceId, message.TempoWorklogId, ex.Message, state.TogglRetryAttempt));

            // Re-deliver this same event after the delay so tagging is retried —
            // never re-triggers LogWorklogToJiraHandler, since that only consumes SyncEntryCommand.
            await bus.ScheduleAsync(message, delay);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to tag Toggl entry {TogglId} as synced", message.TogglId);
            state.Stage = SyncStage.Failed;
            state.ErrorMessage = ex.Message;
            state.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);

            await bus.PublishAsync(new EntrySyncFailedEvent(message.TogglId, ex.Message, LoggedToTempo: true));
        }
    }
}
