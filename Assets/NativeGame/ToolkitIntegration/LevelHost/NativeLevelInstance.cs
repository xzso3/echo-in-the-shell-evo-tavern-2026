using Echo.LevelToolkit.Binding;
using Echo.LevelToolkit.Combat;
using Echo.LevelToolkit.Foundation;
using Echo.LevelToolkit.Level;
using Echo.LevelToolkit.Map;
using Echo.LevelToolkit.Map.Doors;
using UnityEngine;

namespace Echo.NativeGame.ToolkitIntegration.LevelHost
{
    // A single placed copy owns one session, world and actor set. Its Scope includes both
    // the Native run ID and a fresh level instance ID, even for copies of the same work.
    public sealed class NativeLevelInstance
    {
        private readonly NativeLevelHost host;
        private readonly NativeLevelInstanceRunContext runContext;
        private bool disposed;

        internal NativeLevelInstance(NativeLevelHost host, NativeLevelPlacement placement,
            RuntimeScope scope, LevelBindingSession session, NativeLevelInstanceRunContext runContext)
        {
            this.host = host;
            this.runContext = runContext;
            Placement = placement;
            Scope = scope;
            Session = session;
        }

        public NativeLevelPlacement Placement { get; }
        public RuntimeScope Scope { get; }
        public LevelBindingSession Session { get; }
        public LevelStage Stage => Placement ? Placement.Stage : null;
        public ChunkMap Map => Placement ? Placement.Map : null;
        public MapDoorSet Doors => Placement ? Placement.Doors : null;
        public CombatWorld World => Placement ? Placement.World : null;
        public CombatPlayer Player => Placement ? Placement.Player : null;
        public bool IsMounted => !disposed && host && host.IsRunning && Placement
            && Placement.gameObject.activeInHierarchy;
        public bool IsFocused => IsMounted && host.FocusedInstanceId == Scope.InstanceId;
        public bool IsRunning => IsMounted && runContext.IsRunning;

        internal void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (Placement) Placement.gameObject.SetActive(false);
            if (Placement) Placement.UnbindEmitters();
            if (Stage) Stage.Dispose();
            Session.Dispose();
            // Combat projectiles are parented to World; fan warnings and Boss hazards
            // are children of their actors. Destroying the placement removes all three.
            if (Placement) Object.Destroy(Placement.gameObject);
        }
    }

    internal sealed class NativeLevelInstanceRunContext : ILevelRunContext
    {
        private readonly NativeLevelHost host;
        private readonly NativeLevelPlacement placement;
        private readonly RuntimeScope scope;
        private bool initializing = true;
        private bool stopped;

        internal NativeLevelInstanceRunContext(NativeLevelHost host, NativeLevelPlacement placement,
            RuntimeScope scope)
        { this.host = host; this.placement = placement; this.scope = scope; }

        public RunId RunId => scope.RunId;
        public bool IsRunning => !stopped && host && host.IsRunning && host.RunId == scope.RunId && placement
            && placement.gameObject.activeInHierarchy
            && (initializing || host.FocusedInstanceId == scope.InstanceId);
        public Transform Player => placement && placement.Player ? placement.Player.transform : null;
        public void ReportPlayerDeath()
        {
            if (IsRunning && !initializing) host.ReportPlayerDeath(scope);
        }
        internal void FinishInitialization() { initializing = false; }
        internal void Stop() { stopped = true; }
    }
}
