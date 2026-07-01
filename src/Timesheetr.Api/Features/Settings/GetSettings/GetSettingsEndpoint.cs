using Timesheetr.Api.Infrastructure.Services;
using Wolverine.Http;

namespace Timesheetr.Api.Features.Settings.GetSettings;

public static class GetSettingsEndpoint
{
    [WolverineGet("/api/settings")]
    public static IResult Get(SettingsService settingsService) => Results.Ok(settingsService.Settings);
}
