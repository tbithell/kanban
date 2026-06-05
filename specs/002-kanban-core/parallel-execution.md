# Parallel Execution Guide — 002-kanban-core

**When to use this**: After the `002-setup` branch (Phase 1 + Phase 2) is merged into
`002-kanban-core`, the dependency graph allows two independent tracks to run in parallel.
This guide explains how to split, work, and re-integrate them.

## Dependency Graph (Quick Reference)

```
002-setup (Phase 1 + Phase 2) — MUST MERGE FIRST
    ├── Track A: 002-us1-boards-lanes  (Phase 3: US1) → 002-us3-drag-drop (Phase 5: US3)
    └── Track B: 002-us2-cards         (Phase 4: US2) → 002-us4-membership (Phase 6: US4)

Both tracks merged back into 002-kanban-core → US5 + Polish (Phases 7–8)
```

## Step 1 — Create the Two Worktrees

Run these from the repo root **after** `002-setup` is merged into `002-kanban-core`:

```bash
# Track A — boards, lanes, then drag-and-drop
git worktree add ../kanban-us1 -b 002-us1-boards-lanes

# Track B — cards, then membership
git worktree add ../kanban-us2 -b 002-us2-cards
```

Both branches are cut from `002-kanban-core` so they start with all Phase 1/2 infrastructure.

## Step 2 — Open Chat Windows

Open each worktree folder as a separate VS Code workspace (File → Open Folder):

| Window | Directory | Branch |
|--------|-----------|--------|
| Window 1 (Track A) | `../kanban-us1` | `002-us1-boards-lanes` |
| Window 2 (Track B) | `../kanban-us2` | `002-us2-cards` |

Each Claude Code chat in a VS Code window operates on that window's working directory,
so the two agents edit completely different file trees.

## Step 3 — Track A Work Order

**Phase 3 (US1): T031–T047**

1. Write all five US1 test files first (T031–T035) — commit (red)
2. Implement `BoardService` + `LaneService` (T036–T037) — commit (green)
3. Implement endpoints + hooks + components (T038–T047) — commit per logical group
4. Validate US1 checkpoint (all tests green)

**Phase 5 (US3): T060–T068** — start after Track B completes T055 (`CardItem.tsx`)

> **Sync point**: Before T067 (making CardItem sortable), pull the `CardItem.tsx` file
> from Track B's branch:
> ```bash
> git checkout 002-us2-cards -- src/Kanban.Web/src/components/board/CardItem.tsx
> ```
> This is the only cross-track dependency.

1. Write US3 test files (T060–T062) — commit (red)
2. Implement hooks + `KanbanBoard` + previews + sortable wiring (T063–T068) — commit (green)
3. Validate US3 checkpoint

## Step 4 — Track B Work Order

**Phase 4 (US2): T048–T059**

1. Write all US2 test files (T048–T051) — commit (red)
2. Implement `CardService` (T052) — commit (green)
3. Implement endpoints + hooks + components (T053–T059) — commit per logical group
4. Validate US2 checkpoint

**Phase 6 (US4): T069–T079** — no dependency on Track A

1. Write all US4 test files (T069–T072) — commit (red)
2. Implement `BoardMembershipService` + extend `InvitationService` (T073–T074) — commit (green)
3. Implement endpoints + hooks + `BoardMembersPanel` (T075–T079) — commit per logical group
4. Validate US4 checkpoint

## Step 5 — Merge Both Tracks Back

```bash
# From 002-kanban-core
git merge 002-us1-boards-lanes
git merge 002-us2-cards     # resolve any conflicts (expect Lane.tsx to have sortable additions from Track A vs card additions from Track B)
```

Likely conflict zone: `Lane.tsx` and `BoardPage.tsx` — Track A adds sortable wiring;
Track B adds card rendering. Merge manually: the final file should have both.

## Step 6 — Finish on 002-kanban-core

- **Phase 7 (US5)**: T080–T081 — viewer role guards across all components
- **Phase 8 (Polish)**: T082–T087 — accessibility audit, bundle size, full test run

## Worktree Cleanup

After both tracks are merged and verified:

```bash
git worktree remove ../kanban-us1
git worktree remove ../kanban-us2
git branch -d 002-us1-boards-lanes 002-us2-cards
```

## Tips

- Each track should commit after every task group — small commits make merge conflicts
  easier to resolve and preserve the TDD red/green history (constitution requirement).
- Don't forget: failing-test commit MUST precede passing-implementation commit in each track.
- If Track B finishes US2 before Track A needs `CardItem.tsx`, just ping Track A so it
  knows the sync point is ready.
- Run `dotnet build` and `npm run build` at each checkpoint before moving to the next story —
  catches DI registration misses and TypeScript errors early.
