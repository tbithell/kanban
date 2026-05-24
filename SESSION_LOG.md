# AI Session Log — Kanban Build Exercise

**Candidate:** Tyler Bithell  
**Position:** Staff Engineer, Product — Superwise  
**AI:** Claude Code (claude-sonnet-4-6)  
**Date:** 2026-05-23

---

## Session 1

**User:** I want to apply for this position - https://app.trinethire.com/companies/35824-superwise/jobs/120230-staff-engineer-product

As you can see I need to include our AI session logs, so I'll want you to create an md file that you add to as we go.

I plan to use spec kit, which I have already installed along with UV. https://github.com/github/spec-kit

**Claude:** Let me fetch the job posting and the spec-kit repo to understand what we're working with.

*[Fetched job posting and spec-kit README, inspected existing project scaffold]*

Good picture of everything. Let me check the current project directory before creating the session log file.

`specify` 0.8.13 is ready and there's already a .NET project scaffolded. Let me peek at what's there.

Good — a bare ASP.NET Web project on .NET 10. Now I'll create the session log file at the repo root.

`SESSION_LOG.md` is created and ready to grow with each session.

Here's where things stand:

- **Job requirement:** Build a Kanban app with agentic AI, deliver a GitHub repo + session logs + CV. Submission window closes June 15.
- **Stack:** bare ASP.NET 10 Web project already scaffolded; `specify` 0.8.13 ready.
- **spec-kit flow:** the tool wants you to run `/speckit.constitution` → `/speckit.specify` → `/speckit.plan` → `/speckit.tasks` before touching implementation.

**Recommended next step:** run `specify init` (or check if the existing project needs a `specify` config), then use `/speckit.constitution` to set the governing principles (quality bar, testing standards, auth approach, etc.) and `/speckit.specify` to write the Kanban feature spec. Want to do that now, or would you like to talk through the architecture first?

**User:** This session log doesn't look like our conversation, my read is that is what they want.

**Claude:** You're right — they want the literal AI conversation transcript, not a polished summary I wrote. Rewriting it as our actual dialogue now.

**User:** I've initiated the project in specify, note the `Kanban/.specify` as well as the new skills in `Kanban/.claude/`

**Claude:** Inspected the generated structure — `specify init` ran with the `claude` integration and installed 14 skills (`speckit-constitution`, `speckit-specify`, `speckit-plan`, `speckit-tasks`, `speckit-implement`, and git/analysis helpers). The constitution file at `.specify/memory/constitution.md` is still the blank template. The next step is to invoke `/speckit-constitution` with your governing principles so it fills out the constitution and syncs the dependent templates before we write the spec.

**User:** `/speckit-constitution`

Testing discipline is TDD, unit tests, in repo integration test and Playwright automated tests. Agents must use TDD.

UI Framework will be the latest version of React.js

Persistence will be SQLite. We need auth. Users should be able to log in via their Google account.

**Claude:** Ran the mandatory `speckit-git-initialize` pre-hook (repo already initialized, skipped). Drafted and wrote the constitution to `.specify/memory/constitution.md` with four core principles:

- **I. Spec-First** — No code without a committed spec.
- **II. Test-Driven Development (NON-NEGOTIABLE)** — TDD mandatory for all work including AI agents; three layers required: xUnit unit tests, xUnit integration tests against real SQLite, and Playwright e2e covering every spec user scenario.
- **III. Simplicity at Velocity** — YAGNI, minimum dependencies, no premature abstraction.
- **IV. Security-First** — Google OAuth 2.0 only, no custom credential storage, input validation at API boundary, OWASP Top 10 awareness.

Stack locked: ASP.NET 10 minimal API backend, React (latest, Vite/TypeScript), SQLite with EF Core migrations, xUnit, Playwright.

Also updated `.specify/templates/tasks-template.md` to reflect that tests are REQUIRED (not optional) on every feature per the TDD mandate.

**User:** Before we start writing spec, let's get more specific in our constitution and add more to it. We want to develop an easily maintainable codebase, by both humans and AI agents. SOLID is a must. The code should be easy to read, methods should do what their name says they do so there is very little if any need for in-class comments.

We want to make sure to follow well established design patterns. The solution should be structured as follows:

A gRPC API project will serve as the back end API that the React.js front end (also in its own project) will use to access the back end. There should be a business layer project that will have a simple fluent interface implementation and the builders will be created via factories. We will follow domain driven design, so there will be a domain project for entities and a contracts project for DTOs that will be transformed by the business layer via simple transforms. We will have an anti-corruption layer project for external API requests etc. There will be a database project for the data layer and a data access layer project. We'll use Dapper for database access.

Let's also give devs in this repo a head start by pushing them in the direction of using https://code.claude.com/docs/en/best-practices.

We need to build token efficiency into our constitution and set this solution up to be as efficient as possible. We want the most we can get out of the fewest tokens used per prompt.

**Claude:** Fetched the Claude Code best practices page (key insight: context window is the primary constraint; CLAUDE.md must stay lean or agents ignore it). Left MCP setup to the user per their preference.

Bumped constitution to v1.1.0 (MINOR) with three new principles and three new sections:

New principles:
- **V. SOLID (NON-NEGOTIABLE)** — all five rules enforced, violations block merge
- **VI. Self-Documenting Code** — no in-class comments; rename until the name explains the behavior
- **VII. Established Design Patterns** — GoF/DDD patterns required by layer (fluent+factory in Business, Repository in DAL, Adapter in ACL, etc.)

New sections:
- **Solution Structure** — eight-project layout with strict dependency matrix; entities never appear in Api project
- **Domain-Driven Design Constraints** — explicit rule: entities MUST NEVER cross the API boundary; DTOs only at API layer; all transforms in Business layer
- **Token Efficiency & AI Agent Ergonomics** — lean CLAUDE.md, `@`-references, subagents for investigation, `/clear` and `/compact` discipline, self-documenting code reduces re-reads

Also rewrote `CLAUDE.md` to be a lean ~30-line guide pointing agents at the constitution, the spec, the plan, and the Claude Code best practices URL. Hard rules summarized inline so agents get them even without reading the full constitution.

**User:** `/speckit-constitution` — I've got a bit more to add to the constitution. We are looking to always have an eye to getting to MVP as defined here - https://www.svpg.com/minimum-viable-product/

We are striving to create a product that users love to use after all, and we want to use the best practices found, for design patterns, usage patterns, React.js and C# that we can access via the Microsoft Learn MCP server that is now installed. FluentUI will help us get there as far as user experience so we should be implementing Fluent 2. Let's take advantage of this Fluent UI testing strategy as well - https://microsoft.github.io/fluentui-charting-contrib/docs/Testing%20Strategy

As you can see I'm quite focused on NFRs, so let's always keep that in mind. Maintainability, Usability and performance are important areas we want to focus on to set ourselves up for success.

Another thing on the unit and integration test front is we will be using xUnit and Fluent Assertions.

**Claude:** Fetched the Fluent UI testing strategy (three-tier decision tree: Jest for pure logic → React Testing Library + Jest for component rendering → Playwright for browser behavior; ≥90% automated coverage). SVPG MVP page returned 403 but definition is well-established: MVP = minimum scope, not minimum quality — a product people love.

Bumped constitution to v1.2.0 (MINOR) with two new principles and multiple stack updates:

- **VIII. MVP-Oriented Delivery** — SVPG definition governs; every feature must solve a real user problem; "minimum" applies to scope only, not quality
- **IX. NFRs as first-class acceptance criteria** — Maintainability (SOLID + cyclomatic complexity ≤ 10), Usability (Fluent UI 2, WCAG 2.1 AA, RTL accessibility queries), Performance (gRPC ≤ 200ms p95, no N+1 queries, bundle size monitored)
- **Principle II updated** — xUnit + FluentAssertions mandatory (`Should().Be()` not `Assert.Equal`); React Testing Library component test tier added; Fluent UI test decision tree formalized
- **Principle IV updated** — Snyk coverage expanded to full surface: SCA, SAST, license compliance, IaC; VS Code extension required; medium/high/critical findings block merge
- **Principle IV** — gitleaks pre-commit hard gate added: secrets blocked at commit time, no `--no-verify` bypass permitted, `.gitleaks.toml` committed to repo root
- **Stack table** updated: Fluent UI 2, FluentAssertions, RTL+Jest, gitleaks, Microsoft Learn MCP reference

**User:** I realize now that we need to pivot to REST from gRPC since there is no reason for back end to back end communication and the API will only be called by the UI.

I also see we are missing linting tools, so let's make sure that we use ESLint and Prettier for FE and let's get a standard EditorConfig planned for the backend. I want this EditorConfig to be simple and useful for both agents and humans.

I also need to call out that SQLite, while super convenient for local dev simplicity, zero config etc, we have to make sure that we make it easy to swap for Postgres were this to go to a production deployment. Let's make sure we are clear on SQL injection protection, auth and input validation, and that the schema reflects how a real Kanban works... we'd need boards, lanes, cards, ordering, assignees. Remember we'll use Dapper as our ORM.

**Claude:** Bumped constitution to v1.3.0 (MINOR). Changes:

- **REST replaces gRPC** — simplified to ASP.NET 10 minimal API over HTTP; no gRPC-Web on the frontend; all references updated throughout
- **ESLint + Prettier** — `@typescript-eslint`, `eslint-plugin-react`, `eslint-plugin-react-hooks`, `eslint-plugin-jsx-a11y` (a11y as errors); `eslint-config-prettier` last in extends chain; `lint-staged` pre-commit hook
- **EditorConfig** — explicit `.editorconfig` committed to repo root: 4-space C#, 2-space web files, LF, UTF-8, trim trailing whitespace, final newline
- **Database Portability section** — `IDbConnection`-only in DataAccess; connection factory is the single swap point; `migrations/sqlite/` and `migrations/postgres/` subfolders; no `RETURNING` in INSERTs; SQLite for CI, Postgres for staging
- **SQL injection prevention** — Dapper `@paramName` mandatory; whitelist dictionary for dynamic ORDER BY; no string interpolation in SQL
- **Domain Model Concepts section** — governing entities (User, Board, Lane, Card, CardAssignee) and ordering invariant; full SQL schema deferred to spec + Data project
- **Multi-Tenancy Readiness section** — out of scope for MVP but seams preserved: all data user-scoped, auth context via DI not method params, Board as natural tenant boundary, GUIDs on all PKs, no static state, IDbConnection factory enables database-per-tenant routing later

**User:** I just want to add that we'll use Polly for retries.

**Claude:** Noted. Also fetched the Microsoft Learn SQLite transactions doc — key findings: no TransactionScope (SQLite doesn't support DTC); use `IDbTransaction` directly; `BeginTransaction(deferred: true)` for read-then-write operations; savepoints for optimistic concurrency. Added Transaction Pattern section and Polly as the standard retry/resilience mechanism across the app. CLAUDE.md updated to surface the transaction rule inline.

CLAUDE.md updated to surface gitleaks gate, Snyk medium+ block, and RTL/FluentAssertions requirement at a glance.
