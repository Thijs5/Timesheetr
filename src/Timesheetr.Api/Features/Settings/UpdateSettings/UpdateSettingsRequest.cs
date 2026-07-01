namespace Timesheetr.Api.Features.Settings.UpdateSettings;

public record UpdateSettingsRequest(
    string TogglApiToken,
    string TempoApiToken,
    string JiraAccountId,
    string JiraBaseUrl,
    string JiraEmail,
    string JiraApiToken);
