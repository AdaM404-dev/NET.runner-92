using System.Runtime.CompilerServices;

// The menu's editor builder wires the scene and the PlayMode tests read and
// swap its references, so both may use the [SerializeField] internal fields.
// No other assembly can (docs/architecture/coding-standards.md, section 2).
[assembly: InternalsVisibleTo("NetRunner.MainMenu.Editor")]
[assembly: InternalsVisibleTo("NetRunner.MainMenu.Tests.PlayMode")]
