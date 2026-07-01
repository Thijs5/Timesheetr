using Timesheetr.Api.Infrastructure.Services;

namespace Timesheetr.Api.Features.Settings.GetSettings;

public class GetSettingsEndpoint(SettingsService settingsService) : IEndpoint
{
    public void Map(WebApplication app)
    {
        app.MapGet("/api/settings", () => Results.Ok(settingsService.Settings));
    }
}
