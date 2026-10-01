# Journal

Newest entry first. One entry per session: what was built, what was learned,
what was confusing. The authoritative record of changes is the work log in
`agent.md`; this is the human-readable story.

## 2026-10-01 — Decisions move to GitHub issues

- Samuel asked for one place where every next step is written with why it
  is needed and what will change, and chose GitHub Issues for it.
- Created labels for status, missing agreement, who builds and size, the
  Foundation milestone, and 15 issues: the pinned Roadmap (#5), the seven
  foundation steps (#6 to #12), six open decisions (#13 to #18) and the old
  decision 0001 (#19, closed). Issue #6, renaming the layers, is the full
  example of the format.
- Seven issues are assigned to AdaM404, because questions written into pull
  request descriptions were merged without answers three times.
- Added an issue form, a pull request template and `bin/decisions`;
  [[decisions/README]] explains the system.

## 2026-09-30 — Understanding what exists

- Stepped back before writing new code: none of the existing player, camera
  or door code was written by Samuel, so it had to be understood first.
- Inspected the open editor read-only and checked behaviour in Play mode
  with scripted calls (walking, jumping, view switch, doors, interaction
  reach, lower detail levels).
- Surprises: the player exists three times; layers 8 and 9 are used but
  unnamed; 71 of 113 doors are exported standing open, so E closes them while
  the screen says "Access granted"; E only reaches 0.3 m in third person; the
  built game would start in an empty scene; only the most detailed character
  model animates.
- Wrote the learning path ([[guide/01-unity-in-this-project]],
  [[guide/02-guided-tour]]), the [[architecture/project-map]], seven system
  notes, a floor plan, and the ordered list of changes in
  [[architecture/before-new-scripts]].
- Next: Samuel does the tour and writes down what was unclear; the team
  answers decisions D1–D6.

## 2026-09-30 — Workflow setup

- Read the repository for the first time: a complete Unity 6000.6.2f1 URP
  project with the NEXUS character, a warehouse environment, two scripts and
  an agent handoff file.
- Decided to keep knowledge in `docs/` inside the repo and view it with
  Obsidian (decision 0001, now [issue #19](https://github.com/AdaM404-dev/NET.runner-92/issues/19)).
- Added `CLAUDE.md`, this `docs/` vault, and headless `bin/unity-*` scripts.
- Fixed pointers to the repository's old name in `README.md` and `agent.md`.
