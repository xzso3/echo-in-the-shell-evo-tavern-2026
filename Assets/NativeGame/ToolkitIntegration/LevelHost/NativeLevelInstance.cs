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
        private bool disposed;

        internal NativeLevelInstance(NativeLevelHost host, NativeLevelPlacement placement,
            RuntimeScope scope, LevelBindingSession session)
        {
            this.host = host;
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
        public bool IsRunning => !disposed && host && host.IsRunning && Placement && Placement.gameObject.activeInHierarchy;

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
        private bool stopped;

        internal NativeLevelInstanceRunContext(NativeLevelHost host, NativeLevelPlacement placement)
        { this.host = host; this.placement = placement; }

        public RunId RunId => host ? host.RunId : default;
        public bool IsRunning => !stopped && host && host.IsRunning && placement
            && placement.gameObject.activeInHierarchy;
        public Transform Player => placement && placement.Player ? placement.Player.transform : null;
        public void ReportPlayerDeath()
        {
            if (IsRunning) host.ReportPlayerDeath();
        }
        internal void Stop() { stopped = true; }
    }
}
