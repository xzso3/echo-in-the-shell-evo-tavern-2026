using Echo.LevelToolkit.Binding;
using Echo.LevelToolkit.Combat;
using Echo.LevelToolkit.Foundation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echo.LevelToolkit.Level
{
    // Place only in an independent test scene. Restart reloads that scene so
    // destroyed enemies, Boss state, door requests and all run IDs are rebuilt.
    public sealed class SandboxLevelHost : MonoBehaviour, ILevelRunContext, ISandboxFeedback
    {
        [SerializeField] private CombatWorld world;
        [SerializeField] private CombatPlayer player;
        [SerializeField] private CombatWeapon weapon;
        [SerializeField] private LevelStage stage;
        [SerializeField] private LevelEndpointCatalog catalog;
        [SerializeField] private SandboxBindingDefinition bindingDefinition;
        [SerializeField] private GameObject restartPrefab;
        [SerializeField] private bool showDebugHud = true;
        private LevelBindingSession session;
        private string status = "Starting test level";
        private bool completed;

        public RunId RunId { get; private set; }
        public bool IsRunning { get; private set; }
        public Transform Player => player ? player.transform : null;
        public bool Completed => completed;

        private void Start()
        {
            if (!world || !player || !stage || !catalog || !bindingDefinition)
            { Fail("Assign World, Player, Stage, Endpoint Catalog and Sandbox Binding."); return; }
            RunId = RunId.New();
            RuntimeScope scope;
            try { scope = RuntimeScope.New(RunId, stage.LevelId); }
            catch (System.Exception exception) { Fail(exception.Message); return; }
            var context = new LevelInstanceContext(this, scope);
            if (stage.PlayerSpawn) player.transform.position = stage.PlayerSpawn.position;
            IsRunning = true;
            if (!world.Initialize(context, player)) { Fail("CombatWorld initialization failed."); return; }
            if (weapon && !weapon.Initialize(world)) { Fail("CombatWeapon initialization failed."); return; }
            session = new LevelBindingSession(context, catalog,
                new LevelRunBindingPolicy(RunId, LevelRunMode.Sandbox));
            if (!stage.Prepare(world, session)) { Fail("LevelStage preparation failed."); return; }
            BindingStartResult started = session.TryStart(bindingDefinition.CreateBinding(this));
            if (!started.Succeeded) { Fail(started.Diagnostic); return; }
            if (!stage.StartBound()) { Fail("LevelStage binding start failed."); return; }
            if (bindingDefinition.RequiredEncounterEvents.Count == 0
                && bindingDefinition.UnlockExitAction.IsComplete)
                session.Execute(new LevelActionCommand(scope, bindingDefinition.UnlockExitAction,
                    LevelEndpointKind.UnlockExit));
            status = "WASD move · Space fire · E Boss core · R restart";
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R)) { Restart(); return; }
            if (!IsRunning || !player) return;
            player.MoveInput = new Vector2(Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical")).normalized;
            if (weapon && Input.GetKeyDown(KeyCode.Space)) weapon.ToggleFire();
            if (Input.GetKeyDown(KeyCode.E))
            {
                CombatBoss nearest = null;
                float nearestDistance = float.PositiveInfinity;
                foreach (CombatBoss boss in world.Bosses)
                {
                    if (!boss || !boss.CanInteractCore(player)) continue;
                    float distance = Vector2.SqrMagnitude((Vector2)boss.transform.position -
                        (Vector2)player.transform.position);
                    if (distance >= nearestDistance) continue;
                    nearest = boss; nearestDistance = distance;
                }
                if (nearest) nearest.InteractCore(player);
            }
        }

        public void ReportPlayerDeath()
        {
            if (!IsRunning) return;
            IsRunning = false;
            status = "Player defeated. Press R to restart the entire test level.";
        }

        public void ShowPlaceholderDialogue(ContentIdentity id)
        { status = "Interaction: " + id.LocalId; }
        public void ShowTestExitLocked(ContentIdentity id)
        { status = "Test exit locked: " + id.LocalId; }
        public void ShowTestLevelCompleted(ContentIdentity id)
        {
            if (completed || !IsRunning) return;
            completed = true;
            IsRunning = false;
            status = "Test level complete. Press R to restart.";
        }
        public void ReportActionFailure(ContentIdentity id, LevelActionResult result)
        { status = "Action " + id.LocalId + " failed: " + result; }

        private void Fail(string diagnostic)
        {
            IsRunning = false;
            status = "Test level setup failed: " + diagnostic;
            Debug.LogError(status, this);
        }

        private void Restart()
        {
            IsRunning = false;
            session?.Dispose();
            if (stage) stage.Dispose();
            if (restartPrefab)
            {
                gameObject.SetActive(false);
                Instantiate(restartPrefab, transform.position, transform.rotation);
                Destroy(gameObject);
                return;
            }
            Scene scene = gameObject.scene;
            if (scene.IsValid() && Application.CanStreamedLevelBeLoaded(scene.path))
                SceneManager.LoadScene(scene.path);
            else Fail("Assign a fresh level prefab for restart, or add this scene to Build Settings.");
        }

        private void OnDestroy()
        {
            session?.Dispose();
            if (stage) stage.Dispose();
        }

        private void OnGUI()
        {
            if (!showDebugHud) return;
            GUILayout.BeginArea(new Rect(12, 12, 610, 70), GUI.skin.box);
            GUILayout.Label(status);
            if (player) GUILayout.Label("HP " + player.Health.ToString("0") + " / " + player.maxHealth);
            GUILayout.EndArea();
        }
    }
}
