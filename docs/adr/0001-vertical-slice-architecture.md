# ADR-0001: Vertical Slice Architecture

## Status
Accepted

## Context
Timesheetr connects to multiple external APIs (Toggl, Tempo, Jira) and exposes a React frontend over an ASP.NET Core backend. Features tend to span the full stack — a UI page, an API endpoint, and domain logic are typically developed and shipped together. A traditional layered architecture (Controllers → Services → Models) means each feature is scattered across multiple horizontal layers, creating coupling between unrelated features through shared service classes.

## Decision
Organise code by feature (vertical slice) rather than by layer. Each feature owns everything it needs end-to-end: the API endpoint(s), request/response types, and domain logic. Slices do not share code with each other except through explicit, well-defined abstractions in shared infrastructure.

Backend folder structure:

```
Timesheetr/
  Features/
    Entries/
      GetEntries/
        GetEntriesEndpoint.cs     ← GET /api/entries
        GetEntriesResponse.cs     ← response model
      SyncEntries/
        SyncEntriesEndpoint.cs    ← POST /api/entries/sync
        SyncEntriesRequest.cs
    Worklogs/
      ListWorklogs/
        ListWorklogsEndpoint.cs   ← GET /api/worklogs
        ListWorklogsResponse.cs
    Settings/
      GetSettingsEndpoint.cs      ← GET /api/settings
      UpdateSettingsEndpoint.cs   ← PUT /api/settings
  Infrastructure/                 ← cross-cutting concerns only
    Data/
      TimesheetrDbContext.cs
```

Frontend folder structure mirrors the URL structure (see ADR-0002 for component conventions):

```
web/src/
  pages/
    SyncPage.tsx       ← /sync
    TempoPage.tsx      ← /tempo
    SettingsPage.tsx   ← /settings
```

### URL mirroring
The `Features/` folder structure mirrors the API route structure. An endpoint at `/api/entries/sync` lives at `Features/Entries/SyncEntries/`. This makes it trivial to locate the code for any route in the application.

### Naming conventions
| File type | Convention | Example |
|-----------|-----------|---------|
| API endpoint class | `{Action}{Feature}Endpoint.cs` | `GetEntriesEndpoint.cs` |
| Response DTO | `{Action}{Feature}Response.cs` | `GetEntriesResponse.cs` |
| Request DTO | `{Action}{Feature}Request.cs` | `SyncEntriesRequest.cs` |
| React page | `{Feature}Page.tsx` | `SyncPage.tsx` |

## Consequences
- New features are added by creating a new folder under `Features/` — no existing code needs to change.
- Features are easy to delete: remove the folder.
- Code for a feature is co-located and easy to navigate.
- Shared infrastructure in `Infrastructure/` must be kept minimal; resist the urge to extract abstractions prematurely.
- Some duplication between slices is acceptable and preferable to premature generalisation.
