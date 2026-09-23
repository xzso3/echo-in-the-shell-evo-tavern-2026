# Repository Guidelines

## Project Structure & Module Organization

- `Assets/NativeGame/` contains the current playable C# components, prefabs, UI, audio, and Editor authoring tools. Start with its `README.md` and `Assets/Scenes/NativeDemo.unity`.
- `Assets/EchoFramework/` separates Core contracts, Gameplay contracts, UnityHost, Editor adapters, and shared contract tests through assembly definitions. Keep Core and Gameplay independent of Unity and prototype assemblies.
- `Assets/PlagueSurvivor/` and `Assets/CyberCity/` contain earlier prototypes; shared artwork lives under `Assets/Art/`.
- `Tests/EchoFramework/` contains external .NET hosts, JSON fixtures, and verification scripts; `Tools/EchoContent/` contains tooling contracts. `Docs/README.md` indexes design and implementation records.

## Build, Test, and Development Commands

Use Unity **2021.3.27f1c2**, URP **12.1.12**, and .NET SDK **8.0.413** (`global.json`). Run shell commands from the repository root.

- Open `NativeDemo.unity` and press Play, or use **Echo → Native → Open Playable**. Do not rerun one-time scene creation or migration menus on the saved scene.
- `sh Tests/EchoFramework/Contracts/verify.sh` builds the contract host, runs shared checks, regenerates contract schemas, and performs Python static validation. Requires Python 3 and Unity's Newtonsoft.Json DLL; set `ECHO_JSON_DLL` if automatic discovery fails. Review generated changes before committing.
- `"$UNITY_EDITOR" -batchmode -nographics -projectPath "$PWD" -executeMethod Echo.Editor.Contracts.ContractSmokeEntry.Run -logFile /tmp/echo-contract-smoke.log` runs the same checks in Unity. Set `UNITY_EDITOR` to the pinned Editor executable; coordinate Editor access and close this checkout's Editor first.
- Player builds use Unity Build Settings; the documented Windows workflow is manual.

## Coding Style & Naming Conventions

Follow neighboring C# files: four-space indentation, namespace blocks, PascalCase types/methods/properties, and camelCase fields/locals. Match MonoBehaviour filenames to class names. External projects enforce C# 9 and warnings as errors; no dedicated formatter is configured. Contract JSON uses snake_case. Preserve Unity `.meta` files and GUIDs when moving assets.

## Testing Guidelines

The contract harness uses custom assertions and JSON fixtures named `A03.unknown_field.json`, for example. Unity Test Framework 1.1.33 is installed; no numerical coverage threshold is configured. Add regression cases for changed contracts. Record commands, results, and untested scope; Editor smoke checks do not establish Play Mode, visual, or Windows player correctness.

## Commit & Pull Request Guidelines

History mixes imperative subjects with scoped prefixes such as `feat(native):` and `docs(native):`; prefer the scoped form. Keep commits focused. PRs should explain behavior changes, link relevant issues or design documents, report verification and limitations, and include screenshots for UI or scene changes.

## Configuration & Asset Hygiene

Keep credentials server-side; `Assets/StreamingAssets/commander-proxy.json` contains client proxy configuration only. Exclude generated caches and build outputs. Coordinate shared contract changes through `Docs/Framework/Contracts/README.md`.
