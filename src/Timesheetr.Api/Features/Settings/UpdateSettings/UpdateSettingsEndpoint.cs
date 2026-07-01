using Timesheetr.Api.Infrastructure.Models;
using Timesheetr.Api.Infrastructure.Services;
using Wolverine.Http;

namespace Timesheetr.Api.Features.Settings.UpdateSettings;

public static class UpdateSettingsEndpoint
{
    [WolverinePut("/api/settings")]
    public static IResult Put(UpdateSettingsRequest request, SettingsService settingsService)
    {
        settingsService.Save(new AppSettings
        {
            TogglApiToken = request.TogglApiToken,
            TempoApiToken = request.TempoApiToken,
            JiraAccountId = request.JiraAccountId,
            JiraBaseUrl = request.JiraBaseUrl,
            JiraEmail = request.JiraEmail,
            JiraApiToken = request.JiraApiToken,
        });
        return Results.Ok();
    }
}
