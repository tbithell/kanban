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

## Tech stack

- **Backend**: ASP.NET 10 minimal API, Dapper, SQLite (dev), DbUp migrations
- **Frontend**: React, Vite, TypeScript, Fluent UI 2, TanStack Query
- **Auth**: Google OAuth 2.0
- **Tests**: xUnit + FluentAssertions, React Testing Library, Playwright
