# KT-08 packaging handoff

Base: `c1a995e07ee967107d1b1940dc5f12272d326dc8`; branch: `codex/kt-08-packaging`.

## Callable surfaces

- `PackagePolicy.Scan(authorId, workId, roots)` classifies the actual `AssetDatabase.GetDependencies` closure as work owned, toolkit shared, Unity/UPM, or undeclared external. Work roots must be under `Assets/EchoUserContent/<author>/<work>/`; executable files and undeclared external assets block export.
- `WorkPackageExporter.BuildManifest(...)`, `RecordHumanAcceptance(...)`, and `Export(...)` produce explicit work packages. The package has one embedded JSON manifest, exact version and package whitelist, path/GUID/content/meta digests, shared dependency declarations, content fingerprint, endpoint manifest for a Level, and either matching human evidence or an explicit internal candidate status.
- `PackagePreflight.Inspect(packagePath)` reads the archive without importing it and reports errors/changes/update consent. `WorkPackageReceiver.ImportValidated(packagePath, acceptExistingWorkUpdate, acceptInternalCandidate)` repeats the check immediately before import. The receive window never enters Play mode, opens a scene, inserts into a map, or writes narrative state.
- `ToolkitPackageExporter.ExportTo(outputPath)` exports an explicit toolkit asset list and writes `<output>.manifest.json` with file/folder hashes, package hash and version requirements. It includes `SkillSources/` and `Editor/Skills/` once integrated, and excludes generated work manifests and acceptance records. It never uses `IncludeDependencies`.

Menus: `Echo/Level Toolkit/Export Toolkit Package`, `Export Work Package`, and `Receive Work Package`.

## Interface needed from KT-04 / KT-05

Register `PackageContentValidation.ValidateContent` with a synchronous `Func<WorkPackageKind, string[], IReadOnlyList<string>>`. Return concrete errors for Chunk/Enemy/Boss/Level, including missing level validation spec, map/door/encounter validation, interface IDs, and test binding. Formal export fails closed until registered. Internal S3 candidate still needs base root, dependency, identity, package and endpoint catalog checks; it is visibly unaccepted.

The current `ToolkitVersion.asset` declares toolkit `0.1.0-dev.1`, content format `1`, Unity `2021.3.27f1c2`, URP `12.1.12`, and exact Tilemap, Tilemap Extras, URP, TMP and uGUI packages. If the actual toolkit dependency scan discovers another UPM package, update this profile deliberately before export and reaccept work whose version fingerprint changed.

## Checks and remaining gate

- Offline Roslyn C# 8 compilation against Unity 2021.3.27f1c2 managed assemblies passed for all current toolkit Runtime sources and all KT-08 Editor sources. This checks syntax and referenced APIs only; it is not a Unity domain reload or package round trip.
- `git diff --check` passed. GUID/meta uniqueness and exact package contents should be checked after integration.
- Unity Editor, import/export, Play mode and the clean project S3 round trip were not run in this worktree because Unity is reserved for INT-LT. S3 must inspect actual exported asset/folder entries, then import the toolkit and one full candidate work package in a clean matching project. It must verify Skill source retention and explicit installer discovery with KT-09. Formal human acceptance was not recorded.
