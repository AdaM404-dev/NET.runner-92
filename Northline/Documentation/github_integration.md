# Northline repository integration

Northline Phase 07 runs as a sibling HDRP Unity project. The root project uses URP and its own `NetRunner.Core`, `NetRunner.Player`, `NetRunner.World` and `NetRunner.MainMenu` assemblies. Open `Northline/NETRunner92_Hospital` in Unity Hub to play or develop the snapshot.

The import preserves the verified Northline C# source, scene files, asset GUIDs and pinned dependencies. It uses the existing project boundaries so the Unity projects load their own assets and packages. A future port into the root project needs URP material and lighting conversion, a mapping to the current feature assemblies and tests against the shared player and interaction contracts. [Root architecture](../../docs/architecture/overview.md) · [Coding standards](../../docs/architecture/coding-standards.md)

The previous working branch was based on `8d5de4e`. The integration uses the rewritten GitHub history based on `9460f12`. The source snapshot was transferred as files from a local backup. The root Git LFS attributes apply to the Northline art, audio and video. Northline adds scoped rules for mesh binary data and its interface archive. [LFS migration](https://github.com/AdaM404-dev/NET.runner-92/issues/17) · [Production materials](../README.md)

The repository includes the final 60-second intro under Git LFS. Windows binaries and individual frame sequences are separately packaged for a release. The Phase 08 game HUD and presentation are outside this snapshot. [Release index](../Builds/README.md)

## Validation

A fresh Windows import in Unity 6000.6.2f1 ran the Northline PlayMode validator on Direct3D 11. All 95 checks passed, covering the hospital route, doors and lift, K-7 patrols, weather, reception relay and six live camera feeds on LATCH-07. The reception tablet and a camera feed were inspected in the generated captures.

The staged snapshot retained all 35 C# files byte for byte and preserved 313 unique Unity GUIDs. All 222 new LFS pointers were matched against their local assets. The root project keeps the upstream URP settings, namespaces and assemblies.

The root URP project also imported and compiled in Unity 6000.6.2f1 on Windows. All 10 EditMode and 5 PlayMode tests passed.
