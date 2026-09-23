using Echo.LevelToolkit.Combat;
using Echo.LevelToolkit.Foundation;
using UnityEngine;

namespace Echo.NativeGame
{
    // NativeDemo's explicit adapter for one run and one mounted level instance.
    [DisallowMultipleComponent]
    public sealed class NativeCombatIntegration : MonoBehaviour
    {
        public string authorId = "echo";
        public string workId = "native-demo";
        public string levelLocalId = "main";
        public NativeEnemy[] enemies = new NativeEnemy[0];
        public NativeBossController boss;

        public CombatWorld World { get; private set; }
        public LevelInstanceContext InstanceContext { get; private set; }
        public RunId CurrentRunId { get; private set; }
        public bool IsInitialized { get; private set; }

        NativeRunController level;

        public bool Initialize(NativeRunController run)
        {
            if (IsInitialized || !run || run.gameObject != gameObject || !run.Running ||
                !run.player || run.player.run != run || !run.combat || run.combat.level != run ||
                run.combat.actor != run.player || !boss || boss.level != run || enemies == null)
                return false;

            var content = new ContentIdentity(authorId, workId, levelLocalId);
            if (!content.IsComplete) return false;
            foreach (var enemy in enemies)
                if (!enemy || enemy.run != run || !enemy.isActiveAndEnabled) return false;

            level = run;
            CurrentRunId = RunId.New();
            var scope = RuntimeScope.New(CurrentRunId, content);
            InstanceContext = new LevelInstanceContext(new NativeRunContext(run, CurrentRunId), scope);

            var worldObject = new GameObject("Native shared combat world");
            worldObject.transform.SetParent(transform, false);
            World = worldObject.AddComponent<CombatWorld>();
            var player = run.player.PrepareSharedActor();
            if (!player || !World.Initialize(InstanceContext, player) ||
                !run.combat.InitializeSharedWeapon(World)) return false;

            foreach (var enemy in enemies)
                if (!enemy.InitializeSharedActor(World)) return false;
            if (!boss.InitializeSharedActor(World)) return false;

            IsInitialized = true;
            return true;
        }

        // Dynamically spawned Native enemies must be registered by their owning run.
        public bool RegisterEnemy(NativeEnemy enemy)
        {
            return IsInitialized && enemy && enemy.run == level && enemy.InitializeSharedActor(World);
        }

        sealed class NativeRunContext : ILevelRunContext
        {
            readonly NativeRunController run;
            public RunId RunId { get; }
            public bool IsRunning => run && run.Running;
            public Transform Player => run && run.player ? run.player.transform : null;

            public NativeRunContext(NativeRunController run, RunId runId)
            {
                this.run = run;
                RunId = runId;
            }

            public void ReportPlayerDeath()
            {
                if (run) run.PlayerDied();
            }
        }
    }
}
