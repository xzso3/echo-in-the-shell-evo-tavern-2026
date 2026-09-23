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
The editor and Play Mode checks still need to prove full exit coverage.

Completion uses `CombatWorld.EnemyDied` or `CombatBoss.Defeated` after core E.
Disabling, destroying or unloading a target does not count. An encounter
unlocks its doors on real completion and publishes its scoped event through
KT-06. `LevelExit` publishes ExitReached only after an UnlockExit action.
`SandboxLevelHost` is a standalone test scene host. Integrated hosts must
select an Integrated binding and never install that sandbox host.
