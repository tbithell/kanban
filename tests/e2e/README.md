# Kanban — Playwright E2E Tests

Two modes are supported. Pick one based on your goal:

| Mode | Command | When to use |
|------|---------|-------------|
| **CI / bypass** | `npm run test:e2e` | Automated runs, no Google sign-in required |
| **Full demo** | `npm run test:e2e:setup` then `npm run test:e2e:demo` | Real Google OAuth, headed browser, one-time setup |

---

## Prerequisites (both modes)

**The API must be running before you start any test.** Playwright cannot start it automatically because it requires `dotnet user-secrets` that the test runner cannot inject.

```bash
# Terminal 1 — start the API (from repo root)
cd src/Kanban.Api && dotnet run
# Wait for: "DbUp: Applied 2 script(s)" and "Now listening on: http://localhost:5077"
```

Vite **does not** need to be started manually — Playwright starts it automatically via `webServer` config. If you already have it running, Playwright reuses it.

---

## Mode 1 — CI / bypass (no Google required)

Uses `GET /api/v1/dev/authenticate` to issue real auth cookies without touching Google OAuth. This endpoint only exists when `ASPNETCORE_ENVIRONMENT=Development`.

```bash
cd tests/e2e
npm run test:e2e
```

That's it. All auth states (admin, invitee, unregistered) are set up automatically before any test runs.

**What runs:**
- US1 scenarios 1–4: admin sign-in, session recognition, not-registered redirect, unauthenticated redirect
- US3 scenarios 1,3–6: valid acceptance, expired/consumed/mismatch/fabricated token errors
- US2 scenarios 1–5 + US3-2: scaffolded (`test.fixme`) — require the board page

---

## Mode 2 — Full demo (real Google OAuth)

### Prerequisite — Google Chrome must be installed

The setup test uses your real installed Chrome (`channel: 'chrome'` in `playwright.config.ts`). Playwright's bundled Chromium triggers Google's "This browser or app may not be secure" block because it carries automation detection flags. Real Chrome does not.

Download Chrome at https://www.google.com/chrome if it is not already installed. The bypass CI mode is unaffected — it skips the Google sign-in entirely.

### Step 1 — One-time auth setup

Run this once. It opens a headed Chrome window and waits up to 2 minutes for you to complete Google sign-in manually. The authenticated browser state is saved to `playwright/.auth/admin.json`.

```bash
cd tests/e2e
npm run test:e2e:setup
```

When the browser opens:
1. You are on `http://localhost:5173/signin`
2. Click **Sign in with Google**
3. Complete the Google sign-in flow in the browser window
4. Once you are redirected back to the app, Playwright saves the state and exits

You only need to repeat this when the saved session expires (typically ~1 week).

### Step 2 — Run the demo

```bash
cd tests/e2e
npm run test:e2e:demo
```

This runs all tests in a headed browser using your saved Google session.

---

## View the HTML report

After any test run:

```bash
cd tests/e2e
npm run test:e2e:report
```

---

## Troubleshooting

**Google says "This browser or app may not be secure"**
The setup test is running with Playwright's bundled Chromium instead of real Chrome. Check that `channel: 'chrome'` is set in the `setup` project in `playwright.config.ts` and that Google Chrome is installed at its default location.

**`ERR_CONNECTION_REFUSED` on `localhost:5173`**
Playwright starts Vite automatically, but it can take a few seconds. If you see this error on a cold start, re-run — Playwright will retry. If it persists, start Vite manually first: `cd src/Kanban.Web && npm run dev`.

**`ERR_CONNECTION_REFUSED` on `localhost:5077`**
The API is not running. Start it: `cd src/Kanban.Api && dotnet run`.

**`404` on `/api/v1/dev/authenticate`**
The API is running in a non-Development environment. Check `ASPNETCORE_ENVIRONMENT` — it must be `Development` (the default when running with `dotnet run`).

**Google setup test is skipped in CI mode**
Expected — `google: authenticate as admin` is skipped when `PLAYWRIGHT_AUTH_BYPASS=true`. That is correct behaviour.

**Saved session expired (demo mode)**
Re-run `npm run test:e2e:setup` and sign in again.

---

## Test coverage

| File | Scenarios | Notes |
|------|-----------|-------|
| `Auth/AdminSignInTests.spec.ts` | US1 1–4 | All live |
| `Auth/InviteAcceptanceTests.spec.ts` | US3 1,3–6 | Live; US3-2 + US2 1–5 scaffolded pending board page |

---

## How auth bypass works

`GET /api/v1/dev/authenticate?email=...` is registered in `Program.cs` only when `IsDevelopment()`. It looks up the user by email in the database and issues the same `.AspNetCore.Cookies` cookie that Google OAuth would have produced. For emails not in the database, it issues an unregistered Google-user cookie (sub + email + name, no user_id) — used for testing the accept flow and the not-registered redirect.

`POST /api/v1/dev/seed/invitation` creates invitation records with controlled states (active, expired, consumed) for error-scenario tests.

Neither endpoint is compiled out of production builds, but both are unreachable unless `ASPNETCORE_ENVIRONMENT=Development`.
