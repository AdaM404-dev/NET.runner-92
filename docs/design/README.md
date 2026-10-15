# Game design

**Status: design kickoff pending.** NET.runner-92 is a first-person cyberpunk
RPG. The perspective/model direction below is recorded; the remaining topics
still need design work.

## Perspective direction recorded 2026-10-02

- **Decided 2026-10-15 (#13):** the game is first person. Third person is
  only a debug/development view (Tab, editor and development builds), with no
  polish. AdaM404 proposed it ([comment](https://github.com/AdaM404-dev/NET.runner-92/issues/13#issuecomment-5952922014)),
  Samuel agreed ([comment](https://github.com/AdaM404-dev/NET.runner-92/issues/13#issuecomment-6185718508)).
- **Decided 2026-10-15 (#14):** first person uses the **arms-only model**
  (`NEXUS_FirstPerson_Arms.prefab`), attached to the camera; the full body is
  hidden in first person. The full body stays for the third-person debug view,
  `CharacterPreview` and cutscenes, with no first-person animation work. New
  first-person clips (idle, interact, hack, grip) are made for the arms rig
  `NEXUS_FP_Rig`. Samuel proposed it ([comment](https://github.com/AdaM404-dev/NET.runner-92/issues/14#issuecomment-6187313523)), AdaM404 agreed on the
  issue. This replaces AdaM404's earlier "full body for now" answer.
- Open from #14: whether the arms model includes the cybernetic left arm, and
  which first-person clips AdaM404 makes first.
- No camera, scene or animation behavior was changed by documenting these. See [[systems/camera]] and
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
