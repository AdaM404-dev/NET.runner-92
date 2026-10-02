# NET.runner — standalone observation terminal

Open `Scenes/NETRunner_MainMenu_Prototype.unity` in Unity **6000.6.2f1** and
press Play. The scene is deliberately absent from Build Settings. It never
loads gameplay, touches saves, changes PlayerPrefs or calls the player scripts.

The boot takes approximately five seconds. Select the focused skip button
with Enter to bypass it. Use the mouse or Tab / arrow keys and Enter to access
the menu. Escape closes a modal and returns focus to the button that opened it.
Continue and New Session perform a simulated transfer, reach 100%, display the
local-access messages and return to observation. Disconnect asks for confirmation;
in the editor it returns to the terminal without stopping Play mode. A player
build quits after confirmation.

## Art and content

The service bay is new primitive geometry; the concrete grain and five WAVs
were generated locally by the editor builder. Nothing was downloaded. All
materials use URP; post-processing uses a scene-local volume. Typography uses
Unity's built-in runtime font and default UI Toolkit theme. Audio placeholders
are quiet original synthesis, intended for replacement with authored sound.

There are four fixed surveillance viewpoints. Only one camera renders at a
time. Camera changes occur every 18–27 seconds with a 0.14 s signal break.
Event opportunities occur every 38–62 seconds with a 75% chance. A powered
terminal, extraction-unit restart and shutter actuation form the current event
bank. Events do not repeat immediately. Event selection pauses during boot and
loading; a running event finishes and restores its own state.

## Inspector controls

- `UI/MenuPrototypeSettings.asset`: boot lines/timings, camera/event intervals,
  event probability, loading duration, story logs, text colors and facility data.
- `NET access node 92 / Prototype controllers` in the scene: component references.
- `SurveillanceCameraController`: feed IDs, descriptions and Camera objects.
- `Rare facility events`: event targets and hold/travel/duration values.
- Five descriptively named AudioSource children: replace clips without code edits.
- `UI/MenuTerminal.uss`: spacing, contrast, focus and hover styling.
- `VFX/SurveillanceVolume.asset`: grading, vignette and very restrained bloom.

System Configuration adjusts ambience, feed attenuation and automatic camera
cycling for this session only. `MenuView.SetFacilityStatus(...)` can later accept
external state, including unknown status or values such as `19 / 18`. There is
no story progression or network connection behind this API yet.

## Code responsibilities

| Script | Responsibility |
| --- | --- |
| MainMenuController | State machine, modal actions, simulated handoff, safe disconnect |
| MenuView | UI Toolkit elements, focus, controls, styles and presentation |
| MenuBootSequence | Short startup and skip behavior |
| SurveillanceCameraController | Active feed and brief signal interruption |
| FacilityEventController | Rare-event scheduling and extensible event bank |
| FacilityEvent + RelayLightEvent / MachineryEvent / ShutterEvent | Individual environment effects and cleanup |
| LoadingScreenController | Transfer animation and completion/return flow |
| SystemLogController | Sequential access logs and varied internal records |
| MenuAudioController | Layered sources and local volume |
| MenuPrototypeSettings | Inspector-editable timing and content |
| Editor/MenuPrototypeBuilder | Reproducible geometry, assets and scene generation |

All runtime code is in the isolated `NetRunner.MenuPrototype` assembly. Editor
authoring and PlayMode tests have separate assemblies. The builder is available
from **NET.runner > Menu Prototype > Create or rebuild standalone scene**. It
replaces this prototype scene; preserve manual scene edits before rebuilding.
Existing settings, WAVs, texture, theme and volume assets are retained. The
builder never changes Build Settings and never saves gameplay scenes on its own.

## Verification and handoff

`Tests/PlayMode/MenuPrototypeTests.cs` covers boot, skip, focus/submit events,
both loading entries, double-click rejection, modal/back behavior, sliders and
camera toggle, varied logs, alternative facility data, all environmental events,
natural camera switching, safe editor disconnect and scene-unload cleanup.
Only the existing Unity AI editor package's exact cloud-account timeout warning
is accepted by the tests; other unexpected messages fail the run.

Run through Test Runner or from a connected editor:

```text
unity command run_tests --mode playmode --filter NetRunner.MenuPrototype --async_tests true
unity command test_status
```

See the repository-root `MAIN_MENU_PROGRESS.md` for the actual test results and
review images. Real save data, asynchronous scene loading, persisted settings,
story-state binding, rebinding and authored audio remain later integration work.
