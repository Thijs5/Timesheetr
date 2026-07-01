namespace Timesheetr.Api.Features.Entries.SyncEntries;

public record SyncEntriesResponse(List<long> QueuedTogglIds, List<long> SkippedTogglIds);
