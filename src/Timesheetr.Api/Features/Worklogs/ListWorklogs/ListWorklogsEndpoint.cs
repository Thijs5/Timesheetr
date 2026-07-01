using Timesheetr.Api.Infrastructure.Services;

namespace Timesheetr.Api.Features.Worklogs.ListWorklogs;

public class ListWorklogsEndpoint(
    TempoService tempoService,
    SettingsService settingsService,
    ILogger<ListWorklogsEndpoint> logger) : IEndpoint
{
    public void Map(WebApplication app)
    {
        app.MapGet("/api/worklogs", HandleAsync);
    }

    private async Task<IResult> HandleAsync(DateOnly from, DateOnly to)
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
