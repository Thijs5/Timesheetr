# ADR-0009: End-to-end test strategy — fake external services, .NET Playwright

## Status
Accepted

## Context

The project had no automated tests. ADR-0008 explicitly flagged the risk of "smoke-testing locally" against the real Toggl/Tempo/Jira APIs, since the durable message bus means a bug can be *durably* retried against production third-party accounts. We need coverage of the full user-facing feature set (Integrations settings, the Toggl→Tempo/Jira sync pipeline including its async retry/error paths, and the worklog view) without ever calling the real Toggl, Tempo, or Jira APIs, and without maintaining a large, slow, or duplicative test suite.

## Decision

- **Playwright, not a UI unit-test framework.** Tests drive the real Vite dev server and the real ASP.NET/Wolverine backend as black boxes through the browser — the same code paths a user hits, including the SignalR-pushed status updates and the durable Wolverine handlers, are exercised for real. No component is mocked inside the app.
- **`Microsoft.Playwright.NUnit` (.NET), not the Node/TypeScript `@playwright/test` package.** Keeps the whole repo on one toolchain/SDK instead of adding a second (npm-based) test runner alongside the existing .NET solution.
- **Only Toggl, Tempo, and Jira are faked** — via one small ASP.NET Minimal API host (`tests/Timesheetr.FakeExternalApis`) that the real backend is pointed at for the duration of the test run (`ExternalServices:TogglBaseUrl` / `TempoBaseUrl` config, and the Jira base URL already being a per-user Settings value). Everything else in the stack — frontend, backend, SQLite, Wolverine, SignalR — is the real thing.
- **No test ever talks to the real third-party APIs.** This directly closes the gap ADR-0008 called out: a bug that would previously have to be "smoke-tested" against production Toggl/Tempo/Jira accounts is now caught by tests that can force error/rate-limit responses on demand via the fake host's admin endpoints, with no risk to real data or real API quotas.
- **Minimize test *files*, not test *coverage*.** Each spec/test class covers a full user journey end-to-end (e.g. "load entries → sync → see it land in Tempo → reset it") rather than one file per component or per assertion, so the suite stays small while still exercising every page and the async success/failure paths of the sync pipeline.

### Alternatives considered

- **Browser-level request mocking (`page.route`)** — faster to write, but only intercepts what the *frontend* calls; it can't verify the backend's own outbound calls to Toggl/Tempo/Jira (tagging, worklog creation, issue lookup), which is most of what actually needs coverage here.
- **WireMock.NET / WireMock standalone** — a viable alternative to a hand-rolled fake host, but a purpose-built Minimal API host is a few dozen lines given how small the Toggl/Tempo/Jira surface actually is, and keeps the fake's behavior (e.g. forced rate-limits) expressed directly in C# rather than JSON stub mappings.
- **Real sandbox/disposable Toggl/Tempo/Jira accounts** — rejected per ADR-0008's own caution: durable retries against real third-party accounts are exactly the risk being designed away here.

## Consequences

- New test-only projects: `tests/Timesheetr.FakeExternalApis` (fake Toggl/Tempo/Jira host) and `tests/Timesheetr.E2E` (`Microsoft.Playwright.NUnit` tests), both added to `Timesheetr.slnx` but never shipped.
- `TogglService`/`TempoService` base URLs had to become configuration (`ExternalServices:TogglBaseUrl`/`TempoBaseUrl`) instead of hardcoded literals so tests can redirect them; `JiraService` needed no change since its base URL was already a runtime Settings value.
- **New features are expected to come with Playwright coverage under `tests/Timesheetr.E2E`** as part of the same change, following the existing "one test class per user journey" pattern rather than adding new files per component. If a feature can't reasonably be driven through the fake Toggl/Tempo/Jira host, that's a signal to extend the fake host's admin API rather than skip coverage.
- Running the suite requires three local processes (fake host, API, Vite dev server); `tests/Timesheetr.E2E`'s assembly-level fixture starts and tears these down automatically, so `dotnet test` remains the only command a contributor needs to run.
