using Timesheetr.Api.Infrastructure.Models;
using Timesheetr.Api.Infrastructure.Services;

namespace Timesheetr.Api.Features.Settings.UpdateSettings;

public class UpdateSettingsEndpoint(SettingsService settingsService) : IEndpoint
{
    public void Map(WebApplication app)
    {
        app.MapPut("/api/settings", Handle);
    }

    private IResult Handle(UpdateSettingsRequest request)
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
