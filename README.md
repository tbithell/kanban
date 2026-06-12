# Kanban

A board-based task management app built with ASP.NET 10 and React.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 22+](https://nodejs.org)
- [gitleaks](https://github.com/gitleaks/gitleaks) — `brew install gitleaks`
- A Google Cloud project with an OAuth 2.0 Web Application credential
  - Authorized redirect URI: `https://localhost:7282/signin-google`

## First-time setup

**1. Install the gitleaks pre-commit hook**

```bash
./install-hooks.sh
```

**2. Install frontend dependencies**

```bash
cd src/Kanban.Web && npm install
```

**3. Configure secrets** (run from `src/Kanban.Api/`)

```bash
dotnet user-secrets init
dotnet user-secrets set "Authentication:Google:ClientId"     "<your-client-id>"
dotnet user-secrets set "Authentication:Google:ClientSecret" "<your-client-secret>"
dotnet user-secrets set "ConnectionStrings:Kanban"           "Data Source=kanban.db"
dotnet user-secrets set "Seed:AdminEmail"                    "<your-google-email>"
```

## Running locally

Start the API (runs DB migrations on first launch):

```bash
dotnet run --project src/Kanban.Api
```

Start the frontend dev server (separate terminal):

```bash
cd src/Kanban.Web && npm run dev
```

| Service | URL |
|---------|-----|
| Frontend | http://localhost:5173 |
| API (HTTP) | http://localhost:5077 |
| API (HTTPS) | https://localhost:7282 |
| API docs (Scalar) | http://localhost:5077/scalar/v1 |

Sign in at http://localhost:5173 using the Google account matching `Seed:AdminEmail`.

## Running tests

```bash
dotnet test tests/unit/
dotnet test tests/integration/

cd src/Kanban.Web && npm test

# E2E — requires both API and frontend running
npx playwright test
```

## Running with Docker Desktop

A single `docker compose up` starts the API and the React frontend together.

**One-time Google Cloud Console setup** — add an authorized redirect URI for the Docker origin:

```
http://localhost:3000/signin-google
```

**Steps**

```bash
# 1. Copy the example env file and fill in your secrets
cp .env.docker.example .env
# Edit .env — set GOOGLE_CLIENT_ID, GOOGLE_CLIENT_SECRET, ADMIN_EMAIL

# 2. Start both containers (builds images on first run, ~2 min)
docker compose up --build

# 3. Open the app
open http://localhost:3000
```

| Service | URL |
|---------|-----|
| Frontend | http://localhost:3000 |
| API (via nginx proxy) | http://localhost:3000/api |
| Health (liveness) | http://localhost:3000/health/live |
| Health (readiness) | http://localhost:3000/health/ready |

The SQLite database persists in a Docker volume (`kanban-data`). To reset to a clean state:

```bash
docker compose down -v   # removes the volume — all data is deleted
docker compose up
```

> **Note:** The chiseled API image (`mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled`) is
> non-root and ~30 MB. SQLite is fine for single-user demo; swap `ConnectionStrings__Kanban`
> for a Postgres connection string and rebuild to use a real database.

## Tech stack

- **Backend**: ASP.NET 10 minimal API, Dapper, SQLite (dev), DbUp migrations
- **Frontend**: React, Vite, TypeScript, Fluent UI 2, TanStack Query
- **Auth**: Google OAuth 2.0
- **Tests**: xUnit + FluentAssertions, React Testing Library, Playwright
