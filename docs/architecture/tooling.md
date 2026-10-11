# Tooling: build, test and the editor bridge

How an agent (or a person in a terminal) compiles, tests and looks at the
game without anyone relaying errors by hand. Verified on Linux with Unity
`6000.6.2f1` on 2026-09-30.

There are two paths. Which one applies depends on whether the Unity editor
has the project open, because batch mode and an open editor cannot share a
project.

## Editor open: the Unity CLI bridge

The project already contains `com.unity.pipeline`, which lets the `unity`
command-line tool drive a running editor. No extra package is needed.

```bash
unity status                       # is an editor connected, and is it "ready"?
unity command                      # list every command the editor exposes
unity command console --level error
unity command recompile            # then poll: unity command recompile_status
unity command open_scene --path Assets/Scenes/MainTest.unity
unity command editor_play          # and editor_stop
unity command run_tests --mode editor   # EditMode tests inside the open editor
unity command run_tests --mode playmode --async_tests true   # PlayMode tests; then
unity command test_status          # poll until "completed"; prints every result
unity command eval 'return Application.unityVersion;'
bin/unity-shot maintest            # Game view -> Logs/shots/maintest.png
bin/unity-shot layout scene        # Scene view
bin/unity-inspect                  # read-only reports: player, scene, environment, animation
bin/unity-inspect doors map        # more reports; see tools/inspect/
```

`bin/unity-inspect` runs the C# snippets in `tools/inspect/` inside the
editor. They only read; the numbers in the `docs/systems/` notes come from
them. `tools/inspect/floorplan.py` turns `bin/unity-inspect map` into
`docs/img/warehouse-floorplan.svg`. Snippets for `unity command eval_file`
cannot contain `using` lines (the code is wrapped in a method, with
`UnityEngine` and `System` already in scope), and `Object` must be written
`UnityEngine.Object`.

Notes from first use:

- Right after the editor starts, commands return `503 Server Busy` until
  import and compilation settle. Retry.
- `capture_game_view --save_path` only writes under `Assets/`, which would
  add screenshots to the asset database. Use `bin/unity-shot`, which decodes
  the inline image into `Logs/shots/` (git-ignored).
- If scripts do not compile, the editor starts in Safe Mode and the bridge
  does not load; `unity status` shows nothing. Fix the errors using
  `bin/unity-compile` with the editor closed.
- Asset commands (`create_prefab`, `save_prefab_contents`,
  `set_material_properties`, `move_asset`, ...) are the way to change scenes,
  prefabs and materials. They keep GUIDs and `.meta` files correct.
- PlayMode tests need the asynchronous form above. On 2026-10-04 a waiting
  `run_tests --mode PlayMode` returned "0 tests" without running anything;
  `--mode playmode --async_tests true` followed by `test_status` ran all
  five (35 s). While they run, the editor is in Play mode: do not edit
  scripts until `test_status` says `completed`.
- Tests live next to their code: `Assets/NETRunner/<Feature>/Tests/EditMode/`
  and `Tests/PlayMode/`, one test assembly each ([[architecture/overview]]).
  Screenshots saved by PlayMode tests go to `Logs/PlayModeTests/`.
- `.claude/skills/unity-cli/` is the CLI's own reference, installed with
  `unity skill install claude-code --local`. Refresh it after upgrading the
  CLI with `unity skill refresh`.

Install the CLI (it is separate from the editor): see `unity-cli` skill,
"Step 1". Version used here: `1.0.0-beta.5`.

### Why not the Unity MCP server

`com.unity.ai.assistant` also ships an MCP server (`~/.unity/relay/relay_linux
--mcp`). Its own documentation marks it deprecated in favour of the Unity CLI,
so it is not configured here.

## Editor closed: headless scripts

Plain wrappers around the editor binary in batch mode. They depend only on
the editor being installed, so they also suit CI.

| Script | Does | Exit codes |
| --- | --- | --- |
| `bin/unity-compile` | Import and compile; prints compiler errors only | 0 ok, 1 errors, 3 editor is open |
| `bin/unity-test edit\|play [filter]` | Runs Test Framework tests; prints a summary and failures | 0 ok, 1 failures, 4 no tests found |
| `bin/unity-build` | Linux player into `Builds/Linux/` | 0 ok, 1 failed |

Full logs: `Logs/cli-*.log`. Test results: `Logs/test-results-<mode>.xml`.
The editor binary is resolved from `ProjectSettings/ProjectVersion.txt` under
`~/Unity/Hub/Editor/`; override with `UNITY_EDITOR=/path/to/Unity`.

These are bash scripts and have only been run on Linux. On Windows they
would need Git Bash and `UNITY_EDITOR` pointing at `Unity.exe`; that is
untested. The `unity command ...` bridge is the same on every platform.

Timings on the development machine: first import about 2 minutes, a warm
compile check about 25 seconds, an EditMode test run about 75 seconds, a
Linux player build about 5 minutes (356 MB output).

## Known platform quirks

- The Linux editor adds `com.unity.sdk.linux-x86_64` and
  `com.unity.toolchain.linux-x86_64-linux` to `Packages/manifest.json`, and
  may touch `ProjectSettings/ProjectSettings.asset` (a `SENTIS_ANALYTICS_ENABLED`
  define), `EditorBuildSettings.asset` and `QualitySettings.asset` (a
  serialization version bump). These are never committed: the two packages
  are toolchains for a Linux host, and the other contributor works on
  Windows. Stage files by name so they stay out; see [[assets/handoff]].
- A player build rewrites three URP assets in `Assets/Settings/` and
  `ProjectSettings/GraphicsSettings.asset`. Restore with
  `git checkout -- Assets/Settings` unless the change is intended.
- Running a URP converter also rewrites `ProjectSettings/URPProjectSettings.asset`.
- Unity's Built-in to URP material converter does not turn emission back on.
  After converting, re-enable `_EMISSION` on materials that glow and check
  them in a screenshot. (Before the conversion of 2026-09-30, closing the
  editor also stripped `_EMISSION` from the then-invalid Built-in materials.)
- `unity command quit` fails in edit mode with Pipeline `0.8.0-exp.1`
  (a `DontDestroyOnLoad` error). Close the editor window instead, or run
  `unity command eval 'UnityEditor.EditorApplication.Exit(0);'`, which exits
  without saving.
- `MainTest` gets marked as unsaved without anyone editing it: URP attaches
  its helper components (`UniversalAdditionalLightData` on 105 lights,
  `UniversalAdditionalCameraData` on the camera) when it first draws them.
  The committed scene file has none of them, so saving would add 106
  components to the teammate's 2 MB scene. Answer **Don't Save** until the
  team decides; agents must not save it either. Play mode is safe: Unity
  restores the unsaved state when Play stops.
