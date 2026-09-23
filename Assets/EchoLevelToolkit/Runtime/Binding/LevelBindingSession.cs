using System;
using System.Collections.Generic;
using Echo.LevelToolkit.Foundation;
using UnityEngine;

namespace Echo.LevelToolkit.Binding
{
    public enum LevelActionResult
    {
        Success,
        AlreadySatisfied,
        ConditionNotMet,
        TargetMissing,
        ModeDenied,
        InvalidScope,
        RunInactive,
        TargetFailed
    }

    public enum LevelEventResult
    {
        Published,
        AlreadyPublished,
        EndpointMissing,
        ModeDenied,
        InvalidScope,
        RunInactive,
        HandlerFailed
    }

    public readonly struct LevelLocalEvent
    {
        public RuntimeScope Scope { get; }
        public ContentIdentity EndpointId { get; }
        public LevelEndpointKind Kind { get; }
        public long Sequence { get; }
        public UnityEngine.Object Actor { get; }

        public LevelLocalEvent(RuntimeScope scope, ContentIdentity endpointId,
            LevelEndpointKind kind, long sequence, UnityEngine.Object actor)
        {
            Scope = scope;
            EndpointId = endpointId;
            Kind = kind;
            Sequence = sequence;
            Actor = actor;
        }
    }

    public readonly struct LevelActionCommand
    {
        public RuntimeScope Scope { get; }
        public ContentIdentity EndpointId { get; }
        public LevelEndpointKind Kind { get; }
        public bool DesiredState { get; }

        public LevelActionCommand(RuntimeScope scope, ContentIdentity endpointId,
            LevelEndpointKind kind, bool desiredState = true)
        {
            Scope = scope;
            EndpointId = endpointId;
            Kind = kind;
            DesiredState = desiredState;
        }
    }

    public interface ILevelActionTarget
    {
        LevelActionResult Execute(LevelActionCommand command);
    }

    // A host selects exactly one binding before the level begins. The binding registers
    // event consumers; Map, Level and Combat retain their own mutable state.
    public interface ILevelBinding
    {
        LevelRunMode Mode { get; }
        void Bind(LevelBindingSession session);
    }

    public readonly struct BindingStartResult
    {
        public bool Succeeded { get; }
        public string Diagnostic { get; }

        public BindingStartResult(bool succeeded, string diagnostic)
        {
            Succeeded = succeeded;
            Diagnostic = diagnostic ?? string.Empty;
        }
    }

    // Create one policy when the run is created and pass it to every placed level.
    // Its mode cannot change; a restart creates a fresh policy with a fresh RunId.
    public sealed class LevelRunBindingPolicy
    {
        public RunId RunId { get; }
        public LevelRunMode Mode { get; }

        public LevelRunBindingPolicy(RunId runId, LevelRunMode mode)
        {
            if (!runId.IsValid) throw new ArgumentException("A valid RunId is required.", nameof(runId));
            if (mode != LevelRunMode.Sandbox && mode != LevelRunMode.Integrated)
                throw new ArgumentException("Select Sandbox or Integrated before the run starts.", nameof(mode));
            RunId = runId;
            Mode = mode;
        }
    }

    public sealed class LevelBindingSession : IDisposable
    {
        private enum SessionState { Created, Binding, Active, Disposed }

        private readonly LevelInstanceContext context;
        private readonly LevelEndpointCatalog catalog;
        private readonly LevelRunMode mode;
        private readonly Dictionary<ContentIdentity, List<Action<LevelLocalEvent>>> subscribers =
            new Dictionary<ContentIdentity, List<Action<LevelLocalEvent>>>();
        private readonly Dictionary<ContentIdentity, ILevelActionTarget> targets =
            new Dictionary<ContentIdentity, ILevelActionTarget>();
        private readonly HashSet<ContentIdentity> completedEvents = new HashSet<ContentIdentity>();
        private SessionState state;
        private long nextSequence;

        public LevelInstanceContext Context => context;
        public LevelEndpointCatalog Catalog => catalog;
        public LevelRunMode Mode => mode;
        public bool IsActive => state == SessionState.Active && IsCurrentRun();

        public LevelBindingSession(LevelInstanceContext context, LevelEndpointCatalog catalog,
            LevelRunBindingPolicy policy)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            if (policy == null) throw new ArgumentNullException(nameof(policy));
            if (policy.RunId != context.Scope.RunId)
                throw new ArgumentException("Binding policy must belong to the instance run.", nameof(policy));
            mode = policy.Mode;
        }

        public BindingStartResult TryStart(ILevelBinding binding)
        {
            if (state != SessionState.Created)
                return new BindingStartResult(false, "Binding session has already started or ended.");
            if (!IsCurrentRun())
                return Fail("Run is inactive or belongs to another RunId.");
            if (!catalog.LevelIdentity.Equals(context.Scope.Content))
                return Fail("Catalog level identity does not match the placed level.");
            IReadOnlyList<string> catalogErrors = catalog.ValidateCatalog();
            if (catalogErrors.Count != 0)
                return Fail(string.Join("; ", catalogErrors));
            if (binding == null || binding.Mode != mode)
                return Fail($"A {mode} binding is required; no mode fallback is permitted.");

            state = SessionState.Binding;
            try
            {
                binding.Bind(this);
                if (!IsCurrentRun())
                    return Fail("Run stopped during binding.");
                foreach (LevelEndpointDefinition endpoint in catalog.Endpoints)
                {
                    if (!endpoint.RequiredBinding || !endpoint.Allows(mode)) continue;
                    if (endpoint.IsEvent && (!subscribers.TryGetValue(endpoint.Identity, out var handlers)
                        || handlers.Count == 0))
                        return Fail($"Required {mode} event binding missing: {endpoint.Identity}.");
                    if (endpoint.IsAction && !targets.ContainsKey(endpoint.Identity))
                        return Fail($"Required {mode} action target missing: {endpoint.Identity}.");
                }
                state = SessionState.Active;
                return new BindingStartResult(true, string.Empty);
            }
            catch (Exception exception)
            {
                return Fail($"Binding failed: {exception.Message}");
            }
        }

        // Target registration may happen before TryStart; required targets are checked
        // after ILevelBinding.Bind and before any event can be published.
        public IDisposable RegisterActionTarget(ContentIdentity endpointId, ILevelActionTarget target)
        {
            if (state != SessionState.Created && state != SessionState.Binding)
                throw new InvalidOperationException("Register action targets before the level starts.");
            if (target == null) throw new ArgumentNullException(nameof(target));
            LevelEndpointDefinition endpoint = RequireEndpoint(endpointId, false);
            if (!endpoint.Allows(mode)) throw new InvalidOperationException("Action is not allowed in this mode.");
            if (targets.ContainsKey(endpointId))
                throw new InvalidOperationException($"Duplicate action target: {endpointId}.");
            targets.Add(endpointId, target);
            return new Registration(() =>
            {
                if (targets.TryGetValue(endpointId, out var current) && ReferenceEquals(current, target))
                    targets.Remove(endpointId);
            });
        }

        public IDisposable Subscribe(ContentIdentity endpointId, Action<LevelLocalEvent> handler)
        {
            if (state != SessionState.Binding)
                throw new InvalidOperationException("Subscribe only while the selected binding is installed.");
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            LevelEndpointDefinition endpoint = RequireEndpoint(endpointId, true);
            if (!endpoint.Allows(mode)) throw new InvalidOperationException("Event is not allowed in this mode.");
            if (!subscribers.TryGetValue(endpointId, out var handlers))
            {
                handlers = new List<Action<LevelLocalEvent>>();
                subscribers.Add(endpointId, handlers);
            }
            handlers.Add(handler);
            return new Registration(() => handlers.Remove(handler));
        }

        public LevelEventResult Publish(RuntimeScope scope, ContentIdentity endpointId,
            LevelEndpointKind kind, UnityEngine.Object actor = null)
        {
            if (!ScopeMatches(scope)) return LevelEventResult.InvalidScope;
            if (!IsActive) return LevelEventResult.RunInactive;
            if (!catalog.TryGet(endpointId, out var endpoint) || !endpoint.IsEvent || endpoint.Kind != kind)
                return LevelEventResult.EndpointMissing;
            if (!endpoint.Allows(mode)) return LevelEventResult.ModeDenied;
            // A blocked exit must be reachable again after its condition becomes true.
            bool once = kind == LevelEndpointKind.EncounterCompleted;
            if (once && !completedEvents.Add(endpointId)) return LevelEventResult.AlreadyPublished;

            var levelEvent = new LevelLocalEvent(scope, endpointId, kind, ++nextSequence, actor);
            if (!subscribers.TryGetValue(endpointId, out var handlers)) return LevelEventResult.Published;
            bool failed = false;
            foreach (Action<LevelLocalEvent> handler in handlers.ToArray())
            {
                try { handler(levelEvent); }
                catch (Exception exception)
                {
                    failed = true;
                    Debug.LogException(exception);
                }
            }
            return failed ? LevelEventResult.HandlerFailed : LevelEventResult.Published;
        }

        public LevelActionResult Execute(LevelActionCommand command)
        {
            if (!ScopeMatches(command.Scope)) return LevelActionResult.InvalidScope;
            if (!IsActive) return LevelActionResult.RunInactive;
            if (!catalog.TryGet(command.EndpointId, out var endpoint)
                || !endpoint.IsAction || endpoint.Kind != command.Kind)
                return LevelActionResult.TargetMissing;
            if (!endpoint.Allows(mode)) return LevelActionResult.ModeDenied;
            if (!targets.TryGetValue(command.EndpointId, out var target))
                return LevelActionResult.TargetMissing;
            if (target is UnityEngine.Object unityTarget && unityTarget == null)
                return LevelActionResult.TargetMissing;
            // The target is the owner of conditions and mutable state. It must recheck
            // its own state when called, then return an explicit outcome.
            try { return target.Execute(command); }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return LevelActionResult.TargetFailed;
            }
        }

        public void Dispose()
        {
            state = SessionState.Disposed;
            subscribers.Clear();
            targets.Clear();
            completedEvents.Clear();
        }

        private BindingStartResult Fail(string diagnostic)
        {
            Dispose();
            return new BindingStartResult(false, diagnostic);
        }

        private LevelEndpointDefinition RequireEndpoint(ContentIdentity endpointId, bool isEvent)
        {
            if (!catalog.TryGet(endpointId, out var endpoint) || endpoint.IsEvent != isEvent)
                throw new ArgumentException($"Unknown or wrong-direction endpoint: {endpointId}.", nameof(endpointId));
            return endpoint;
        }

        private bool IsCurrentRun()
        {
            return context.Run.IsRunning && context.Run.RunId.IsValid
                && context.Run.RunId == context.Scope.RunId;
        }

        private bool ScopeMatches(RuntimeScope scope)
        {
            return scope.RunId.IsValid && scope.InstanceId.IsValid && scope == context.Scope;
        }

        private sealed class Registration : IDisposable
        {
            private Action release;
            public Registration(Action release) { this.release = release; }
            public void Dispose()
            {
                Action action = release;
                release = null;
                action?.Invoke();
            }
        }
    }
}
