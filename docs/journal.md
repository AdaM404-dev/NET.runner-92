# Journal

Newest entry first. One entry per session: what was built, what was learned,
what was confusing. The authoritative record of changes is the work log in
`agent.md`; this is the human-readable story.

## 2026-10-04 — Foundation steps 1 to 4

- Steps 1 to 3 (PRs #23 and #24): named layers and `LayerMask` fields,
  template leftovers removed, company name `NET.runner`, PC only, the
  warehouse as a prefab and a gameplay scene of our own.
- Step 4 (#9), as PAIR: the code moved into feature folders under
  `Assets/NETRunner/` (`Core`, `World`, `Player`), like the main menu, with
  one assembly definition and namespace each. Samuel picked feature folders
  over the issue's original `Assets/Scripts/Core…` layout. See
  [[architecture/overview]].
- First tests for the gameplay code: the gravity and jump maths moved into
  `VerticalMotion` in `Core` with six EditMode tests; three PlayMode tests
  walk, run and jump the player in the gameplay scene and open a door. They
  replace the old command-line auto-test.
- Learned: moving a script through the editor keeps its GUID, so every scene
  and prefab still finds it. An assembly definition cannot use code from
  Unity's default assembly, so scripts that arrive with art need their own
  assembly definition before our code can call them (the K7 robot). PlayMode
  tests in the open editor only ran in the asynchronous form
  (`--async_tests true`, then `test_status`).
- Next: Samuel's follow-up for Step 4 (move the door's easing formula into
  `Core` with a test), then Step 5 (#10), which Samuel writes.

## 2026-10-03 — Documentation audit of October 2

- Checked the October 2 main-branch history (Europe/Budapest): PR #20
  merged the existing issue workflow, PR #21 corrected warehouse doors,
  and PR #22 added the independent main menu.
- Door/warehouse system notes and art handoff already documented the
  closed-door revision. Added the missing menu system note and links,
  refreshed architecture/project map, and corrected stale README/status text.
- Recorded AdaM404's first-person/full-body direction from issue comments
  #13 and #14, preserving the distinction between direction and implemented
  behavior. No gameplay or Unity settings changed; Unity tests were not rerun.

## 2026-10-02 — Warehouse revision and standalone terminal

- PR #21 closes all 119 warehouse doors and adds editable warehouse art
  under `ArtSource/Warehouse_NearFuture`; 71 previously open leaves were
  corrected. Its recorded validation passed 594 Unity assertions; see
  [[systems/doors-and-interaction]] and [[systems/level-warehouse]].
- PR #22 adds a standalone surveillance menu with boot, four feeds,
  environmental events, session settings and simulated transfer/return.
  Its recorded validation passed two PlayMode tests; see [[systems/main-menu]].
- PR #20 merged the issue workflow authored on October 1. Decisions and
  next steps remain in GitHub Issues rather than a second docs backlog.

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
