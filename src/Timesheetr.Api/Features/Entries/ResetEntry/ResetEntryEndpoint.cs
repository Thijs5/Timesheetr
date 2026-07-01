using Microsoft.EntityFrameworkCore;
using Timesheetr.Api.Infrastructure.Data;
using Timesheetr.Api.Infrastructure.Services;

namespace Timesheetr.Api.Features.Entries.ResetEntry;

public class ResetEntryEndpoint(
    TogglService togglService,
    SettingsService settingsService,
    IDbContextFactory<TimesheetrDbContext> dbFactory,
    ILogger<ResetEntryEndpoint> logger) : IEndpoint
{
    public void Map(WebApplication app)
    {
        app.MapPost("/api/entries/{id:long}/reset", HandleAsync);
    }

    private async Task<IResult> HandleAsync(long id, ResetEntryRequest request, CancellationToken ct)
    {
        var settings = settingsService.Settings;
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var state = await db.SyncEntryStates.FindAsync([id], ct);

            if (state is not null && state.IsInFlight)
                return Results.Conflict(new { error = "Entry is still syncing; nothing to reset yet." });

            if (state?.TempoWorklogId is null)
                await togglService.UntagAsSyncedAsync(settings.TogglApiToken, request.WorkspaceId, id);

            if (state is not null)
            {
                db.SyncEntryStates.Remove(state);
                await db.SaveChangesAsync(ct);
            }

            return Results.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to reset entry {TogglId}", id);
            return Results.Problem(ex.Message, statusCode: 500);
        }
    }
}
