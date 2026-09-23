# KT-06 Binding contract

`LevelEndpointCatalog` is the authored interface for one level. Each entry has a complete
`author/work/local` `ContentIdentity`, a kind, an object path, a description, allowed modes,
and a required-binding flag. The local ID is authored once and remains stable when the
object moves or the work is re-exported. Duplicate, incomplete, wrong-work and unknown
IDs stop startup. The catalog and generated manifest contain no RunId or instance ID.

Select a catalog asset and run **Assets → Echo → Level Toolkit → Export Endpoint Manifest**
to write JSON. Replacing an older manifest reports removed endpoint IDs before export.
An interface change requires existing bindings to migrate; the exporter never creates
new IDs automatically.

Example manifest shape (generated values come from the catalog asset):

```json
{
  "levelId": "sample/archive/level_01",
  "endpoints": [
    {"id":"sample/archive/archive_terminal","kind":"InteractionConfirmed","objectPath":"Level/ArchiveTerminal","description":"Read archive","requiredBinding":true,"allowedModes":"Both"},
    {"id":"sample/archive/boss_encounter","kind":"EncounterCompleted","objectPath":"Level/BossEncounter","description":"Boss defeated","requiredBinding":true,"allowedModes":"Both"},
    {"id":"sample/archive/test_exit","kind":"ExitReached","objectPath":"Level/Exit","description":"Preview exit","requiredBinding":false,"allowedModes":"Sandbox"},
    {"id":"sample/archive/open_exit","kind":"UnlockExit","objectPath":"Level/Exit","description":"Unlock preview exit","requiredBinding":true,"allowedModes":"Sandbox"}
  ]
}
```

Host sequence:

1. Create a fresh `RunId` per run and a `RuntimeScope` per placed level instance.
   Construct one immutable `LevelRunBindingPolicy(runId, LevelRunMode.Sandbox)` or
   `LevelRunBindingPolicy(runId, LevelRunMode.Integrated)` before the run starts,
   and share it across every instance in that run.
2. Create `LevelInstanceContext`, then `LevelBindingSession(context, catalog, policy)`.
   Register action targets before
   `TryStart`, or in the chosen `ILevelBinding.Bind`. Targets own encounter, Map door,
   interaction and exit state. Their `Execute` method checks current conditions and
   returns a `LevelActionResult`.
3. Call `TryStart` with exactly one binding. A failed result stops level activation;
   the session is disposed. Integrated mode requires an Integrated binding and every
   required endpoint allowed in that mode must have a subscriber or target. No Sandbox
   fallback exists. Install `LevelEventEmitter` on real source components and call
   `Raise` only after the physical or gameplay fact actually occurs. Bind each emitter
   with the owning instance's scope; disabling and re-enabling an object keeps its
   binding, while unloading explicitly unbinds it.
4. On level unload, call `Dispose` and unbind emitters. Old Run and other instance
   events/actions are rejected even if they share the same authored content ID.

`SandboxBindingDefinition` lists required `EncounterCompleted` endpoints, an
`ExitReached` test endpoint, optional region-to-`ActivateEncounter` triggers,
placeholder interaction endpoints, and an optional `UnlockExit` action. Its
`SandboxBinding` only reports preview feedback. It never calls a whole-run ending or
writes Native Quest/Narrative. In Integrated mode, a Native adapter implements
`ILevelBinding` and uses these same event/action APIs to update the real owners.

Consumer migration request: KT-05 should call emitter `Raise` from verified region,
interaction, real encounter completion and exit components; Map/Combat/Level should
implement `ILevelActionTarget` without moving their state into Binding. NB-03 should
provide the Integrated binding, subscribe to required facts, and route formal
Quest/Narrative/Dialogue writes in `Assets/NativeGame/ToolkitIntegration/`. INT-LT
owns any shared Foundation/API adjustment and the S1/S4 Unity checks.
