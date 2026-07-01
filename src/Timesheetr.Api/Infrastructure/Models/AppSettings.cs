namespace Timesheetr.Api.Infrastructure.Models;

public class AppSettings
{
    public string TogglApiToken { get; set; } = string.Empty;
    public string TempoApiToken { get; set; } = string.Empty;
    public string JiraAccountId { get; set; } = string.Empty;
    public string JiraBaseUrl { get; set; } = string.Empty;
    public string JiraEmail { get; set; } = string.Empty;
    public string JiraApiToken { get; set; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(TogglApiToken) &&
        !string.IsNullOrWhiteSpace(TempoApiToken) &&
        !string.IsNullOrWhiteSpace(JiraAccountId) &&
        !string.IsNullOrWhiteSpace(JiraBaseUrl) &&
        !string.IsNullOrWhiteSpace(JiraEmail) &&
        !string.IsNullOrWhiteSpace(JiraApiToken);
}
