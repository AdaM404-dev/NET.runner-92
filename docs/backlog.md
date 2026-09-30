# Backlog

Ordered by priority. Each task has one tag, agreed before starting:
**YOU WRITE** (Samuel implements, Claude reviews), **PAIR** (Claude writes and
explains, Samuel follows up), **CLAUDE** (Claude builds, Samuel reviews).
Verification items from `agent.md` are referenced by their ID.

## Now — foundation

| # | Task | Tag | Notes |
| --- | --- | --- | --- |
| 1 | Finish V-01: exercise movement, jump, view switch, doors in `MainTest`; check `CharacterPreview` | CLAUDE | import, compile and Play-mode entry verified 2026-09-30 |
| 2 | Agree LFS, ownership, branching with AdaM404 | Samuel | [[assets/handoff]] |
| 3 | Add assembly definitions and one EditMode test | PAIR | [[architecture/overview]] |
| 4 | Split `NexusPlayer` into input, motor, camera, HUD; move to Input System | PAIR | V-03; [[systems/player-movement]] |
| 5 | Move the auto-test into a PlayMode test | CLAUDE | depends on 4 |
| 6 | Design kickoff: fill [[design/README]] and derive milestones | Samuel + Claude | blocks everything below |

## Next — first gameplay milestone

To be defined at the design kickoff.

## Done

- 2026-09-30 — Converted the 52 Built-in materials to URP/Lit (V-02); AdaM404 to review the look.
- 2026-09-30 — Agent workflow: `CLAUDE.md`, `docs/`, headless scripts, Unity CLI bridge ([[architecture/tooling]]).

## Later

- Editor validation command for incoming art (missing references, Built-in
  shaders, oversized textures, naming).
- Remove Unity template leftovers (`Assets/TutorialInfo`, `Assets/Readme.asset`)
  and the empty `SampleScene` from Build Settings.
- CI: compile and EditMode tests on every PR.
