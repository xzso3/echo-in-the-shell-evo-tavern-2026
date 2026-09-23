# Foundation API v0

Assembly: `Echo.LevelToolkit.Runtime`; namespace: `Echo.LevelToolkit.Foundation`.

| API | Owner and consumer |
| --- | --- |
| `ContentIdentity(authorId, workId, localId)` | Authored asset identity. KT-03 uses it on Chunk prefabs; KT-05 uses it on level definitions; KT-08 writes it into manifests. Keep IDs stable across re-export. |
| `RunId.New()` | Run host creates once per run. A restart creates a new ID. |
| `RuntimeScope.New(runId, content)` | Host creates once per placed level instance. It generates a distinct `LevelInstanceId`, even when content is identical. KT-02/05/06 route runtime facts by both IDs. Never save these IDs into a prefab or ScriptableObject. |
| `ILevelRunContext` and `LevelInstanceContext` | Preview host or Native adapter owns `IsRunning`, `Player`, and player-death handling. Consumers receive the context explicitly during initialization; they never search for an arbitrary host. KT-02 can add combat-specific interfaces in its own module. |
| `ChunkDefinition`, `EdgePort`, `ChunkPlacement` | KT-03 attaches the definition to each prefab root and places references at integer tile origins. Each side has zero or one port; open intervals are `[offset, offset + width)`. North/south offsets count left to right; east/west count bottom to top. Size is 3–32. `HasValidMetadata` checks metadata only, not geometry or physical clearance. |
| `StandardPlayerProfile` | KT-00/02 populate an asset from the saved standard player prefab and record its revision/fingerprint. `IsReady` remains false until all source values are verified. Author works cannot change it; actor runtime state lives on actor instances. |
| `ToolkitVersion` / `PackageVersionRequirement` | KT-08 authors the release asset with exact Unity, URP, template, content-format, and UPM package versions. `MatchesExactly` rejects incomplete or different declarations until compatibility is demonstrated. This type alone does not install UPM packages or inspect the current environment. |
| `ValidationIssue` / `ValidationReport` | KT-03/04/08 produce one report model for Inspector and batch output. Code is stable; source, cell and side locate issues. Report instances are owned by each validation call. |

Serialization and migration: only public Unity fields shown in Inspector through private `[SerializeField]` backing fields are persistent. Existing Native components and GUIDs are untouched. Consumers add new components/assets rather than moving old scripts. Changing `ContentIdentity` or an endpoint local ID is an authored interface change that requires migration; `RunId` and `LevelInstanceId` are never migrated. No world-unit cell size is frozen here; KT-00 must verify the proposed 1-unit cell and standard player values before KT-03/02 use them. No asset instance is shipped by this task.

Dependency boundary: the assembly references UnityEngine only. It does not reference NativeGame, old prototypes, UnityEditor, or EchoFramework. KT-02/03/06 may add source under `Runtime/` and compile into this assembly, or create assemblies that reference this one without modifying Foundation. If a shared signature must change, send a proposal to the KT-01 owner and coordinate consumer migration through the main controller.
