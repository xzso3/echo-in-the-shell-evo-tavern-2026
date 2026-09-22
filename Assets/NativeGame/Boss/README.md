# Native development Boss

This is a development placeholder, not the canonical WARDEN-01. It reuses existing BossArena sprite/material assets and line/explosion presentation helpers. It does not instantiate the old BossArena encounter or use its origin-centred bounds, player, or progression.

## Main-line integration

Dependency: native base `91ca8556cbe7239bee3a3e9d668a95293de18d53` and interface commit `4191c872b65816f47dca20694ffe0091953f5810` (locally cherry-picked as `437416f`). Cherry-pick the implementation commit only if the interface already exists.

In the main owner's Unity slot, run **Echo > Native > Create Development Boss Prefab**, or call:

```csharp
var asset = Echo.NativeGame.Editor.NativeBossPrefabBuilder.CreatePrefab();
var instance = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(asset);
instance.transform.position = new UnityEngine.Vector3(23, 0, 0);
var boss = instance.GetComponent<Echo.NativeGame.NativeBossController>();
boss.level = run;
```

The builder writes only `Assets/NativeGame/Boss/DevelopmentBoss.prefab`, returns an existing prefab unchanged, and does not save/open scenes. Root and all authored content use local origin `(0,0)`; main assigns world `(23,0)` exactly once. Main guarantees an obstacle-free region x15..31/y-7..7. This stationary Boss does not own arena boundaries or gates.

Scene ECA calls `ActivateEncounter()`, subscribes to `Defeated`, and owns quest/route/end-state changes. Main's E input calls `CanInteractCore(run.player)` and `InteractCore(run.player)` through `INativeBossEncounter`; the latter validates again. No Boss component polls input or changes quests, dialogue, gates, or completion. Subscribe before activation; unsubscribe during main teardown. Successful E emits `Defeated` once; repeated E returns false.

`NativeBossActor` owns shell health and final life state. `NativeCombat` selects enemy/Boss targets, controls player firing cooldown, and routes damage. `NativeProjectile` uses Unity circle sweeps for friendly/hostile shots. Controller owns attack timing/telegraphs and calls Combat for damage. Display only reads Boss state.

## Encounter and tuning

- Tracking line follows the player for 0.9s, locks white for 0.55s, then fires three projectiles 0.22s apart along the fixed line. Sidestep after lock.
- Three fixed circles are placed at the player's sampled positions 0.7s apart. Each warns for 1.25s before one collider-overlap damage check, with the same 1.05-unit radius. Leave the orange circles.
- Shell starts at 1400 health and receives 45% of ordinary bullet damage. At native 24 damage / 0.38s, about 130 hits / 49s of uninterrupted shooting break it. Dodging, initial startup and approaching the core target roughly a minute; actual gameplay duration requires main-line play validation.
- Breaking the shell cancels existing warnings/projectiles and opens a safe 6s core window. Bullets cannot win. The main E action must come from the living run player within 1.9 units, in the active window with clear line of sight.
- Missing a window resumes attacks; another window opens after 8s. Shell stays broken. No permanent miss/fail flag.
- Win clears hazards, disables the body collider, changes to the existing wreck sprite and emits the event. Player death/run stop clears hazards and stops attacks. Disabling the component clears hazards; re-enabling begins from recovery rather than firing without warning. Retry is through a fresh scene instance; there is no in-place reset API.

The Boss-owned world-space TMP/uGUI display shows shell health and attack/core cues. It has no input raycast components. Existing Pulse prefab, BossArena config/art and LiberationSans SDF font are required. Only sprites/material are read from the legacy config; native tuning lives on Boss components.

## Validation handoff

Static source/path/metadata and `git diff --check` verification only in this task. **Unity compilation, prefab generation, rendering, Play Mode and Windows build: not_run**, because the active Unity slot belongs to the main owner. No .NET tests or legacy P1 harness were run.

Main smoke acceptance: generate/instantiate at (23,0); verify dormant before ECA; activate; observe both lock/shot and fixed-circle warnings; verify shell takes friendly shots and player takes hostile damage; break shell and let a window expire; observe another window; reject distant E/dead player; nearby E succeeds exactly once and main opens route; check death/unload clears hazards. Tune health only after measuring a representative ~60s fight.
