---
name: session-start
description: Load the current state of NET.runner-92 at the beginning of a work session and register the task in agent.md. Use when the user says to start a session, asks "where are we", or begins a new task in this repository.
---

# Session start

1. Sync, as `AGENTS.md` section 1 says: `git fetch origin --prune`,
   `git status -sb`, and `git rev-list --count HEAD..origin/main`. Say what
   you found, and bring the branch up to date before doing anything else:
   on `main`, `git merge --ff-only origin/main`; on a branch, merge
   `origin/main` into it; if its pull request is already merged, start a
   new branch from the updated `main`. Do not discard local changes; stop
   and ask on a conflict.
2. Read `agent.md` in full: Current status, Active work, Open items, and the
   last five Work log rows.
3. Run `bin/decisions` to see the open next steps and decisions (GitHub
   issues labelled `decision`; the pinned Roadmap is issue #5). If the user
   named a task or system, read its issue (`gh issue view <n>`) and the
   matching note in `docs/systems/`. Note new comments: an answer from
   AdaM404 or Samuel may change a decision's status.
4. Check the tools: `unity status`. If an editor is connected and ready, the
   bridge is available; otherwise the headless `bin/unity-*` scripts are.
   Details in `docs/architecture/tooling.md`.
5. Report in a few lines: what was last done, what is in progress (including
   rows another agent left in Active work), the next foundation step with its
   status, and which decisions wait for whom.
6. Once the task is agreed (Claude writes all code; Samuel reviews), work
   on a planned change only if its issue has `status: accepted` or Samuel
   says to go ahead; set `status: in progress`. Add a row to
   `agent.md` under Active work, and create a branch if on `main`.
