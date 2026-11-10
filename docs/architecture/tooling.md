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
unity command open_scene --path Assets/NETRunner92/Scenes/Hospital_Exploration.unity
unity command editor_play          # and editor_stop
unity command run_tests --mode editor   # EditMode tests inside the open editor
unity command run_tests --mode playmode --async_tests true   # PlayMode tests; then
unity command test_status          # poll until "completed"; prints every result
unity command eval 'return Application.unityVersion;'
bin/unity-shot hospital            # Game view -> Logs/shots/hospital.png
bin/unity-shot layout scene        # Scene view
bin/unity-inspect                  # read-only reports: scene, environment, animation
```

`bin/unity-inspect` runs the C# snippets in `tools/inspect/` inside the
editor. They only read; the numbers in the `docs/systems/` notes come from
them. The warehouse reports (`player`, `doors`, `map` and the floor-plan
generator) were removed with the warehouse in #31; hospital reports come
with #31 PR B. Snippets for `unity command eval_file`
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
| `bin/unity-build [linux\|windows]` | player build into `Builds/Linux/` or `Builds/Windows/` (needs that platform's build support in the editor); runs with a graphics device for HDRP | 0 ok, 1 failed |

The hospital's own Play-mode validator (95 checks: route, doors, lift,
patrols, weather, LATCH-07, CCTV) runs headless too, with the editor
closed:

```bash
source bin/_unity.sh && run_unity validate -executeMethod NETRunner92.Editor.PlayModeValidation.Run
grep 'NET92 QA' Logs/cli-validate.log     # one PASS or FAIL line per check
```

It exits 0 when every check passes and writes its captures and
`playmode_checks.txt` into `Northline/Renders/Phase_07_CCTV/`, overwriting
the committed ones; restore them with `git checkout -- Northline/Renders`
unless the new captures are meant to be committed. It must not use
`-nographics`: the CCTV checks need HDRP to render. #31 PR B turns these
checks into NUnit PlayMode tests.

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

- Art is in Git LFS since 2026-11-09 (see [[assets/handoff]]). Run
  `git lfs install` once before cloning. Merged branches are deleted on
  GitHub after merging.

- The Linux editor adds `com.unity.sdk.linux-x86_64` and
  `com.unity.toolchain.linux-x86_64-linux` to `Packages/manifest.json`, and
  `com.unity.sysroot.base` with them to `packages-lock.json`, and may touch
  `ProjectSettings/ProjectSettings.asset` (`APP_UI_EDITOR_ONLY` and
  `SENTIS_ANALYTICS_ENABLED` defines), `EditorBuildSettings.asset` (a
  `com.unity.dt.app-ui` entry) and `QualitySettings.asset` (a
  serialization version bump). These are never committed: the packages
  are toolchains for a Linux host, and the other contributor works on
  Windows. Stage files by name so they stay out; see [[assets/handoff]].
  When one of these files has an intended change too (as in #31: HDRP,
  input, layers, build list), stage a copy with only that change, for
  example by writing it to the index with `git hash-object -w` and
  `git update-index --cacheinfo`, and check `git diff --cached`.
- **HDRP needs Vulkan on Linux** (since #31). With automatic graphics APIs
  the Linux editor started on OpenGL, where HDRP draws nothing ("graphics
  API OpenGLCore is not supported with HDRP"). Player Settings now list
  Vulkan as the only Linux API, so the editor starts on Vulkan; the setting
  does not affect Windows. If it ever starts on OpenGL again, add
  `-force-vulkan`.
- `unity command quit` fails in edit mode with Pipeline `0.8.0-exp.1`
  (a `DontDestroyOnLoad` error). Close the editor window instead, or run
  `unity command eval 'UnityEditor.EditorApplication.Exit(0);'`, which exits
  without saving.
