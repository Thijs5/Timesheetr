# Timesheetr

Syncs [Toggl Track](https://toggl.com) time entries to [Tempo](https://www.tempo.io) worklogs. Toggl entries with a `[INSP-XXXX]` Jira issue key in the description are mapped to Tempo worklogs automatically.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org)

## Running with Aspire (recommended)

Aspire starts both the backend API and the Vite frontend together and opens the Aspire dashboard.

```bash
dotnet run --project src/Timesheetr.Aspire
```

The Aspire dashboard opens automatically and shows the URLs for each service.

## Running manually

**Backend** (ASP.NET Core API on `http://localhost:5000`):

```bash
dotnet run --project src/Timesheetr.Api
```

**Frontend** (Vite dev server on `http://localhost:5173`):

```bash
cd src/Timesheetr.WebApp
npm install
npm run dev
```

Open `http://localhost:5173` in your browser.

## Configuration

API tokens are stored in a SQLite database at `data/settings.db` and can be changed at runtime via the Settings page in the UI. On first run the database is seeded from `appsettings.json` if it contains values.

| Key | Description |
|---|---|
| `TogglApiToken` | Toggl Track API token |
| `TempoApiToken` | Tempo API token |
| `JiraAccountId` | Your Jira account ID |
| `JiraBaseUrl` | Jira base URL (e.g. `https://yourorg.atlassian.net`) |
| `JiraEmail` | Email address for Jira authentication |
| `JiraApiToken` | Jira API token |

## Docker Compose

Generate a `docker-compose.yml` into a `./deploy` directory:

```bash
dotnet run --project src/Timesheetr.Aspire -- publish --output-path ./deploy
```

Then start the stack:

```bash
docker compose -f ./deploy/docker-compose.yml up
```

Settings are stored in `./deploy/data/settings.db` (created automatically on first run). Configure tokens via the Settings page in the UI — they persist across restarts via the bind-mounted `./data` volume.
