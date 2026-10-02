# Standalone main menu prototype

Completed 2026-10-02 on `codex/main-menu-prototype`, based on `origin/main` at `8d5de4e`.

## Scope

Create `Assets/NETRunner/MainMenu/Scenes/NETRunner_MainMenu_Prototype.unity`.
This is an independent surveillance terminal prototype: no gameplay scene loads,
save integration, startup/build-list changes, or existing scene edits.

## Created

- `Assets/NETRunner/MainMenu/Scenes/NETRunner_MainMenu_Prototype.unity`:
  independent electrical service bay and network terminal.
- Original concrete/steel primitive environment, eight URP surface materials,
  a 256 px locally generated grain texture, sparse dust (35 particles maximum),
  one shadowed light, and a scene-local grading volume.
- Four viewpoints: CAM_02 service bay, CAM_04 power cabinets, CAM_07 ventilation,
  CAM_11 restricted access. One camera renders at a time; 18–27 s cycling.
- Short skippable boot, hover/focus feedback, archive placeholder, local settings
  (ambience, feed attenuation, cycling), confirmation-based disconnect.
- Both Continue and New Session demonstrate deeper network access: interface
  fade, brief signal interruption, node map, 32-segment progress, sequential
  transfer logs, varied machine archive records, 100% and black-screen handoff,
  then return to the same scene.
- Three rare events: relay/terminal power, extraction rotor restart with sound,
  and slow shutter actuation. Opportunities every 38–62 s, probability 75%.
- Five original synthesized WAV placeholders and descriptively named sources.
- Editable facility data supports later states, including `UNKNOWN` / `19 / 18`.
- Runtime, editor and test assemblies; a reproducible editor scene builder and
  an environment prefab. All new Unity assets and metadata live under the
  prototype folder. No external asset downloads or new packages.

## Scripts and configuration

`Scripts/` contains MainMenuController, MenuView, MenuBootSequence,
SurveillanceCameraController, FacilityEventController, FacilityEvent,
RelayLightEvent, MachineryEvent, ShutterEvent, LoadingScreenController,
SystemLogController, MenuAudioController and MenuPrototypeSettings.

`UI/MenuPrototypeSettings.asset` controls timing, colors, state text and logs.
`UI/MenuTerminal.uss` controls presentation. `Editor/MenuPrototypeBuilder.cs`
generates assets and saves only the new scene; it refuses unsaved current scenes.
`Assets/NETRunner/MainMenu/README.md` explains opening, tuning and extending it.

## Validation

- Imported and compiled in the installed Windows Unity 6000.6.2f1 editor.
- Final regression: **2 PlayMode tests passed, 0 failed**, 28.67 s. They exercise
  full boot and skip, UI Toolkit submit/focus
  events, archive/back, settings callbacks, both loading entries and duplicate
  click rejection, varied logs, status binding, all event effects/restoration,
  natural camera cycling, one active camera, safe editor disconnect and unload.
- The existing Unity AI editor package emits a cloud-account timeout warning;
  tests accept only that exact unrelated warning. Runtime errors still fail.
- Visual review corrected dim lighting, foreground pipe obstruction, camera
  framing and footer readability. A cleanup null-reference error and duplicate
  test-assembly references found during development were fixed.
- Final Unity reference audit: 0 missing components, 0 missing/error materials,
  0 external project-asset dependencies, 4 cameras with 1 enabled, 9 lights,
  5 AudioSources and **4,820 mesh triangles** (excluding text/particles).
- Six final screenshots were inspected at 1920 × 1080. Review images, successful
  test report and reference audit are under `docs/img/menu-prototype/`.
- Runtime assembly has no scene-loading, gameplay-controller, save or PlayerPrefs
  dependency. Existing gameplay scenes, startup list, packages and settings are
  excluded from the prototype changes.

## Known limits

- This is an intentionally small primitive-built art prototype; authored audio,
  bespoke industrial fonts and final environment art can replace placeholders.
- Current UI was designed for landscape desktop displays; dedicated mobile or
  unusual aspect-ratio layouts are not supplied.
- Settings are session-local; no saves or actual network/gameplay access exist.
- No standalone player build or manual gamepad test has been performed.
- Runtime behavior outside this isolated scene is intentionally untested.

## Review files

- [Main menu / service bay](docs/img/menu-prototype/menu-final-cam02.png)
- [Power distribution](docs/img/menu-prototype/menu-final-cam04.png)
- [Ventilation machinery](docs/img/menu-prototype/menu-final-cam07.png)
- [Restricted access](docs/img/menu-prototype/menu-final-cam11.png)
- [Network transfer](docs/img/menu-prototype/menu-loading.png)
- [System configuration](docs/img/menu-prototype/menu-configuration.png)
- [PlayMode results](docs/img/menu-prototype/playmode-results.json)
- [Reference audit](docs/img/menu-prototype/reference-audit.json)

## Intentionally deferred

Gameplay integration, real save slots, persisted settings, actual asynchronous
scene loading, story progression, localization and controller rebinding.
