using Timesheetr.Api.Infrastructure.Models;

namespace Timesheetr.Api.Features.Entries.SyncEntries;

public record SyncEntriesRequest(List<TimesheetEntry> Entries);
