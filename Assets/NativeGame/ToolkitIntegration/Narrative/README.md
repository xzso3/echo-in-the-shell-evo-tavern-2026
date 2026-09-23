# NB-03 Native narrative binding handoff

`IntegratedNarrativeBinding` handles one required `EncounterCompleted` endpoint in one placed
level instance. Its constructor receives the Native Quest/Narrative owners, the stable
encounter endpoint ID, a distinct stable semantic fact ID from the same work, the
`UnlockExit` endpoint ID, and human-facing objective/fact text. The optional
`NativeNarrativeMappedAction` defaults to `None`; only `PreserveAnomaly` and
`RewriteEcho` are permitted. Both still check their original memory prerequisites.
Foreign encounter completion never calls `RecordBossDefeated`, `RecordBypass`,
`RecordRelay`, or `CommitEnding`.

The host creates a `LevelBindingSession` with `LevelRunMode.Integrated`, registers a
real `ILevelActionTarget` for `UnlockExit` before `TryStart`, then passes this binding
to `TryStart`. The catalog must mark both endpoints as required Integrated bindings.
The host binds an emitter to the same `RuntimeScope` and raises the encounter endpoint
only after verified real defeat/core interaction. On `HandlerFailed`, retry that same
verified completion while the instance is active. Dispose the session and unbind
emitters at unload. A failed `TryStart` stops level activation.

Quest state API:

```csharp
bool TryRegisterRequiredLevelObjective(LevelBindingSession session,
    ContentIdentity endpointId, string label);
bool RecordVerifiedLevelObjective(LevelBindingSession session, LevelLocalEvent fact);
bool HasCompletedLevelObjective(RuntimeScope scope, ContentIdentity endpointId);
int RequiredLevelObjectiveCount { get; }
int CompletedLevelObjectiveCount { get; }
bool AllRequiredLevelObjectivesCompleted { get; }
```

Narrative state API:

```csharp
bool RecordLevelFact(RuntimeScope scope, ContentIdentity semanticId, string text);
bool HasLevelFact(RuntimeScope scope, ContentIdentity semanticId);
int LevelFactCount { get; }
```

Both stores use `NativeLevelSourceKey`: authored work identity plus RunId and placed
LevelInstanceId. Quest only accepts active Integrated session events that match its
registered required endpoint. Replays settle once. `CanLeaveSector` includes all
registered required level objectives; with no foreign objective, its original rule
is unchanged. External facts never change Sync, Difference, memory count, original
boss/bypass state, or ending eligibility by default. An explicitly configured
`PreserveAnomaly` or `RewriteEcho` action may change Difference through the existing
Native prerequisite checks.

NB-01 host needs to expose its run context, exact instance scope and validated
catalog, install the Integrated session, register the exit target, and route verified
`EncounterCompleted` through `LevelEventEmitter.Raise`. KT-07 should configure a
sample catalog and show the same fact reaching Quest, Narrative, then `UnlockExit`.
The original final choice remains exclusively in Native ECA after its own conditions.
