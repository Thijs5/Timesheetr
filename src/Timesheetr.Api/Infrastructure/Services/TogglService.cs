using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Timesheetr.Api.Infrastructure.Models;

namespace Timesheetr.Api.Infrastructure.Services;

public class TogglService
{
    private const string SyncedTag = "✅ synced-to-tempo";
    private static readonly Regex IssueKeyPattern = new(@"\[([A-Z]+-\d+)\]", RegexOptions.Compiled);
    private readonly HttpClient _http;

    public TogglService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<TimesheetEntry>> GetEntriesAsync(string apiToken, DateOnly from, DateOnly to)
    {
        SetAuth(apiToken);

        var url = $"me/time_entries" +
                  $"?start_date={from:yyyy-MM-dd}&end_date={to.AddDays(1):yyyy-MM-dd}";

        var response = await _http.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var entries = JsonSerializer.Deserialize<List<TogglEntry>>(json) ?? [];

        return entries
            .Where(e => !e.IsRunning && e.Stop.HasValue)
            .Select(Map)
            .ToList();
    }

    public async Task TagAsSyncedAsync(string apiToken, long workspaceId, long entryId)
    {
        SetAuth(apiToken);
        await EnsureTagExistsAsync(workspaceId);
        await SetTagAsync(workspaceId, entryId, SyncedTag, "add");
    }

    public async Task UntagAsSyncedAsync(string apiToken, long workspaceId, long entryId)
    {
        SetAuth(apiToken);
        await SetTagAsync(workspaceId, entryId, SyncedTag, "remove");
    }

    private async Task EnsureTagExistsAsync(long workspaceId)
    {
        var body = JsonSerializer.Serialize(new { name = SyncedTag });
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        // Returns 200 if created, 400 if already exists — both are fine
        await _http.PostAsync($"workspaces/{workspaceId}/tags", content);
    }

    private async Task SetTagAsync(long workspaceId, long entryId, string tag, string action)
    {
        var body = JsonSerializer.Serialize(new { tags = new[] { tag }, tag_action = action });
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await _http.PutAsync(
            $"workspaces/{workspaceId}/time_entries/{entryId}",
            content);

        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromHours(1);
            throw new RateLimitException(retryAfter);
        }

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Toggl tag {action} failed {(int)response.StatusCode}: {error}");
        }
    }

    private void SetAuth(string apiToken)
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiToken}:api_token"));
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
    }

    private static TimesheetEntry Map(TogglEntry e)
    {
        var description = e.Description ?? string.Empty;
        var match = IssueKeyPattern.Match(description);
        var issueKey = match.Success ? match.Groups[1].Value : string.Empty;
        var cleanDescription = IssueKeyPattern.Replace(description, "").Trim();
        var alreadySynced = e.Tags.Contains(SyncedTag);

        return new TimesheetEntry
        {
            TogglId = e.Id,
            WorkspaceId = e.WorkspaceId,
            IssueKey = issueKey,
            Description = cleanDescription,
            Date = DateOnly.FromDateTime(e.Start.LocalDateTime),
            StartTime = TimeOnly.FromDateTime(e.Start.LocalDateTime),
            DurationSeconds = (int)e.Duration,
            Status = alreadySynced ? SyncStatus.Synced : SyncStatus.Pending,
            Selected = !alreadySynced
        };
    }
}
