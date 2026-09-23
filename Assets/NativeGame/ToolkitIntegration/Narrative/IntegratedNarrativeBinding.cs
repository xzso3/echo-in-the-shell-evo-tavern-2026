using System;
using Echo.LevelToolkit.Binding;
using Echo.LevelToolkit.Foundation;

namespace Echo.NativeGame.ToolkitIntegration.Narrative
{
    // Only these existing Native actions may be explicitly attached to a foreign
    // encounter fact. The original boss, bypass, relay and ending are never mapped.
    public enum NativeNarrativeMappedAction { None, PreserveAnomaly, RewriteEcho }

    // One required, real EncounterCompleted fact -> Quest objective -> Narrative fact
    // -> optional whitelisted Native action -> authored UnlockExit action.
    // The host supplies identities from its validated catalog and installs this as
    // the single Integrated binding for this placed level instance.
    public sealed class IntegratedNarrativeBinding : ILevelBinding
    {
        private readonly NativeQuest quest;
        private readonly NativeNarrative narrative;
        private readonly ContentIdentity encounterCompletedId;
        private readonly ContentIdentity semanticFactId;
        private readonly ContentIdentity unlockExitId;
        private readonly string objectiveLabel;
        private readonly string factText;
        private readonly NativeNarrativeMappedAction nativeAction;
        private LevelBindingSession session;

        public LevelRunMode Mode => LevelRunMode.Integrated;

        public IntegratedNarrativeBinding(NativeQuest quest, NativeNarrative narrative,
            ContentIdentity encounterCompletedId, ContentIdentity semanticFactId,
            ContentIdentity unlockExitId, string objectiveLabel, string factText,
            NativeNarrativeMappedAction nativeAction = NativeNarrativeMappedAction.None)
        {
            this.quest = quest;
            this.narrative = narrative;
            this.encounterCompletedId = encounterCompletedId;
            this.semanticFactId = semanticFactId;
            this.unlockExitId = unlockExitId;
            this.objectiveLabel = objectiveLabel;
            this.factText = factText;
            this.nativeAction = nativeAction;
        }

        public void Bind(LevelBindingSession bindingSession)
        {
            if (bindingSession == null) throw new ArgumentNullException(nameof(bindingSession));
            if (session != null) throw new InvalidOperationException("Binding already installed.");
            if (bindingSession.Mode != LevelRunMode.Integrated || !quest || !narrative)
                throw new InvalidOperationException("Integrated binding requires Native Quest and Narrative.");
            if (quest.narrative != narrative)
                throw new InvalidOperationException("Quest must use the supplied Narrative owner.");
            if (string.IsNullOrWhiteSpace(objectiveLabel) || string.IsNullOrWhiteSpace(factText))
                throw new InvalidOperationException("Objective label and narrative fact text are required.");
            if (nativeAction != NativeNarrativeMappedAction.None
                && nativeAction != NativeNarrativeMappedAction.PreserveAnomaly
                && nativeAction != NativeNarrativeMappedAction.RewriteEcho)
                throw new InvalidOperationException("Unknown Native narrative action.");

            RuntimeScope scope = bindingSession.Context.Scope;
            RequireEndpoint(bindingSession, encounterCompletedId,
                LevelEndpointKind.EncounterCompleted, true);
            RequireEndpoint(bindingSession, unlockExitId, LevelEndpointKind.UnlockExit, true);
            if (!new NativeLevelSourceKey(scope, semanticFactId).IsValid)
                throw new InvalidOperationException("Narrative semantic ID must belong to this work.");

            // Subscribe first: if registration fails, TryStart disposes the session
            // and no objective has been added to Native Quest.
            bindingSession.Subscribe(encounterCompletedId, OnEncounterCompleted);
            if (!quest.TryRegisterRequiredLevelObjective(bindingSession,
                encounterCompletedId, objectiveLabel))
                throw new InvalidOperationException("Required Native Quest objective could not be registered.");
            session = bindingSession;
        }

        private void OnEncounterCompleted(LevelLocalEvent fact)
        {
            if (session == null || !session.IsActive || fact.Scope != session.Context.Scope
                || !fact.EndpointId.Equals(encounterCompletedId)
                || fact.Kind != LevelEndpointKind.EncounterCompleted)
                throw new InvalidOperationException("Encounter fact does not belong to this binding.");

            // A later action can fail and KT-06 will retry only this subscriber with
            // the original event sequence. Both state writes remain idempotent.
            if (!quest.HasCompletedLevelObjective(fact.Scope, encounterCompletedId)
                && !quest.RecordVerifiedLevelObjective(session, fact))
                throw new InvalidOperationException("Native Quest rejected the verified objective.");
            if (!narrative.HasLevelFact(fact.Scope, semanticFactId)
                && !narrative.RecordLevelFact(fact.Scope, semanticFactId, factText))
                throw new InvalidOperationException("Native Narrative rejected the level fact.");
            ApplyExplicitNativeAction();

            if (!quest.HasCompletedLevelObjective(fact.Scope, encounterCompletedId)
                || !narrative.HasLevelFact(fact.Scope, semanticFactId))
                throw new InvalidOperationException("Required story condition is not satisfied.");
            LevelActionResult result = session.Execute(new LevelActionCommand(
                fact.Scope, unlockExitId, LevelEndpointKind.UnlockExit));
            if (result != LevelActionResult.Success && result != LevelActionResult.AlreadySatisfied)
                throw new InvalidOperationException($"UnlockExit failed: {result}.");
        }

        private void ApplyExplicitNativeAction()
        {
            if (nativeAction == NativeNarrativeMappedAction.None) return;
            if (nativeAction == NativeNarrativeMappedAction.PreserveAnomaly)
            {
                if (!narrative.PreservedAnomaly && !narrative.PreserveAnomaly())
                    throw new InvalidOperationException("PreserveAnomaly condition is not satisfied.");
            }
            else if (nativeAction == NativeNarrativeMappedAction.RewriteEcho
                && !narrative.RewroteEcho && !narrative.RewriteEcho())
                throw new InvalidOperationException("RewriteEcho condition is not satisfied.");
        }

        private static void RequireEndpoint(LevelBindingSession bindingSession,
            ContentIdentity id, LevelEndpointKind kind, bool required)
        {
            if (!new NativeLevelSourceKey(bindingSession.Context.Scope, id).IsValid
                || !bindingSession.Catalog.TryGet(id, out var endpoint)
                || endpoint.Kind != kind || !endpoint.Allows(LevelRunMode.Integrated)
                || required && !endpoint.RequiredBinding)
                throw new InvalidOperationException($"Required Integrated {kind} endpoint is invalid: {id}.");
        }
    }
}
