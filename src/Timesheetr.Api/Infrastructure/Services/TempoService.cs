using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Timesheetr.Api.Infrastructure.Models;

namespace Timesheetr.Api.Infrastructure.Services;

public class TempoService(HttpClient http, ILogger<TempoService> logger)
{
    public async Task<long> LogWorkAsync(string apiToken, string accountId, TimesheetEntry entry, int issueId)
    {
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);

        var body = new
        {
            issueId,
            timeSpentSeconds = entry.DurationSeconds,
            startDate = entry.Date.ToString("yyyy-MM-dd"),
            startTime = entry.StartTime.ToString("HH:mm:ss"),
            description = entry.Description,
            authorAccountId = accountId
        };

        var json = JsonSerializer.Serialize(body);
        logger.LogInformation("Posting worklog to Tempo: {Json}", json);

        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await http.PostAsync("https://api.tempo.io/4/worklogs", content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Tempo API error {Status} for {IssueKey}: {Body}",
                (int)response.StatusCode, entry.IssueKey, responseBody);
            throw new Exception($"Tempo {(int)response.StatusCode}: {responseBody}");
        }

        logger.LogInformation("Worklog created for {IssueKey}: {Body}", entry.IssueKey, responseBody);

        using var doc = System.Text.Json.JsonDocument.Parse(responseBody);
        return doc.RootElement.GetProperty("tempoWorklogId").GetInt64();
    }

    public async Task<List<TempoWorklog>> GetWorklogsAsync(string apiToken, string accountId, DateOnly from, DateOnly to)
    {
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);

        var results = new List<TempoWorklog>();
        var url = $"https://api.tempo.io/4/worklogs?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}&authorAccountId={accountId}&limit=50";

        while (url != null)
        {
            var response = await http.GetAsync(url);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Tempo {(int)response.StatusCode}: {body}");

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            var items = root.GetProperty("results").EnumerateArray().ToList();
            if (items.Count > 0)
                logger.LogInformation("Tempo worklog sample: {Sample}", items[0].GetRawText());

            foreach (var item in items)
                results.Add(JsonSerializer.Deserialize<TempoWorklog>(item.GetRawText())!);

            url = root.TryGetProperty("metadata", out var meta) &&
                  meta.TryGetProperty("next", out var next) &&
                  next.ValueKind == JsonValueKind.String
                ? next.GetString()
                : null;
        }

        return results;
    }
}
