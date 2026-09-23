# NB-02 Native input handoff

`NativeHud` keeps the saved `NativeDemo` array and ECA core path until an instance is explicitly registered. Registration then selects targets only from the active `RuntimeScope` (RunId + LevelInstanceId). Reachability is checked on every E press and prompt update. Candidates are ordered by squared physical distance, then core before ordinary interaction on an exact tie, then registration order. The caller should register child components in a stable hierarchy order.

## NB-01 / INT-LT wiring

After `NativeLevelHost.Mount` succeeds, use the mounted instance's own `Session.Context`, player, weapon and placement root:

```csharp
var scope = instance.Scope;
if (!hud.RegisterToolkitInputInstance(instance.Session.Context, instance.Player,
        instance.Placement.Weapon, instance.Placement.transform))
    throw new InvalidOperationException("Native input instance registration failed.");
foreach (var interaction in instance.Placement.GetComponentsInChildren<NativeScopedInteraction>(true))
    if (!hud.RegisterInteraction(scope, interaction))
        throw new InvalidOperationException("Native interaction registration failed.");
foreach (var boss in instance.Placement.GetComponentsInChildren<CombatBoss>(true))
    if (!hud.RegisterBoss(scope, boss))
        throw new InvalidOperationException("Native boss registration failed.");
```

Place `NativeScopedInteraction` with its `LevelEventEmitter` on each E interaction or exit endpoint. The component checks radius and `CombatSight` before publishing the bound endpoint with the focused `CombatPlayer` as actor. Boss cores use `CombatBoss.CanInteractCore` and `InteractCore` directly, so the real core window, radius and sight check remain authoritative. The instance's binding handles its `EncounterCompleted` event; Native ECA's fixed Boss is used only in the old scene.

After every successful `host.Focus(scope)`, call `hud.SetActiveInputInstance(scope)`. The first Mount focuses automatically, so do this in the `Mounted` callback too. `NativeLevelHost` currently has no focus event; the caller that invokes `Focus` must pair these calls. When no instance is focused, call `hud.ClearActiveInputInstance()`. In the host's `Unmounted` callback, call `hud.UnregisterInputInstance(instance.Scope)`; this clears the old player's movement and prevents a pending phone contract from applying to another instance. If another instance is automatically focused by the host during unmount, set the HUD scope to that instance after unregistering the old one. The focused instance owns WASD, Space, E, health and kill display. NativePlayer movement is zero while a toolkit player is focused; never put NativePlayer and another CombatPlayer on the same Rigidbody2D.

`NativeSupportController` reads the HUD's focused player and the nearest eligible registered Boss. It captures the RunId, LevelInstanceId, player and Boss at Request. Confirm rejects a different or invalid target, and Update clears a stale pending contract. Medical healing and weakpoint timing apply to the focused toolkit actor only; the existing phone confirmation frame guard and successful `Authorized` event remain in place.

## Verification boundary

This branch was compiled offline against Unity 2021.3.27f1c2 managed assemblies and the generated Native/Toolkit assemblies. It has not been imported or played in Unity. INT-LT owns the scene/Prefab save and the S1/S4 checks, including two reachable Bosses, focus switching, instance unload during a pending support contract, text input focus, same-frame Enter protection, and an old `NativeDemo` short path.
