using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace Timesheetr.E2E;

public abstract class TimesheetrTest : PageTest
{
    protected FakeApi FakeApi { get; } = new();

    public override BrowserNewContextOptions ContextOptions() => new()
    {
        BaseURL = TestServerFixture.FrontendUrl,
    };

    [SetUp]
    public async Task ResetFakesAndSettings()
    {
        await FakeApi.ResetAsync();
        await SaveSettingsAsync(new IntegrationSettings(
            TogglApiToken: "test-toggl-token",
            TempoApiToken: "test-tempo-token",
            JiraAccountId: "test-account-id",
            JiraBaseUrl: TestServerFixture.FakeHostUrl + "/jira",
            JiraEmail: "e2e@example.com",
            JiraApiToken: "test-jira-token"));
    }

    protected static async Task SaveSettingsAsync(IntegrationSettings settings)
    {
        using var http = new HttpClient();
        var response = await http.PutAsJsonAsync(TestServerFixture.ApiUrl + "/api/settings", settings);
        response.EnsureSuccessStatusCode();
    }
}

public record IntegrationSettings(
    string TogglApiToken,
    string TempoApiToken,
    string JiraAccountId,
    string JiraBaseUrl,
    string JiraEmail,
    string JiraApiToken);

/// <summary>Mirrors the Toggl "time entry" JSON shape TogglService.GetEntriesAsync deserializes.</summary>
public record TogglEntrySeed(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("start")] DateTimeOffset Start,
    [property: JsonPropertyName("stop")] DateTimeOffset Stop,
    [property: JsonPropertyName("duration")] long Duration,
    [property: JsonPropertyName("workspace_id")] long WorkspaceId,
    [property: JsonPropertyName("tags")] string[] Tags);

public class FakeApi
{
    static readonly HttpClient Http = new() { BaseAddress = new Uri(TestServerFixture.FakeHostUrl) };

    public async Task ResetAsync()
    {
        var response = await Http.PostAsync("/__admin/reset", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task SeedTogglEntryAsync(TogglEntrySeed entry)
    {
        var response = await Http.PostAsJsonAsync("/__admin/toggl/entries", new[] { entry });
        response.EnsureSuccessStatusCode();
    }

    public async Task SeedJiraIssueAsync(string issueKey, int issueId)
    {
        var response = await Http.PostAsJsonAsync("/__admin/jira/issues", new Dictionary<string, int> { [issueKey] = issueId });
        response.EnsureSuccessStatusCode();
    }

    public async Task ForceErrorAsync(string provider, int status)
    {
        var response = await Http.PostAsJsonAsync("/__admin/force-error", new { provider, status });
        response.EnsureSuccessStatusCode();
    }
}
