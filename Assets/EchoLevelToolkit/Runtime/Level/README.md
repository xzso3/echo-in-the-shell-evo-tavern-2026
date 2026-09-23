# KT-05 level runtime

`LevelStage` owns one placed level instance. Give `CombatWorld` and
`LevelBindingSession` the same `LevelInstanceContext`. Before starting the
binding session, call `LevelStage.Prepare(world, session)`. After `TryStart`
succeeds, call `LevelStage.StartBound()`. Call `Dispose()` on unload. A new run
must create a new RunId, scope, binding session and level scene or prefab;
reusing a defeated actor cannot restore destroyed enemies.

Each `LevelEncounter` uses a PolygonCollider2D trigger, which may cover cells
from several Chunk prefabs. Assign its local encounter identity, RegionEntered
event, and for clear/Boss encounters an ActivateEncounter action and
EncounterCompleted event. Assign actual CombatEnemy references or one
CombatBoss. Clear/Boss enemies begin dormant. Free-combat enemies wake after
the binding starts and are optional by default. Non-sealing encounters leave
the door list empty. A sealed encounter lists every relevant MapDoor identity;
the spatial coverage and remaining standing space are checked by KT-04.

The MapDoorSet alone stores requested open state and encounter locks. A door
has one reserving encounter in the first version. `SetDoorOpen` can request a
door state, but the physical blocker stays closed while combat owns a lock.
At activation, the player's Collider2D bounds must not touch a closing door.
Real encounter completion opens its reserved doors; later story actions may
request a different state.
The editor and Play Mode checks still need to prove full exit coverage.

Completion uses `CombatWorld.EnemyDied` or `CombatBoss.Defeated` after core E.
Disabling, destroying or unloading a target does not count. An encounter
unlocks its doors on real completion and publishes its scoped event through
KT-06. `LevelExit` publishes ExitReached only after an UnlockExit action.
`SandboxLevelHost` is a standalone test scene host. Integrated hosts must
select an Integrated binding and never install that sandbox host.

The Editor menu `Echo > Level Toolkit > Generate KT-05 Clear Slice` creates a
small independent scene with a cross-Chunk clear encounter, two Map doors and
an exit. `Generate KT-05 Full Sample` adds optional free combat and a Boss
whose core must be confirmed with E. Each menu creates a fresh prefab-backed
scene under `Assets/EchoLevelToolkit/Samples/KT05`; it refuses to overwrite an
existing scene or prefab. The host uses a fresh prefab instance on R, so the
sample does not require a Build Settings entry to restart.

The LevelStage Inspector exposes `Validate encounter configuration`. It checks
IDs, target sharing, region geometry and door lock ownership. For KT-08's
full-level content hook, call
`LevelStageExportValidation.ValidateContentPaths(assetPaths)` and return its
diagnostics. Diagnostic codes begin with `KT05_`. Single-resource exports
should skip this callback. Region exit coverage and continuous player
clearance still require KT-04's validation and the short Play Mode flow.
