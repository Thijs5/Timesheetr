# ADR-0008: Durable message bus for Toggl/Jira/Tempo sync

## Status
Accepted

## Context

The original sync flow ([ADR-0005](0005-retry-mechanism-and-observability.md)) ran entirely inside one HTTP request: resolve Jira issue IDs, log each entry to Tempo, then tag the Toggl entry as synced, streaming progress back over SSE. Toggl allows only ~30 API calls/hour. When that limit was hit on the Toggl-tag step, the code retried in-process up to 3 times with `Task.Delay` — but the Tempo worklog had already been created, so re-running the whole request could call `TempoService.LogWorkAsync` again and create a duplicate Jira worklog for the same Toggl entry. There was also no recovery if the app restarted while a retry was pending.

## Decision

Sync is now a durable, asynchronous pipeline built on [Wolverine](https://wolverinefx.net) (MIT-licensed):

- **`POST /api/entries/sync`** (`Features/Entries/SyncEntries/SyncEntriesEndpoint.cs`, a `WolverineFx.Http` endpoint) upserts a `SyncEntryStateEntity` row per selected entry and publishes one `SyncEntryCommand` per entry, then returns `202 Accepted` immediately.
- **`LogWorklogToJiraHandler`** consumes `SyncEntryCommand`, resolves the Jira issue and logs the Tempo worklog, then publishes `WorklogLoggedToJiraEvent`. Idempotency is a durable database check (`SyncEntryStateEntity.Stage`) before any Tempo call — a redelivered command for an entry already past this stage skips Tempo entirely and just re-announces the event. **This is what prevents the duplicate-worklog bug.**
- **`TagTogglEntryHandler`** consumes `WorklogLoggedToJiraEvent` and tags the Toggl entry as synced. On `RateLimitException` it reschedules itself via `IMessageBus.ScheduleAsync` after a delay (Toggl's `Retry-After` header, or `Messaging:TogglRetryDelayMinutes` config, default 3 minutes) — indefinitely, with no attempt cap. Re-tagging an already-tagged Toggl entry is a no-op, so redelivery here is always safe.
- **`EntryStatusBroadcastHandler`** reacts to the lifecycle events and publishes an `EntryStatusChanged` (`WebSocketMessage`), which `WolverineFx.SignalR` automatically fans out to all connected browser clients over `/hubs/sync-status`. This replaces the single-request SSE stream — updates can now arrive minutes later from a scheduled retry, possibly after the tab was reloaded, and SignalR gives reconnect handling and multi-tab fan-out that the old raw-SSE-over-fetch approach never attempted.

### Durability

- **Transport**: RabbitMQ, hosted via Aspire (`Aspire.Hosting.RabbitMQ`) with a persistent data volume — durable queues survive a broker container restart.
- **Scheduled/outbox storage**: `WolverineFx.Sqlite` + `WolverineFx.EntityFrameworkCore`, backed by the *same* SQLite database as the rest of the app (`TimesheetrDbContext`). A scheduled Toggl retry is a row in Wolverine's own durability tables in that file, so it survives an API process restart regardless of RabbitMQ.
- `SyncLogService`/`sync-log.json`/`SyncRecord` are retired — `SyncEntryStateEntity` (EF Core, colocated config per [ADR-0004](0004-entity-and-configuration-colocation.md)) is now the single source of truth for both "is this Toggl entry logged to Tempo" and the live pipeline stage the UI needs (so a page reload shows "Syncing"/"Error" instead of resetting to "Pending"). Historical `sync-log.json` data was not migrated — Toggl's own "synced" tag remains authoritative for what's already synced.

### Alternatives considered

- **MassTransit** — the more established choice for this pattern, but moved to a commercial license; ruled out on cost grounds.
- **Postgres/SQL Server for Wolverine's durability store** — initially believed necessary (SQLite appeared unsupported), but Wolverine's own docs confirmed SQLite is a fully supported durability provider, including scheduled delivery. Staying on SQLite avoids a second database/container.
- **CAP (dotnetcore/CAP)** — also free/MIT and outbox-oriented, but heavier and less naturally integrated with minimal-API-style endpoints than Wolverine.Http.

### Observability

Wolverine's own `ActivitySource` (name `"Wolverine"`) and `Meter` (name `"Wolverine:{ServiceName}"`) are registered with the existing OpenTelemetry pipeline (`.AddSource("Wolverine")` / `.AddMeter("Wolverine:backend")` in `Program.cs`, with `opts.ServiceName = "backend"` set to match), so command/event publishing, scheduling, and handler execution show up as spans in the Aspire dashboard's trace view alongside HTTP and EF Core spans, correlated by trace ID back to the originating HTTP request.

### Implementation notes

Two non-obvious runtime requirements surfaced during implementation, beyond the packages listed above:
- `WolverineFx.RuntimeCompilation` is required — without it, Wolverine fails to start with `TypeLoadMode.Dynamic ... no IAssemblyGenerator (Roslyn) is registered`, since core `WolverineFx` no longer ships the runtime compiler.
- `opts.ServiceLocationPolicy = ServiceLocationPolicy.AlwaysAllowed;` is required because `TogglService`/`TempoService`/`JiraService` are registered via `AddHttpClient<T>()`, a transient "opaque lambda factory" registration that Wolverine's default `NotAllowed` policy rejects during handler codegen.

## Consequences

- New runtime dependency: RabbitMQ (via Docker/Aspire) for local dev and deployment.
- `Features/Entries/*` endpoints deviate from [ADR-0007](0007-minimal-api-endpoint-isolation.md)'s `IEndpoint`/Scrutor convention in order to use Wolverine's transactional HTTP middleware; this is documented as an explicit exception there.
- A Toggl retry now waits indefinitely on rate-limit instead of giving up after 3 attempts — an entry can only get stuck if Toggl itself never recovers, at which point a manual reset (`ResetEntryEndpoint`) is still available once the entry leaves the actively-in-flight stages.
- Test/dev runs against this pipeline can trigger *real* Tempo/Jira/Toggl API calls if the local database has real credentials seeded (as this project's `appsettings.json` does) — worth keeping in mind (and ideally using disposable/sandbox credentials) when smoke-testing locally, since a message being durable means it can also be *durably* retried against production APIs.
