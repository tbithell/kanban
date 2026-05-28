# Quickstart: Authentication and User Onboarding

**Branch**: `001-auth-onboarding`  
**Purpose**: Get this feature running locally from a clean checkout.

---

## Prerequisites

Complete the verified onboarding checklist from the Developer Onboarding section of the
constitution before starting:

- [ ] .NET 10 SDK installed (`dotnet --version` shows `10.x.x`)
- [ ] Node.js 22+ installed (`node --version` shows `v22.x.x`)
- [ ] gitleaks installed (`gitleaks version`)
- [ ] Docker Desktop running (for Testcontainers in CI; optional for local dev)
- [ ] Snyk VS Code extension installed and authenticated
- [ ] Google Cloud project with an OAuth 2.0 Web Application credential ready

---

## One-Time Setup

### 1. Install the gitleaks pre-commit hook

```bash
# From repo root — run the provided install script
bash scripts/install-hooks.sh
# Verify: attempt to commit a fake secret and confirm it is blocked
```

### 2. Install frontend dependencies and lint-staged hook

```bash
cd src/Kanban.Web
npm install
# lint-staged is installed automatically via the postinstall script
```

### 3. Configure user secrets (run from `src/Kanban.Api/`)

```bash
dotnet user-secrets init
dotnet user-secrets set "Authentication:Google:ClientId"     "<your-client-id>"
dotnet user-secrets set "Authentication:Google:ClientSecret" "<your-client-secret>"
dotnet user-secrets set "ConnectionStrings:Kanban"           "Data Source=kanban.db"
dotnet user-secrets set "Seed:AdminEmail"                    "<your-google-email>"
```

Verify: `dotnet user-secrets list` — all four keys should appear.

### 4. Google Cloud Console setup

In your Google Cloud project → Credentials → OAuth 2.0 Client ID:

- **Application type**: Web application
- **Authorised redirect URIs**:
  - `http://localhost:5077/signin-google`
  - `https://localhost:7282/signin-google`

Copy the Client ID and Client Secret into user secrets (step 3 above).

---

## Run the Application

### Start the API

```bash
cd src/Kanban.Api
dotnet run
# DbUp runs on startup — creates schema and seeds the admin user record
# API: http://localhost:5077
# API (HTTPS): https://localhost:7282
# Scalar API docs: http://localhost:5077/scalar/v1
```

### Start the frontend

```bash
cd src/Kanban.Web
npm run dev
# Vite dev server: http://localhost:5173
```

---

## Port Map

| Service | URL |
|---------|-----|
| API (HTTP) | http://localhost:5077 |
| API (HTTPS) | https://localhost:7282 |
| Vite dev server | http://localhost:5173 |
| Scalar API docs | http://localhost:5077/scalar/v1 |
| OIDC callback | https://localhost:7282/signin-google |

---

## First Sign-In (Admin)

1. Open http://localhost:5173 in your browser
2. You will be redirected to `NotRegisteredPage` or the sign-in page — click "Sign in with Google"
3. Complete Google sign-in using the email you set as `Seed:AdminEmail`
4. You land on the signed-in landing page as Administrator
5. Verify: `GET http://localhost:5077/api/v1/auth/me` returns `"systemRole": "admin"`

---

## Issue an Invitation

1. Sign in as admin (step above)
2. Open the "Invite User" dialog in the admin UI
3. Enter an email address and submit
4. Copy the redemption link from the response
5. Verify: the link has the form `http://localhost:5173/accept/{token}`

---

## Accept an Invitation

1. Open the redemption link in an incognito window (or a different browser)
2. Click "Accept & Sign in with Google"
3. Complete Google sign-in using the exact email that was invited
4. You land on the signed-in landing page as a Standard user
5. Sign out, then sign in again via the regular flow — confirm you are recognized

---

## Run Tests

```bash
# Unit tests
dotnet test tests/unit/

# Integration tests (real SQLite)
dotnet test tests/integration/

# Frontend component tests
cd src/Kanban.Web && npm test

# E2E tests (requires both API and Web running)
npx playwright test tests/e2e/Auth/
```

---

## Verified Onboarding Checklist (Auth Feature)

- [ ] gitleaks hook blocks a test secret commit
- [ ] `dotnet user-secrets list` shows all four secrets
- [ ] API starts, migrations run, admin record seeded (check logs for "Applied 2 script(s)")
- [ ] Admin can sign in via Google and reach the landing page
- [ ] An invitation can be issued and the redemption link returned
- [ ] Invitee can accept the invitation via the redemption link
- [ ] Unregistered Google user receives "not registered" response (FR-005)
- [ ] All four test layers pass: `dotnet test` (unit + integration) + `npm test` + `npx playwright test`
