using System.Text.Json.Serialization;

namespace Timesheetr.Api.Infrastructure.Models;

public class TempoWorklog
{
    [JsonPropertyName("tempoWorklogId")]
    public long TempoWorklogId { get; set; }

    [JsonPropertyName("issue")]
    public TempoIssue Issue { get; set; } = new();

    [JsonPropertyName("timeSpentSeconds")]
    public int TimeSpentSeconds { get; set; }

    [JsonPropertyName("startDate")]
    public string StartDate { get; set; } = string.Empty;

    [JsonPropertyName("startTime")]
    public string StartTime { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    public DateOnly Date => DateOnly.ParseExact(StartDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

    public string FormattedDuration
    {
        get
        {
            var h = TimeSpentSeconds / 3600;
            var m = (TimeSpentSeconds % 3600) / 60;
            return h > 0 ? $"{h}h {m:D2}m" : $"{m}m";
        }
    }
}

public class TempoIssue
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("key")]
    public string? Key { get; set; }

    public string Display => Key ?? $"#{Id}";
}
