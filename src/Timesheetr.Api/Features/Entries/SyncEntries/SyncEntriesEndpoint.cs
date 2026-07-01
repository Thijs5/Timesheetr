using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Timesheetr.Api.Infrastructure.Data;
using Timesheetr.Api.Infrastructure.Messaging.Contracts;
using Timesheetr.Api.Infrastructure.Models;
using Wolverine;
using Wolverine.Http;

namespace Timesheetr.Api.Features.Entries.SyncEntries;

public static class SyncEntriesEndpoint
{
    [WolverinePost("/api/entries/sync")]
    public static async Task<Accepted<SyncEntriesResponse>> Post(
        SyncEntriesRequest request,
        TimesheetrDbContext db,
        IMessageBus bus,
        CancellationToken ct)
    {
        var toProcess = request.Entries.Where(e => e.Selected && e.Status != SyncStatus.Synced).ToList();
        var skipped = toProcess.Where(e => !e.HasValidIssueKey).ToList();
        var toSync = toProcess.Except(skipped).ToList();

        var togglIds = toSync.Select(e => e.TogglId).ToList();
        var existingStates = await db.SyncEntryStates
            .Where(s => togglIds.Contains(s.TogglId))
            .ToDictionaryAsync(s => s.TogglId, ct);

        var queued = new List<TimesheetEntry>();
        foreach (var entry in toSync)
        {
            if (existingStates.TryGetValue(entry.TogglId, out var existing))
            {
                if (existing.IsInFlight) continue;
                existing.Stage = SyncStage.Queued;
                existing.UpdatedAt = DateTimeOffset.UtcNow;
            }
            else
            {
                db.SyncEntryStates.Add(new SyncEntryStateEntity
                {
                    TogglId = entry.TogglId,
                    WorkspaceId = entry.WorkspaceId,
                    IssueKey = entry.IssueKey,
                    Description = entry.Description,
                    Date = entry.Date,
                    DurationSeconds = entry.DurationSeconds,
                    Stage = SyncStage.Queued,
                    UpdatedAt = DateTimeOffset.UtcNow
                });
            }

            queued.Add(entry);
        }

        await db.SaveChangesAsync(ct);

        foreach (var entry in queued)
        {
            await bus.PublishAsync(new SyncEntryCommand(
                entry.TogglId, entry.WorkspaceId, entry.IssueKey, entry.Description,
                entry.Date, entry.StartTime, entry.DurationSeconds));
        }

        var response = new SyncEntriesResponse(
            queued.Select(e => e.TogglId).ToList(),
            skipped.Select(e => e.TogglId).ToList());

        return TypedResults.Accepted((string?)null, response);
    }
}
