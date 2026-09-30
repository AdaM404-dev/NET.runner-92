# 0001 — Project knowledge lives in `docs/` inside the repo

Date: 2026-09-30 · Status: accepted

## Context

The project is large and is worked on by two people, each with an AI agent
that starts every session without recollection of the previous one. Design
intent, architecture and past decisions have to be written down somewhere
both people and both agents can reach. An Obsidian "second brain" was
suggested. The repository already had `agent.md`, a shared status file and
work log maintained by the first agent.

## Decision

- Knowledge is plain Markdown in `docs/`, committed with the code. Obsidian
  opens `docs/` as a vault and is used as a viewer and editor, not as the
  store; no Obsidian plugin or sync service is required.
- `agent.md` stays the single shared status file and work log. `docs/` does
  not duplicate it.
- `CLAUDE.md` holds only what an agent needs on every session: commands,
  conventions, and pointers.

## Consequences

- Docs change in the same commit as the code they describe, and are reviewed
  in the same PR.
- Both agents read them with ordinary file access.
- The repository is public, so design notes are public too.
- A vault kept outside the repo would not be visible to the teammate and
  would drift from the code; this rules that out.
