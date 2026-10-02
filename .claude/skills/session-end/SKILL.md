---
name: session-end
description: Close a NET.runner-92 work session by verifying the work, updating agent.md and docs, and committing. Use when the user says to wrap up, end the session, or hand off.
---

# Session end

1. Verify what was changed. Code: compile and run the relevant tests
   (`unity command recompile` and `run_tests` with the editor open, or
   `bin/unity-compile` and `bin/unity-test` with it closed). Visual changes:
   take a screenshot with `bin/unity-shot` and look at it. Record what was
   and was not checked; never write that something works if it was not run.
2. Update `agent.md`, pulling first:
   - close or update this session's Active work row,
   - adjust Current status and Open items,
   - append one Work log row: work completed, verification performed, commit.
   Append only; do not rewrite earlier rows.
3. Update `docs/`:
   - the `docs/systems/` note for every system whose behaviour or API changed,
   - a dated entry at the top of `docs/journal.md`, including what Samuel
     learned or found confusing on YOU WRITE and PAIR tasks.
4. Update the decision issues on GitHub:
   - answers given in chat become comments on their issue, with the labels
     updated (`needs …` removed, `status: accepted` when nobody is missing),
   - a new next step or decision becomes a new `decision` issue with the ten
     sections of `docs/decisions/README.md`,
   - a finished step's pull request says `Closes #<n>`, and its box in the
     Roadmap issue (#5) is ticked once merged.
5. Before staging, leave out editor-generated changes that the team has not
   agreed to commit (see "Known platform quirks" in
   `docs/architecture/tooling.md`). Stage files by name, not `git add -A`.
6. Commit on the feature branch. Push and open or update the PR only if the
   user asked for it in this session.
7. Summarise: what is done, what is verified, what is left, what needs a
   decision from Samuel or AdaM404.
