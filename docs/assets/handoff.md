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
- Source packages that Unity must not import (Blender files, generator
  scripts) go under `ArtSource/`, like the K7 robot.
- Proposed (decision D6 in [[architecture/before-new-scripts]]): export
  doors shut. Today 71 of 113 swing doors arrive standing open and the door
  script treats that pose as "closed"; see [[systems/doors-and-interaction]].
- Proposed (decision D3): the warehouse becomes a prefab owned by art, which
  both the preview scene and our gameplay scene place.

## Platforms

Samuel works on Linux, AdaM404 on Windows. Consequences:

- The Linux editor adds two Linux-host toolchain packages to
  `Packages/manifest.json` and touches some `ProjectSettings` files. Those
  changes are never committed; details in [[architecture/tooling]].
- File and folder names must not differ only by letter case, and script
  references must match the file's exact case: Windows ignores case, Linux
  does not.
- Line endings are normalised by `.gitattributes`; Unity YAML and `.meta`
  files are always LF.

## Open questions for the team

1. **Git LFS.** The repository has no LFS; binaries up to 24 MB are in normal
   history (about 155 MB after nine commits). Moving existing files into LFS
   rewrites history and needs a fresh clone by everyone, which is cheapest to
   do now. The alternative is LFS for new files only.
2. **Branching.** Proposed: feature branches and PRs, no direct pushes to `main`.
3. **Visibility.** The repository is public; is that intended?
