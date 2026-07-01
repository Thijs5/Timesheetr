using Microsoft.EntityFrameworkCore;
using Timesheetr.Api.Infrastructure.Data;
using Timesheetr.Api.Infrastructure.Messaging.Contracts;
using Wolverine;

namespace Timesheetr.Api.Infrastructure.Messaging.Handlers;

public static class EntryStatusBroadcastHandler
{
    public static Task Handle(WorklogLoggedToJiraEvent message, TimesheetrDbContext db, IMessageBus bus, CancellationToken ct)
        => BroadcastAsync(message.TogglId, db, bus, ct);

    public static Task Handle(TogglTagUpdateFailedEvent message, TimesheetrDbContext db, IMessageBus bus, CancellationToken ct)
        => BroadcastAsync(message.TogglId, db, bus, ct);

    public static Task Handle(EntrySyncedEvent message, TimesheetrDbContext db, IMessageBus bus, CancellationToken ct)
        => BroadcastAsync(message.TogglId, db, bus, ct);

    public static Task Handle(EntrySyncFailedEvent message, TimesheetrDbContext db, IMessageBus bus, CancellationToken ct)
        => BroadcastAsync(message.TogglId, db, bus, ct);

    private static async Task BroadcastAsync(long togglId, TimesheetrDbContext db, IMessageBus bus, CancellationToken ct)
    {
        var state = await db.SyncEntryStates.AsNoTracking().SingleOrDefaultAsync(s => s.TogglId == togglId, ct);
        if (state is null) return;

        var status = state.Stage switch
        {
            SyncStage.Synced => "Synced",
            SyncStage.Failed => "Error",
            _ => "Syncing"
        };

        await bus.PublishAsync(new EntryStatusChanged(
            state.TogglId, status, state.ErrorMessage, state.TempoWorklogId is not null, state.NextRetryAt));
    }
}
