# Coding standards

The one way code is written in this project. Every script follows it, and a
pull request that breaks it is fixed before merging. Where the folders are
and why: [[architecture/overview]]. Formatting that an editor can check is in
`.editorconfig` at the repository root; Rider and Visual Studio apply it.

Decided 2026-11-09 by Samuel, together with "Claude writes all code"
(`CLAUDE.md`, "How we split work").

## 1. Paradigm: rules in plain C#, thin components, content as data

- **Rules** are plain C# in `Core`: static functions or small classes with
  no `UnityEngine` dependency (the `NetRunner.Core` assembly is compiled
  with *No Engine References*, so the compiler enforces this). Same input,
  same output; no hidden state, no scene access, no `Time.deltaTime` read
  inside (pass `dt` in). Every rule has EditMode tests.
  Examples: `VerticalMotion.Step`, `Easing.SmoothStep01`; `Patrol` is a
  small class that keeps its own progress and is moved on by `Step(dt)`.
  A rule that needs random numbers is given a `System.Random`
  (`RainField`, `AmbientEvents`), so a test can pass a seeded one.
- **MonoBehaviours** are thin: read input or events, call the rules, show the
  result (move a transform, set a label, play a sound). A MonoBehaviour that
  starts doing a second job is split (#41–#45 do this for the hospital code,
  one feature each; `K7Patrol` and the atmosphere are done).
- **Content and tuning are data**: ScriptableObjects (items, enemies,
  dialogue, timings), so new content needs no new code. None exists yet
  (the menu prototype's settings, the first, were deleted in #31); the
  K-7's numbers are named constants in `Patrol` and `K7Gait` until enemies
  get their own data, and the atmosphere's in `Ambience`, `RainField`,
  `AmbientEvents` and `LightPulse` until weather does.
- **Composition, not inheritance.** Behaviour is built from several small
  components and interfaces (`IInteractable` from [#10](https://github.com/AdaM404-dev/NET.runner-92/issues/10)). A base class is used
  only for a closed family with a shared contract (none in the code yet);
  every other class is `sealed`.
- **How components find each other:** references set in the Inspector
  (or by the feature's editor builder), `GetComponent` on the same or a
  parent object, and C# `event`s for "something happened". Never
  `GameObject.Find`, `FindObjectOfType` or tag lookups at runtime, and no
  singletons or static mutable state. Tests and editor tools may use
  `Find` calls.
- **Input** comes from the Input System package. Since #31 it is the only
  input backend (Active Input Handling: Input System).
- **Layers** are named and used through `LayerMask` fields, never numbers.

## 2. Fields and access

| What | How |
| --- | --- |
| A value set in the Inspector, used only inside the class | `[SerializeField] private` |
| A value set in the Inspector that this feature's own editor builder or tests must also set | `[SerializeField] internal`, plus `InternalsVisibleTo` for exactly those assemblies (none today; a builder in another feature calls a public method instead, such as `K7Patrol.Configure`) |
| A value other code reads | a read-only property: `public float Lift => lift;` |
| State the class changes at runtime | `private` field, or a property with `private set` |
| Constants | `const` (or `static readonly` for non-primitive values), PascalCase |

No `public` mutable fields. Unity serializes fields by name, so renaming a
serialized field needs `[FormerlySerializedAs("oldName")]`, or the values
saved in scenes and prefabs are lost. Access modifiers are always written
(`private void Awake()`, not `void Awake()`). One field per declaration.

## 3. Naming

| Kind | Style | Example |
| --- | --- | --- |
| Namespaces, types, methods, properties, events, constants | PascalCase | `NetRunner.World`, `HospitalDoor`, `Toggle()`, `IsOpen`, `ActionSelected`, `Gravity` |
| Interfaces | `I` + PascalCase | `IInteractable` |
| Fields, parameters, locals | camelCase, no prefix (`m_`, `_`) | `closedRotation`, `dt` |
| Booleans | a question | `IsOpen`, `Grounded`, `canJump` |
| Events | what happened, past or present tense; handlers `On…` or `Handle…` | `ActionSelected`, `HandleAction` |
| Files | exactly the type name; one top-level type per file | `HospitalDoor.cs` |
| Tests | a sentence that states the expected behaviour | `AJumpFromTheFloorStartsUpwardsAtTheJumpSpeed` |

## 4. Order inside a class

1. nested types (enums, `[Serializable]` classes)
2. constants and static fields
3. serialized fields (`[Header]` groups allowed)
4. private state
5. events
6. properties
7. Unity messages, in the order Unity calls them: `Awake`, `OnEnable`,
   `Start`, `Update`, `LateUpdate`, `OnGUI`, `OnDisable`, `OnDestroy`
8. public methods
9. private methods

## 5. Layout

```
Assets/NETRunner/<Feature>/
├─ Scripts/            NetRunner.<Feature>.asmdef          namespace NetRunner.<Feature>
├─ Editor/             NetRunner.<Feature>.Editor.asmdef   namespace NetRunner.<Feature>.Editor
├─ Tests/EditMode/     NetRunner.<Feature>.Tests.EditMode  namespace NetRunner.<Feature>.Tests
├─ Tests/PlayMode/     NetRunner.<Feature>.Tests.PlayMode  namespace NetRunner.<Feature>.Tests
└─ Prefabs/, Scenes/, UI/, Materials/, Audio/ …            assets that belong only to this feature
```

- The folder name, assembly name, `.asmdef` file name and namespace match.
- References point towards `Core`; `Core` references no feature.
- Art shared across features stays in AdaM404's folders
  (`Assets/Characters`, `Environment`, `Materials`, `Animations`); see
  [[assets/handoff]].

## 6. Formatting and comments

- One statement per line; braces on their own lines, always, also around a
  one-line `if`. Block-scoped `namespace X { … }` (Unity compiles C# 9; the
  one-line form is C# 10). Four spaces. A blank line after a closing brace
  unless the next line is another `}` or `else`.
- Expression-bodied members (`=>`) only for one-line properties and methods.
- `var` when the type is visible on the same line (`var door = new …`,
  `GetComponent<HospitalDoor>()`), the type name otherwise.
- `using` lines: `System` first, then alphabetical.
- Every public type gets a `/// <summary>` saying what it is for; public
  members get one when the name alone does not say it. Comments explain
  *why*, not *what*.
- No magic numbers in new code: a named constant, or a serialized field when
  it is tuning. (The hospital code's hard-coded numbers, such as the
  2.65 m reach and `~(1 << 2)` masks, are fixed in #43 and #44.)

## 7. Tests

- Every `Core` rule: EditMode tests. Every component: at least one PlayMode
  test of its main behaviour, in the gameplay scene or a scene the test
  builds.
- A test's expected value comes from the specification, never copied from
  the code's own output.
- Tests do not save scenes and write files only under `Logs/`.

## 8. Bringing existing code to the standard

How #41 did it for the K-7 and #42 for the atmosphere; #43–#45 follow the
same steps.

1. **Pin today's behaviour first**: a PlayMode test of the component that
   passes on the old code (for #41, `K7PatrolTests` on a stand-in robot).
2. **Move the rules into `Core`** with EditMode tests whose numbers come
   from the system note, and let the component call them.
3. **Keep serialized field names** (or add `[FormerlySerializedAs]`) and
   move files with their `.meta`, so the scene keeps its values.
4. **Compare old and new side by side** before the pull request: a
   temporary test runs a renamed copy of the old component next to the
   new one, frame by frame, and reports the largest differences. It is not
   committed; the result goes into the system note and the pull request.
5. **Watch the maths**: a turn computed with angles instead of quaternions
   can go the other way round on an exact half turn, and a running total
   rounds differently from a product. Both showed up in #41. Unity's
   `Mathf.Sin` and `Mathf.Exp` round their argument to a `float` before
   the double-precision maths; a rule calling `Math.Sin` on a float
   expression stores the argument in a `float` first, or it differs by a
   few thousandths (#42).
6. **Engine-only functions stay in the component.** `Core` cannot call
   `Mathf.PerlinNoise`; `PracticalPulse` samples the noise and
   `LightPulse.Intermittent` shapes the sample, so the flicker is
   unchanged. Writing our own noise would have changed the look.
7. **Keep shared randomness shared.** Where the old code drew all its
   random numbers from one stream (the atmosphere's rain and events), the
   rules get the same `System.Random` and are called in the old order, so
   the side-by-side comparison matches draw for draw.
