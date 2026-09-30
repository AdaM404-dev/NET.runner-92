# Art handoff and ownership

**Status: proposal, not yet agreed with AdaM404.** Until it is agreed, treat
it as the default and ask before crossing a boundary.

## Who owns what

| Area | Owner |
| --- | --- |
| `Assets/Characters`, `Assets/Environment`, `Assets/Materials`, `Assets/Animations`, `Assets/Documentation` | AdaM404 |
| Preview scenes (`CharacterPreview.unity`, `MainTest.unity` as an asset preview) | AdaM404 |
| `Assets/Scripts`, gameplay scenes, `docs/`, `bin/` | Samuel |
| `Assets/Prefabs` | Shared: art prefabs are AdaM404's; gameplay prefabs wrap them as variants or nested prefabs |
| `ProjectSettings/`, `Packages/` | Shared: change by PR only |

## How art arrives

- Art is delivered as prefabs plus their models, textures and materials, each
  with its `.meta` file. It is not delivered as edits to gameplay scenes.
- Gameplay never modifies an art prefab in place; it nests it or makes a
  prefab variant, so a re-export does not wipe gameplay wiring.
- Materials use URP shaders. Built-in Standard materials render magenta in
  this project (`agent.md` item V-02).
- Source attribution and licences go in `Assets/Documentation/`.

## Open questions for the team

1. **Git LFS.** The repository has no LFS; binaries up to 24 MB are in normal
   history (about 155 MB after nine commits). Moving existing files into LFS
   rewrites history and needs a fresh clone by everyone, which is cheapest to
   do now. The alternative is LFS for new files only.
2. **Editor platforms.** The Linux editor adds two Linux toolchain packages
   to `Packages/manifest.json`. Which OS does each person use, and should
   those entries be committed?
3. **Branching.** Proposed: feature branches and PRs, no direct pushes to `main`.
4. **Visibility.** The repository is public; is that intended?
