# Backlog

Ordered by priority. Each task has one tag, agreed before starting:
**YOU WRITE** (Samuel implements, Claude reviews), **PAIR** (Claude writes and
explains, Samuel follows up), **CLAUDE** (Claude builds, Samuel reviews).
Verification items from `agent.md` are referenced by their ID.

## Now: understand, then the foundation

The foundation steps follow [[architecture/before-new-scripts]]; the numbers
in brackets are its step numbers. No new gameplay scripts until these are
done or explicitly skipped.

| # | Task | Tag | Notes |
| --- | --- | --- | --- |
| 1 | Do the guided tour and note what was unclear | Samuel | [[guide/02-guided-tour]]; unclear points go into [[journal]] |
| 2 | Answer the open decisions D1–D6 | Samuel, AdaM404 | listed at the end of [[architecture/before-new-scripts]] |
| 3 | Agree LFS, ownership, branching with AdaM404 | Samuel | [[assets/handoff]] |
| 4 | Name layers 8 and 9, use `LayerMask` fields (step 1) | CLAUDE | S |
| 5 | Remove template leftovers, set company name (step 2) | CLAUDE | S; needs D5 |
| 6 | Gameplay scene, warehouse prefab, one player prefab, build list (step 3) | CLAUDE | M; needs AdaM404's OK (D3) |
| 7 | Code structure, `.editorconfig`, first EditMode and PlayMode tests (step 4) | PAIR | M; the auto-test moves into the PlayMode test |
| 8 | `IInteractable` and `PlayerInteractor`; doors implement it (step 5) | YOU WRITE | M; Samuel's first own script |
| 9 | Split `NexusPlayer` into components, same behaviour (step 6) | PAIR | L; depends on 7 and 8 |
| 10 | Switch to the Input System (step 7) | PAIR | M; closes V-03 |
| 11 | Design kickoff: fill [[design/README]] and derive milestones | Samuel + Claude | decides the first gameplay |

## Next: first gameplay milestone

To be defined at the design kickoff.

## Soon (from "Should do soon" in [[architecture/before-new-scripts]])

- S1 tunable numbers into a settings asset (with 9).
- S2 UI Toolkit HUD with crosshair and interaction prompt (after 8).
- S3 camera post-processing and anti-aliasing, Volume in the gameplay scene.
- S4 doors: kinematic Rigidbody, open away from the player, states, events, sounds.
- S5 orbit viewer for `CharacterPreview`; remove `previewMode` and F1 from the player.
- S6 Animator speed from real movement; LOD1–3 animation; request missing clips.
- S7 remove `Application.runInBackground` from the player script.

## Later

- Navigation mesh on the warehouse, then import the K7 robot (V-07).
- Lighting pass: shadows, reflection probes, baked or probe lighting, occlusion data.
- Performance baseline in a built game.
- Editor validation command for incoming art (missing references, Built-in
  shaders, oversized textures, naming).
- CI: compile and EditMode tests on every PR.

## Done

- 2026-09-30: Documented the current game objects: [[guide/01-unity-in-this-project]],
  [[guide/02-guided-tour]], [[architecture/project-map]], seven system notes,
  [[architecture/before-new-scripts]]; `bin/unity-inspect`. Finished V-01 with
  scripted Play-mode checks.
- 2026-09-30: Converted the 52 Built-in materials to URP/Lit (V-02); AdaM404 to review the look.
- 2026-09-30: Agent workflow: `CLAUDE.md`, `docs/`, headless scripts, Unity CLI bridge ([[architecture/tooling]]).
