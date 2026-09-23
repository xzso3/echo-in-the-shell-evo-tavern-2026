# AI-03 Support bridge integration

Attach `CommanderSupportBridge` to a scene object and assign its `level` to the active
`NativeRunController`. The bridge uses `level.rules.Support`, so the existing
`NativeEcaRules.supportComponent` must remain wired to `NativeSupportController`.
The bridge never calls `NativeNarrative.RecordSupport`; the existing
`Support.Authorized → NativeEcaRules.OnSupportAuthorized` chain owns scoring.

1. For each new snapshot, call `GetAvailableOptions(sessionId, snapshotId)`. Send
   only the returned opaque option IDs and their legal meaning to the model. This
   read-only call does not create a contract. It yields no options while a manual
   or AI contract is pending.
2. After the session layer validates `kind` and `option_id`, create a Unity-owned
   `CommanderProposal` and call `TryRegister`. A successful registration emits
   `ProposalChanged(id, Available, text)` for a chat card. Registration does not
   occupy the Support contract slot.
3. The card's “查看合同” button calls `OpenContract`. On success display
   `contractText` from `Support.StatusText` in the contract overlay. On failure
   display `reason`; a different pending contract is left intact and the card
   remains available. A stale card becomes `Expired`.
4. The overlay's “接受并执行” button calls `Accept`. Append `resultText` as a local
   fact to the conversation and refresh the game display. Success is `Executed`;
   failure is `Failed` or `Expired`. Do not send another model request.
5. “拒绝” calls `Reject`; closing the contract calls `Cancel`. Both end the card.
   `ProposalChanged` reports every state transition and its local text. `GetState`
   refreshes stale cards before returning; unknown IDs return `Expired`.

The session owner calls `Reset()` when restarting or abandoning a run. A different
`sessionId` on `GetAvailableOptions` also cancels old cards. The bridge records the
Support `PendingVersion` after opening a contract and cancels or confirms only that
version, so later manual contracts cannot be consumed by an old AI card. Its
`Update` expires cards after a run or focus change. Keep `NativeRunController.Running`
true while the phone pauses combat, as the Support and ECA authorization path both
use that live-run condition.
