---
name: session-update
description: Append the current session's conversation to SESSION_LOG.md verbatim
compatibility: Requires SESSION_LOG.md at repo root
metadata:
  author: kanban-project
  source: local
---

# Session Update

Append the current session's exchanges to `SESSION_LOG.md` as a new `## Session N` block.

## Rules (non-negotiable)

1. **Verbatim transcript only.** Copy the actual back-and-forth as it happened — user messages
   and Claude responses — with no paraphrasing, condensing, or summarizing. If a response was
   long, include it in full. The session log is evidence of how AI-assisted development
   actually proceeds; summaries destroy that evidence.

2. **Format each exchange as:**
   ```
   **User:** <exact message>

   **Claude:** <exact response, including any code blocks, lists, or recommendations in full>
   ```

3. **Include everything Claude recommended** — if Claude presented a recommendation, proposed
   text, or an analysis, include the full text. The point of the log is that a reader can see
   exactly what was proposed, not a one-line description of it.

4. **Start a new `## Session N` heading** where N is one more than the last session number in
   the file.

5. **Append only** — never modify earlier sessions.

6. **End with a horizontal rule `---`** unless it is the last entry in the file.

## Execution

1. Read `SESSION_LOG.md` to find the current highest session number.
2. Reconstruct the full session transcript from the conversation in context.
3. Append the new `## Session N` block using the Edit tool (not Write — never overwrite).
4. Confirm the append completed without truncating earlier content.

## What NOT to include

- Tool call output (file reads, bash results) unless Claude quoted them explicitly in a
  response to the user.
- Internal deliberation or thinking — only what was communicated to the user.
- Redundant repetition of the same fix described in a prior session.
