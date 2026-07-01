using Microsoft.EntityFrameworkCore;
using Timesheetr.Api.Infrastructure.Data;
using Timesheetr.Api.Infrastructure.Models;
using Timesheetr.Api.Infrastructure.Services;
using Wolverine.Http;

namespace Timesheetr.Api.Features.Entries.GetEntries;

public static class GetEntriesEndpoint
{
    public sealed class LogCategory;

    [WolverineGet("/api/entries")]
    public static async Task<IResult> Get(
        DateOnly from,
        DateOnly to,
        TogglService togglService,
        SettingsService settingsService,
        IDbContextFactory<TimesheetrDbContext> dbFactory,
        ILogger<LogCategory> logger,
        CancellationToken ct)
    {
        var settings = settingsService.Settings;
        if (!settings.IsConfigured)
            return Results.BadRequest(new { error = "API tokens not configured. Please visit settings." });

        try
        {
            var entries = await togglService.GetEntriesAsync(settings.TogglApiToken, from, to);

            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var states = await db.SyncEntryStates
                .Where(s => entries.Select(e => e.TogglId).Contains(s.TogglId))
                .ToDictionaryAsync(s => s.TogglId, ct);

            foreach (var entry in entries)
            {
                if (!states.TryGetValue(entry.TogglId, out var state)) continue;

                entry.LoggedToTempo = state.TempoWorklogId is not null;
                entry.TogglRetryAttempt = state.TogglRetryAttempt;
                entry.NextRetryAt = state.NextRetryAt;
                entry.Status = state.Stage switch
                {
                    SyncStage.Synced => SyncStatus.Synced,
                    SyncStage.Failed => SyncStatus.Error,
                    _ => SyncStatus.Syncing
                };
                if (state.Stage == SyncStage.Failed)
                    entry.ErrorMessage = state.ErrorMessage;
            }

            return Results.Ok(entries);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load Toggl entries");
            return Results.Problem(ex.Message, statusCode: 500);
        }
    }
}
