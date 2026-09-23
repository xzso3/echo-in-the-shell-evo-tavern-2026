using System;
using System.Collections.Generic;
using Echo.LevelToolkit.Foundation;
using UnityEngine;

namespace Echo.LevelToolkit.Binding
{
    [Serializable]
    public struct SandboxEncounterTrigger
    {
        [SerializeField] private ContentIdentity regionEnteredEndpoint;
        [SerializeField] private ContentIdentity activateEncounterEndpoint;

        public ContentIdentity RegionEnteredEndpoint => regionEnteredEndpoint;
        public ContentIdentity ActivateEncounterEndpoint => activateEncounterEndpoint;
    }

    [CreateAssetMenu(menuName = "Echo/Level Toolkit/Sandbox Binding")]
    public sealed class SandboxBindingDefinition : ScriptableObject
    {
        [SerializeField] private ContentIdentity[] requiredEncounterEvents =
            Array.Empty<ContentIdentity>();
        [SerializeField] private ContentIdentity testExitEvent;
        [SerializeField] private ContentIdentity unlockExitAction;
        [SerializeField] private ContentIdentity[] placeholderInteractionEvents =
            Array.Empty<ContentIdentity>();
        [SerializeField] private SandboxEncounterTrigger[] encounterTriggers =
            Array.Empty<SandboxEncounterTrigger>();

        public IReadOnlyList<ContentIdentity> RequiredEncounterEvents =>
            requiredEncounterEvents ?? Array.Empty<ContentIdentity>();
        public ContentIdentity TestExitEvent => testExitEvent;
        public ContentIdentity UnlockExitAction => unlockExitAction;
        public IReadOnlyList<ContentIdentity> PlaceholderInteractionEvents =>
            placeholderInteractionEvents ?? Array.Empty<ContentIdentity>();
        public IReadOnlyList<SandboxEncounterTrigger> EncounterTriggers =>
            encounterTriggers ?? Array.Empty<SandboxEncounterTrigger>();

        public SandboxBinding CreateBinding(ISandboxFeedback feedback)
        {
            return new SandboxBinding(this, feedback);
        }
    }

    // Feedback is deliberately limited to preview UI. It cannot finish a Native run.
    public interface ISandboxFeedback
    {
        void ShowPlaceholderDialogue(ContentIdentity interactionEndpoint);
        void ShowTestExitLocked(ContentIdentity exitEndpoint);
        void ShowTestLevelCompleted(ContentIdentity exitEndpoint);
        void ReportActionFailure(ContentIdentity actionEndpoint, LevelActionResult result);
    }

    public sealed class SandboxBinding : ILevelBinding
    {
        private readonly SandboxBindingDefinition definition;
        private readonly ISandboxFeedback feedback;
        private readonly HashSet<ContentIdentity> completedRequired = new HashSet<ContentIdentity>();
        private HashSet<ContentIdentity> required;
        private LevelBindingSession session;
        private bool testCompleted;

        public LevelRunMode Mode => LevelRunMode.Sandbox;

        public SandboxBinding(SandboxBindingDefinition definition, ISandboxFeedback feedback)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            this.feedback = feedback;
        }

        public void Bind(LevelBindingSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (this.session != null) throw new InvalidOperationException("Sandbox binding is already installed.");
            if (session.Mode != LevelRunMode.Sandbox)
                throw new InvalidOperationException("Sandbox binding cannot install in Integrated mode.");

            required = new HashSet<ContentIdentity>();
            foreach (ContentIdentity id in definition.RequiredEncounterEvents)
            {
                RequireKind(session, id, LevelEndpointKind.EncounterCompleted);
                if (!required.Add(id))
                    throw new InvalidOperationException($"Duplicate required encounter endpoint: {id}.");
                session.Subscribe(id, OnEncounterCompleted);
            }
            RequireKind(session, definition.TestExitEvent, LevelEndpointKind.ExitReached);
            session.Subscribe(definition.TestExitEvent, OnExitReached);

            foreach (ContentIdentity id in definition.PlaceholderInteractionEvents)
            {
                RequireKind(session, id, LevelEndpointKind.InteractionConfirmed);
                session.Subscribe(id, OnPlaceholderInteraction);
            }
            foreach (SandboxEncounterTrigger trigger in definition.EncounterTriggers)
            {
                RequireKind(session, trigger.RegionEnteredEndpoint, LevelEndpointKind.RegionEntered);
                RequireKind(session, trigger.ActivateEncounterEndpoint, LevelEndpointKind.ActivateEncounter);
                ContentIdentity actionId = trigger.ActivateEncounterEndpoint;
                session.Subscribe(trigger.RegionEnteredEndpoint, levelEvent =>
                    ActivateEncounter(levelEvent, actionId));
            }
            if (definition.UnlockExitAction.IsComplete)
                RequireKind(session, definition.UnlockExitAction, LevelEndpointKind.UnlockExit);
            this.session = session;
        }

        private void OnEncounterCompleted(LevelLocalEvent levelEvent)
        {
            if (!required.Contains(levelEvent.EndpointId) || !completedRequired.Add(levelEvent.EndpointId))
                return;
            if (completedRequired.Count == required.Count && definition.UnlockExitAction.IsComplete)
            {
                LevelActionResult result = session.Execute(new LevelActionCommand(
                    levelEvent.Scope, definition.UnlockExitAction, LevelEndpointKind.UnlockExit));
                if (result != LevelActionResult.Success && result != LevelActionResult.AlreadySatisfied)
                    feedback?.ReportActionFailure(definition.UnlockExitAction, result);
            }
        }

        private void OnExitReached(LevelLocalEvent levelEvent)
        {
            if (completedRequired.Count == required.Count)
            {
                if (testCompleted) return;
                testCompleted = true;
                feedback?.ShowTestLevelCompleted(levelEvent.EndpointId);
            }
            else
                feedback?.ShowTestExitLocked(levelEvent.EndpointId);
        }

        private void OnPlaceholderInteraction(LevelLocalEvent levelEvent)
        {
            feedback?.ShowPlaceholderDialogue(levelEvent.EndpointId);
        }

        private void ActivateEncounter(LevelLocalEvent levelEvent, ContentIdentity actionId)
        {
            LevelActionResult result = session.Execute(new LevelActionCommand(
                levelEvent.Scope, actionId, LevelEndpointKind.ActivateEncounter));
            if (result != LevelActionResult.Success && result != LevelActionResult.AlreadySatisfied)
                feedback?.ReportActionFailure(actionId, result);
        }

        private static void RequireKind(LevelBindingSession session, ContentIdentity id,
            LevelEndpointKind expected)
        {
            if (!id.IsComplete || !session.Catalog.TryGet(id, out var endpoint)
                || endpoint.Kind != expected || !endpoint.Allows(LevelRunMode.Sandbox))
                throw new InvalidOperationException($"Sandbox endpoint {id} must be {expected} and enabled in Sandbox.");
        }
    }
}
