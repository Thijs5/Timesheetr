using System.Text.Json.Serialization;

namespace Timesheetr.Api.Infrastructure.Models;

public class TogglEntry
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("start")]
    public DateTimeOffset Start { get; set; }

    [JsonPropertyName("stop")]
    public DateTimeOffset? Stop { get; set; }

    [JsonPropertyName("duration")]
    public long Duration { get; set; }

    [JsonPropertyName("project_id")]
    public long? ProjectId { get; set; }

    [JsonPropertyName("workspace_id")]
    public long WorkspaceId { get; set; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];

    public bool IsRunning => Duration < 0;
}
