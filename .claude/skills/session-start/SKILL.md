---
name: session-start
description: Load the current state of NET.runner-92 at the beginning of a work session and register the task in agent.md. Use when the user says to start a session, asks "where are we", or begins a new task in this repository.
---

# Session start

1. Sync: `git fetch`, then `git status -sb`. If the current branch is behind
   its upstream, say so and pull before doing anything else. Do not discard
   local changes.
2. Read `agent.md` in full: Current status, Active work, Open items, and the
   last five Work log rows.
3. Read `docs/backlog.md`. If the user named a task or system, also read the
   matching note in `docs/systems/` and any ADR it links.
4. Check the tools: `unity status`. If an editor is connected and ready, the
   bridge is available; otherwise the headless `bin/unity-*` scripts are.
   Details in `docs/architecture/tooling.md`.
5. Report in a few lines: what was last done, what is in progress (including
   rows another agent left in Active work), and the top backlog items with
   their tags.
6. Once the task is agreed, confirm its tag (YOU WRITE, PAIR or CLAUDE), add
   a row to `agent.md` under Active work, and create a branch if on `main`.
