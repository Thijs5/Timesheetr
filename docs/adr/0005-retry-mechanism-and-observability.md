# ADR-0005: Retry Mechanism and Observability

## Status
Accepted

## Context
Timesheetr syncs Toggl entries to Tempo worklogs via external APIs. These APIs can return rate-limit errors (HTTP 429) or transient failures. Without a retry strategy, a single failed API call can abort an entire sync run and leave entries in a partially synced state.

Additionally, sync operations involve multiple steps per entry (Jira issue resolution, Tempo worklog creation, Toggl tag update). When one step succeeds and a later step fails, the system needs to record which steps completed so a retry does not create duplicate worklogs.

## Decision

> **Superseded by [ADR-0008](0008-durable-message-bus-for-sync.md).** The retry flow and state-tracking sections below describe the original in-process design and no longer reflect the code — sync is now driven by a durable message bus with its own retry and idempotency mechanism. They're kept here for history. The Observability section is unaffected and still applies.

### Retry flow for sync operations (superseded)

Each entry in a sync run is processed independently with an inner retry loop:

1. Resolve Jira issue IDs up front for all entries in the batch.
2. For each entry, attempt to log work to Tempo and tag the entry in Toggl.
3. If Tempo returns a rate-limit error (`RateLimitException`), wait the specified backoff duration and retry — up to 3 attempts per entry.
4. If Tempo succeeds but the Toggl tag step fails, the sync record is still written so a subsequent sync knows the worklog already exists and skips Tempo, targeting only the Toggl tag.
5. Progress is streamed to the client via Server-Sent Events so the UI can show per-entry status in real time.

### State tracking for partial completion (superseded)

The `SyncLogService` records each successfully synced entry by Toggl ID. On re-sync, entries already present in the log skip the Tempo step entirely. This prevents duplicate worklog creation across retries.

### Observability

Serilog is bootstrapped at startup with two sinks:
- **Console** — for development and container stdout.
- **File** (`logs/timesheetr.log`) — rolling daily, retaining 7 days.

Every sync operation is logged with structured properties. Errors during sync include the Toggl entry ID, the stage that failed, and the exception detail, making it straightforward to correlate failures across log files.

A Seq sink may be added when `Serilog:Seq:ConnectionString` and `Serilog:Seq:ApiKey` are configured — the app runs without them.

## Consequences
- (Superseded, see ADR-0008) Toggl rate limits are handled transparently; the user sees a brief wait message rather than an error.
- (Superseded, see ADR-0008) Partial sync state is preserved: a retry run resumes from where it left off rather than starting over.
- (Superseded, see ADR-0008) Duplicate Tempo worklogs are prevented even when a sync run is interrupted mid-way.
- (Superseded, see ADR-0008) The 3-attempt cap on rate-limit retries prevents indefinite blocking; if all attempts are exhausted the entry is marked as failed and the run continues with the next entry.
- File-based logging requires no external infrastructure; adding Seq is optional and non-breaking. (Still applies.)
