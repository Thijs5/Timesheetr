using Timesheetr.Api.Infrastructure.Services;
using Wolverine.Http;

namespace Timesheetr.Api.Features.Worklogs.ListWorklogs;

public static class ListWorklogsEndpoint
{
    public sealed class LogCategory;

    [WolverineGet("/api/worklogs")]
    public static async Task<IResult> Get(
        DateOnly from,
        DateOnly to,
        TempoService tempoService,
        SettingsService settingsService,
        ILogger<LogCategory> logger)
    {
        var settings = settingsService.Settings;
        if (!settings.IsConfigured)
            return Results.BadRequest(new { error = "API tokens not configured." });

        try
        {
            var worklogs = await tempoService.GetWorklogsAsync(settings.TempoApiToken, settings.JiraAccountId, from, to);
            return Results.Ok(worklogs);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load Tempo worklogs");
            return Results.Problem(ex.Message, statusCode: 500);
        }
    }
}
