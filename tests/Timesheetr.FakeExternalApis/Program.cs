using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(opts =>
{
    opts.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

var app = builder.Build();

var state = new FakeState();

// ---- Admin (test control) ----

app.MapPost("/__admin/reset", () =>
{
    state.Reset();
    return Results.Ok();
});

app.MapPost("/__admin/toggl/entries", (List<JsonElement> entries) =>
{
    state.TogglEntries = entries;
    return Results.Ok();
});

app.MapPost("/__admin/jira/issues", (Dictionary<string, int> issues) =>
{
    foreach (var (key, id) in issues) state.JiraIssueIds[key] = id;
    return Results.Ok();
});

app.MapPost("/__admin/force-error", (ForceErrorRequest req) =>
{
    state.ForcedErrors[req.Provider] = req.Status;
    return Results.Ok();
});

app.MapGet("/__admin/health", () => Results.Ok());

// ---- Toggl (mirrors https://api.track.toggl.com/api/v9/) ----

app.MapGet("/toggl/me/time_entries", () => Results.Json(state.TogglEntries));

app.MapPost("/toggl/workspaces/{workspaceId}/tags", () => Results.Ok());

app.MapPut("/toggl/workspaces/{workspaceId}/time_entries/{entryId}", (long entryId) =>
{
    if (state.ForcedErrors.TryRemove("toggl", out var status) && status == 429)
    {
        return Results.StatusCode(429);
    }

    state.TaggedEntryIds.Add(entryId);
    return Results.Ok();
});

// ---- Tempo (mirrors https://api.tempo.io/4/) ----

app.MapPost("/tempo/worklogs", (JsonElement body) =>
{
    if (state.ForcedErrors.TryRemove("tempo", out var status))
    {
        return Results.StatusCode(status);
    }

    var worklogId = state.NextTempoWorklogId++;
    var worklog = new Dictionary<string, object?>
    {
        ["tempoWorklogId"] = worklogId,
        ["issue"] = new { id = body.GetProperty("issueId").GetInt32() },
        ["timeSpentSeconds"] = body.GetProperty("timeSpentSeconds").GetInt32(),
        ["startDate"] = body.GetProperty("startDate").GetString(),
        ["startTime"] = body.GetProperty("startTime").GetString(),
        ["description"] = body.TryGetProperty("description", out var d) ? d.GetString() : null,
    };
    state.TempoWorklogs.Add(worklog);

    return Results.Json(worklog);
});

app.MapGet("/tempo/worklogs", () => Results.Json(new
{
    results = state.TempoWorklogs,
    metadata = new { }
}));

// ---- Jira (mirrors https://<site>.atlassian.net) ----

app.MapGet("/jira/rest/api/3/issue/{key}", (string key) =>
{
    if (state.ForcedErrors.TryRemove("jira", out var status))
    {
        return Results.StatusCode(status);
    }

    if (!state.JiraIssueIds.TryGetValue(key, out var id))
    {
        return Results.NotFound();
    }

    return Results.Json(new { id = id.ToString() });
});

app.Run();

class FakeState
{
    public List<JsonElement> TogglEntries { get; set; } = [];
    public HashSet<long> TaggedEntryIds { get; } = [];
    public List<Dictionary<string, object?>> TempoWorklogs { get; set; } = [];
    public int NextTempoWorklogId { get; set; } = 1;
    public ConcurrentDictionary<string, int> JiraIssueIds { get; } = new();
    public ConcurrentDictionary<string, int> ForcedErrors { get; } = new();

    public void Reset()
    {
        TogglEntries = [];
        TaggedEntryIds.Clear();
        TempoWorklogs = [];
        NextTempoWorklogId = 1;
        JiraIssueIds.Clear();
        ForcedErrors.Clear();
    }
}

record ForceErrorRequest(
    [property: JsonPropertyName("provider")] string Provider,
    [property: JsonPropertyName("status")] int Status);
