# Local support component (U4)

Add exactly one `Echo.NativeGame.NativeSupportController` to the active native run scene, and bind its `level` and concrete `boss` fields. No prefab or Editor generator is required. Main owns phone UI/input, ECA subscriptions, Narrative synchronization and memory-sharing records. The component does not poll input, access Narrative private state, or update quest/endings.

Dependency: exact U3 `9e2a7874390e3166459fbc39cfb38204989ecb59` plus interface `90a3ab9b7c67a6b45161bff00a6803297a7a08ab` (local cherry-pick `071d4c7`). Main already has the interface; cherry-pick only the implementation commit.

## Binding and transaction

Subscribe ECA to `Authorized` before enabling user confirmation. Call `Request(kind, tier, sharedMemory)` with the actual user selection; show `StatusText` and `HasPending`. Request never applies an effect. A second request while pending returns false and preserves the visible contract. UI should cancel before switching contracts. Main isolates the input that opened/requested support from Enter/button confirmation.

`Confirm()` revalidates the running level, current living player, same scene and bindings, owned selected memory, unused support kind and actual effect availability. Medical does not require a live Boss; Weakpoint does. On success it applies the effect, marks that support kind used, clears the pending contract, then raises `Authorized` once with exactly the selected Kind/Tier/SharedMemory and SyncDelta. Reentrant or repeated confirmation returns false. Failed confirmation clears pending but does not consume the support; a fresh valid request can retry. Cancel and component disable clear pending without effects or authorization.

Limited authorization has +20 synchronization and Deep +45. Both grant the same concrete gameplay benefit; contract identifies limited/deep access to the one selected owned memory. Narrative/ECA owns the corresponding authorization record and threshold-60 consequences. This component neither performs network transmission nor selects/shares additional memories.

## Actual effects

- Medical: `NativePlayer.TryHeal(40)` returns true only when it actually increases health, capped at maximum. Full health, dead player and non-running level reject. The contract previews the current amount; confirmation computes actual available healing again. Success text reports the actual restored amount.
- Weakpoint: `NativeBossController.TryEnableWeakpointSupport()` adds exactly 3 seconds to the active core window, if open, and every future core window in this encounter. It works during any activated undefeated attack stage, including before the shell breaks. Countdown, expiration and E eligibility all use the extended duration. It does not open a window immediately, break shell, or defeat the Boss. Dormant, disabled, defeated or already-enhanced Boss rejects. The Boss itself prevents repeated enhancement.

Each support kind succeeds once for this scene/run instance, regardless of tier or memory. Retry uses main's fresh-scene restart. No persistent inventory, reset API or rollback system is introduced.

## Validation

Source/reference review and whitespace diff check only. Unity compilation, Play Mode and UI input behavior are **not_run** in this support task: main owns the Unity slot and performs one integrated compilation. No independent .NET tests, test harness, installations or Windows build were run. Main should bind the single component, subscribe ECA and verify actual health/window changes and corresponding authorization record during its normal U4 play-through.
