using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Timesheetr.Api.Infrastructure.Services;

public class JiraService(HttpClient http)
{
    public async Task<Dictionary<string, int>> GetIssueIdsAsync(string baseUrl, string email, string apiToken, IEnumerable<string> issueKeys)
    {
        var keys = issueKeys.Distinct().ToList();
        if (keys.Count == 0) return [];

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{email}:{apiToken}"));
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        var base_ = baseUrl.TrimEnd('/');
        var tasks = keys.Select(async key =>
        {
            var response = await http.GetAsync($"{base_}/rest/api/3/issue/{key}?fields=id");
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Jira API {(int)response.StatusCode} for {key}: {body}");
            using var doc = JsonDocument.Parse(body);
            return (key, id: int.Parse(doc.RootElement.GetProperty("id").GetString()!));
        });
        var results = await Task.WhenAll(tasks);
        return results.ToDictionary(r => r.key, r => r.id);
    }
}
