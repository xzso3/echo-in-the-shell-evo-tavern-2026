using System;
using System.Collections.Generic;
using Echo.LevelToolkit.Binding;
using Echo.LevelToolkit.Foundation;
using UnityEngine;

namespace Echo.NativeGame.ToolkitIntegration.LevelHost
{
    // NativeDemo needs no reference to this component. An installed work is mounted only
    // after an explicit Mount call, never merely because it was imported or previewed.
    public sealed class NativeLevelHost : MonoBehaviour
    {
        [SerializeField] private NativeRunController run;
        [SerializeField] private NativeCamera nativeCamera;
        [SerializeField] private Transform placementParent;
        private readonly Dictionary<LevelInstanceId, NativeLevelInstance> instances =
            new Dictionary<LevelInstanceId, NativeLevelInstance>();
        private readonly Dictionary<LevelInstanceId, NativeLevelInstanceRunContext> contexts =
            new Dictionary<LevelInstanceId, NativeLevelInstanceRunContext>();
        private LevelRunBindingPolicy policy;
        private LevelInstanceId focused;
        private bool savedCamera;
        private bool unmountingAll;
        private Transform originalTarget;
        private Vector2 originalCenter;
        private Vector2 originalHalfSize;

        public RunId RunId { get; private set; }
        public bool IsRunning => isActiveAndEnabled && run && run.Running && RunId.IsValid;
        public IEnumerable<NativeLevelInstance> Instances => instances.Values;
        public LevelInstanceId FocusedInstanceId => focused;
        public event Action<NativeLevelInstance> Mounted;
        public event Action<NativeLevelInstance> Unmounted;

        private void OnEnable()
        {
            if (run) run.Started += OnNativeStarted;
        }

        private void OnDisable()
        {
            if (run) run.Started -= OnNativeStarted;
            UnmountAll();
            RunId = default;
            policy = null;
        }

        private void OnDestroy() { UnmountAll(); }

        private void OnNativeStarted() { BeginRun(); }

        // NativeRunController.Started is the normal call site. A host attached after Start
        // may call this explicitly, provided the Native run is already playing.
        public bool BeginRun()
        {
            if (!isActiveAndEnabled || !run || !run.Running) return false;
            if (RunId.IsValid) return true;
            RunId = RunId.New();
            policy = new LevelRunBindingPolicy(RunId, LevelRunMode.Integrated);
            return true;
        }

        public bool Mount(NativeLevelPlacement prefab, ILevelBinding binding,
            out NativeLevelInstance instance, out string diagnostic)
        {
            return Mount(prefab, prefab ? prefab.transform.position : Vector3.zero,
                binding, out instance, out diagnostic);
        }

        // worldOrigin allows two copies of one authored work in the same scene.
        public bool Mount(NativeLevelPlacement prefab, Vector3 worldOrigin, ILevelBinding binding,
            out NativeLevelInstance instance, out string diagnostic)
        {
            instance = null;
            if (unmountingAll)
            { diagnostic = "The host is unloading its instances."; return false; }
            if (!Application.isPlaying || !BeginRun())
            { diagnostic = "A playing Native run is required."; return false; }
            if (!prefab)
            { diagnostic = "Assign an installed work prefab."; return false; }
            if (!prefab.Validate(out diagnostic)) return false;
            if (binding == null || binding.Mode != LevelRunMode.Integrated)
            { diagnostic = "An explicit Integrated binding is required."; return false; }
            if (nativeCamera == null)
            { diagnostic = "Assign the Native camera before mounting a work."; return false; }

            NativeLevelPlacement placed = null;
            LevelBindingSession session = null;
            NativeLevelInstanceRunContext runContext = null;
            try
            {
                placed = Instantiate(prefab, placementParent ? placementParent : transform);
                placed.transform.position = worldOrigin;
                if (!placed.Validate(out diagnostic)) return false;
                RuntimeScope scope = RuntimeScope.New(RunId, placed.Stage.LevelId);
                runContext = new NativeLevelInstanceRunContext(this, placed);
                var context = new LevelInstanceContext(runContext, scope);
                placed.Player.transform.position = placed.Stage.PlayerSpawn.position;
                if (!placed.World.Initialize(context, placed.Player))
                { diagnostic = "CombatWorld initialization failed."; return false; }
                if (placed.Weapon && !placed.Weapon.Initialize(placed.World))
                { diagnostic = "CombatWeapon initialization failed."; return false; }
                session = new LevelBindingSession(context, placed.Catalog, policy);
                if (!placed.Stage.Prepare(placed.World, session))
                { diagnostic = "LevelStage preparation failed."; return false; }
                BindingStartResult started = session.TryStart(binding);
                if (!started.Succeeded)
                { diagnostic = started.Diagnostic; return false; }
                if (!placed.BindEmitters(session, scope))
                { diagnostic = "A LevelEventEmitter could not bind to this instance scope."; return false; }
                if (!placed.Stage.StartBound())
                { diagnostic = "LevelStage binding start failed."; return false; }
                var mounted = new NativeLevelInstance(this, placed, scope, session);
                instances.Add(scope.InstanceId, mounted);
                contexts.Add(scope.InstanceId, runContext);
                if (!focused.IsValid && !Focus(scope))
                { diagnostic = "Could not focus the mounted level camera."; return false; }
                instance = mounted;
                Notify(Mounted, mounted);
                diagnostic = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                diagnostic = "Mount failed: " + exception.Message;
                Debug.LogException(exception, this);
                return false;
            }
            finally
            {
                if (instance == null)
                {
                    runContext?.Stop();
                    if (placed && placed.Stage)
                    {
                        LevelInstanceId id = default;
                        foreach (var pair in instances)
                            if (pair.Value.Placement == placed) { id = pair.Key; break; }
                        if (id.IsValid) { instances.Remove(id); contexts.Remove(id); }
                    }
                    if (focused.IsValid && !instances.ContainsKey(focused))
                    { focused = default; RestoreCamera(); }
                    if (placed) placed.gameObject.SetActive(false);
                    if (placed) placed.UnbindEmitters();
                    if (placed && placed.Stage) placed.Stage.Dispose();
                    session?.Dispose();
                    if (placed) Destroy(placed.gameObject);
                }
            }
        }

        public bool TryGet(RuntimeScope scope, out NativeLevelInstance instance)
        {
            if (scope.RunId == RunId && instances.TryGetValue(scope.InstanceId, out instance)
                && instance.Scope == scope && instance.IsRunning) return true;
            instance = null;
            return false;
        }

        public bool Focus(RuntimeScope scope)
        {
            if (!nativeCamera || !TryGet(scope, out NativeLevelInstance instance)
                || !instance.Placement.TryGetCameraBounds(out Vector2 center, out Vector2 halfSize))
                return false;
            if (!savedCamera)
            {
                originalTarget = nativeCamera.target;
                originalCenter = nativeCamera.worldCenter;
                originalHalfSize = nativeCamera.worldHalfSize;
                savedCamera = true;
            }
            nativeCamera.target = instance.Player.transform;
            nativeCamera.worldCenter = center;
            nativeCamera.worldHalfSize = halfSize;
            focused = scope.InstanceId;
            return true;
        }

        public bool Unmount(RuntimeScope scope)
        {
            if (scope.RunId != RunId || !instances.TryGetValue(scope.InstanceId, out NativeLevelInstance instance)
                || instance.Scope != scope) return false;
            contexts[scope.InstanceId].Stop();
            instances.Remove(scope.InstanceId);
            contexts.Remove(scope.InstanceId);
            instance.Dispose();
            if (focused == scope.InstanceId)
            {
                focused = default;
                foreach (NativeLevelInstance other in instances.Values)
                    if (Focus(other.Scope)) break;
                if (!focused.IsValid) RestoreCamera();
            }
            Notify(Unmounted, instance);
            return true;
        }

        public void UnmountAll()
        {
            if (unmountingAll) return;
            unmountingAll = true;
            try
            {
                if (instances.Count != 0)
                {
                    var pending = new List<NativeLevelInstance>(instances.Values);
                    foreach (NativeLevelInstance instance in pending)
                        if (contexts.TryGetValue(instance.Scope.InstanceId, out var context)) context.Stop();
                    instances.Clear();
                    contexts.Clear();
                    focused = default;
                    RestoreCamera();
                    foreach (NativeLevelInstance instance in pending)
                    {
                        instance.Dispose();
                        Notify(Unmounted, instance);
                    }
                }
                else { focused = default; RestoreCamera(); }
            }
            finally { unmountingAll = false; }
        }

        public void ReportPlayerDeath()
        {
            if (!IsRunning) return;
            run.PlayerDied();
            UnmountAll();
        }

        private void Update()
        {
            if (RunId.IsValid && !IsRunning && instances.Count != 0) UnmountAll();
        }

        private void RestoreCamera()
        {
            if (!savedCamera) return;
            if (nativeCamera)
            {
                nativeCamera.target = originalTarget;
                nativeCamera.worldCenter = originalCenter;
                nativeCamera.worldHalfSize = originalHalfSize;
            }
            savedCamera = false;
        }

        private void Notify(Action<NativeLevelInstance> handlers, NativeLevelInstance instance)
        {
            if (handlers == null) return;
            foreach (Action<NativeLevelInstance> handler in handlers.GetInvocationList())
            {
                try { handler(instance); }
                catch (Exception exception) { Debug.LogException(exception, this); }
            }
        }
    }
}
