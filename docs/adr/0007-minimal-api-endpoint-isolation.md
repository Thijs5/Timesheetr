# ADR-0007: Minimal API Endpoint Isolation and DTO Naming

## Status
Accepted

## Context
Timesheetr's backend exposes HTTP endpoints via ASP.NET Core. Without structure, all endpoint registrations would accumulate in `Program.cs`, making it hard to navigate and violating the vertical slice principle: code for a feature should live with that feature, not in a central file.

Additionally, anonymous or generically named DTOs (e.g. a record simply called `Response`) are ambiguous when encountered outside their immediate context — in error messages, stack traces, or IDE search results they give no indication of which feature or action they belong to.

## Decision

### Endpoint isolation via `IEndpoint`

Each endpoint or group of closely related endpoints lives in its own class implementing `IEndpoint`:

```csharp
public interface IEndpoint
{
    void Map(WebApplication app);
}
```

Implementations are discovered automatically at startup using Scrutor assembly scanning:

```csharp
builder.Services.Scan(scan => scan
    .FromAssemblyOf<Program>()
    .AddClasses(classes => classes.AssignableTo<IEndpoint>())
    .AsImplementedInterfaces()
    .WithTransientLifetime());
```

`Program.cs` resolves all registrations and calls `Map` on each:

```csharp
foreach (var endpoint in app.Services.GetServices<IEndpoint>())
    endpoint.Map(app);
```

Endpoint classes are placed in the action folder of the feature they belong to, keeping all code for a slice together:

```
Features/
  Entries/
    GetEntries/
      GetEntriesEndpoint.cs
      GetEntriesResponse.cs
    SyncEntries/
      SyncEntriesEndpoint.cs
      SyncEntriesRequest.cs
  Worklogs/
    ListWorklogs/
      ListWorklogsEndpoint.cs
      ListWorklogsResponse.cs
  Settings/
    GetSettingsEndpoint.cs
    UpdateSettingsEndpoint.cs
```

### DTO naming convention

DTOs (request and response records) are named after the action they belong to:

| Type | Convention | Example |
|------|-----------|---------|
| Response DTO | `{Action}{Feature}Response` | `GetEntriesResponse` |
| Request DTO | `{Action}{Feature}Request` | `SyncEntriesRequest` |

One file per type, named after the type it contains:

| Type | File |
|------|------|
| `GetEntriesResponse` | `GetEntriesResponse.cs` |
| `SyncEntriesRequest` | `SyncEntriesRequest.cs` |

Generic names like `Response` and `Request` are not used.

## Consequences
- Adding a new endpoint means creating a new folder + class; no existing file changes.
- Any endpoint class can be deleted along with its folder without touching `Program.cs`.
- DTO type names are unambiguous in any context — a `GetEntriesResponse` is always identifiable without opening the file.
- Scrutor adds a compile-time-safe assembly scan; missing or mis-named classes are simply not registered.

## Exception: `Features/Entries/*`

As of [ADR-0008](0008-durable-message-bus-for-sync.md), the `SyncEntries`, `GetEntries`, and `ResetEntry` endpoints are implemented as `WolverineFx.Http` attribute-routed endpoints (`[WolverinePost]`/`[WolverineGet]`) instead of `IEndpoint`/Scrutor. This is because `SyncEntriesEndpoint` needs to publish bus messages atomically with its database writes via Wolverine's EF Core transactional middleware, which requires Wolverine's own endpoint pipeline. The DTO naming convention above still applies to these endpoints. All other features keep the `IEndpoint`/Scrutor convention.
