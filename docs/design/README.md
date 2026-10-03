# Game design

**Status: design kickoff pending.** NET.runner-92 is a first-person cyberpunk
RPG. The perspective/model direction below is recorded; the remaining topics
still need design work.

## Perspective direction recorded 2026-10-02

- AdaM404 confirmed first person as the default and primary perspective;
  third person is only a debug/development view ([issue #13 comment](https://github.com/AdaM404-dev/NET.runner-92/issues/13#issuecomment-5952922014)).
- Keep the full-body NEXUS model and one skeleton for now, including the
  visible cybernetic arm. Retain the arms-only prefab and reconsider it if
  aiming/weapons require tighter animation control ([issue #14 comment](https://github.com/AdaM404-dev/NET.runner-92/issues/14#issuecomment-5952876378)).
- These comments record AdaM404's direction; issue #14 still has proposed /
  needs-agreement labels at this audit. No camera, scene or animation behavior
  was changed by documenting them. See [[systems/camera]] and
  [[systems/character-and-animation]].

## To settle at the kickoff

- **Pillars** — the three or four things the game must deliver.
- **Core loop** — what the player does minute to minute, and session to session.
- **Player character** — is NEXUS the fixed protagonist? Which interaction and
  hacking animations are needed for the full-body skeleton?
- **Netrunning / hacking** — what it is mechanically and how it ties into
  exploration and combat.
- **RPG systems** — stats, progression, inventory, cyberware, quests, dialogue.
- **Combat** — ranged, melee, stealth, or avoidable.
- **World** — structure (hub and missions, open districts, linear), and how the
  warehouse environment fits.
- **Scope** — target platforms, target length, and the first playable milestone.

Once settled, each topic becomes its own note in this folder
(`pillars.md`, `core-loop.md`, `netrunning.md`, ...) and the tasks that fall
out of it go into [[backlog]].
