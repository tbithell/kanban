<!-- SPECKIT START -->
For additional context about technologies to be used, project structure,
shell commands, and other important information, read the current plan:
@specs/001-auth-onboarding/plan.md
<!-- SPECKIT END -->

# Kanban — Agent & Developer Guidance

## Essential reading before you start
- Constitution (governing principles, solution structure, layer rules): @.specify/memory/constitution.md
- Current feature spec: `specs/<branch>/spec.md` (if one exists)
- Current plan: `specs/<branch>/plan.md` (if one exists)
- Claude Code best practices: https://code.claude.com/docs/en/best-practices

## Hard rules (summary — full detail in constitution)
- Entities NEVER cross the API boundary — DTOs only at the API layer
- SOLID on every class; violations block merge
- No in-class comments — rename instead
- TDD: write failing tests first, then implement (xUnit + FluentAssertions; RTL + Jest for React)
- All four test layers required: unit, integration (real SQLite), RTL component, Playwright e2e
- Transactions: IDbTransaction only (no TransactionScope); deferred + Polly retries; savepoints for concurrency
- gitleaks pre-commit hook MUST be installed before first commit — hard gate, no bypass
- Snyk VS Code extension required; medium/high/critical findings block merge
- Query Microsoft Learn MCP for C# and React best practices before proposing patterns

## Project layout
```
Kanban.Api (REST) / Kanban.Web (Fluent UI 2) / Kanban.Business / Kanban.Domain /
Kanban.Contracts / Kanban.AntiCorruption / Kanban.Data / Kanban.DataAccess
```
See constitution for full dependency matrix and portability / multi-tenancy seams.

## Agent efficiency
- Read the spec before asking questions
- Use subagents for investigation to protect main context
- `/clear` between unrelated tasks
- `/compact` at the end of each implementation phase
- Reference files with `@` rather than copy-pasting content
