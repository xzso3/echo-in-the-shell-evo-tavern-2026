using System;
using System.Collections.Generic;
using Echo.LevelToolkit.Binding;
using Echo.LevelToolkit.Combat;
using Echo.LevelToolkit.Foundation;
using Echo.LevelToolkit.Map.Doors;
using UnityEngine;

namespace Echo.LevelToolkit.Level
{
    public enum EncounterMode { FreeCombat, ClearEnemies, Boss }
    public enum EncounterState { Dormant, Active, Completed }

    // A PolygonCollider2D trigger may span several placed Chunk instances.
    [RequireComponent(typeof(PolygonCollider2D))]
    public sealed class LevelEncounter : MonoBehaviour, ILevelActionTarget
    {
        [SerializeField] private ContentIdentity encounterId;
        [SerializeField] private ContentIdentity regionEnteredEndpoint;
        [SerializeField] private ContentIdentity activateEndpoint;
        [SerializeField] private ContentIdentity completedEndpoint;
        [SerializeField] private EncounterMode mode;
        [SerializeField] private bool autoStartOnEnter = true;
        [SerializeField] private CombatEnemy[] enemies = Array.Empty<CombatEnemy>();
        [SerializeField] private CombatBoss boss;
        [SerializeField] private ContentIdentity[] sealDoors = Array.Empty<ContentIdentity>();

        private CombatWorld world;
        private MapDoorSet doors;
        private LevelBindingSession session;
        private RuntimeScope scope;
        private IDisposable actionRegistration;
        private readonly HashSet<CombatEnemy> realKills = new HashSet<CombatEnemy>();
        private bool completionPublished;
        private bool playerInside;

        public ContentIdentity EncounterId => encounterId;
        public ContentIdentity RegionEnteredEndpoint => regionEnteredEndpoint;
        public ContentIdentity ActivateEndpoint => activateEndpoint;
        public ContentIdentity CompletedEndpoint => completedEndpoint;
        public EncounterMode Mode => mode;
        public EncounterState State { get; private set; }
        public IReadOnlyList<CombatEnemy> Enemies => enemies ?? Array.Empty<CombatEnemy>();
        public CombatBoss Boss => boss;
        public IReadOnlyList<ContentIdentity> SealDoors => sealDoors ?? Array.Empty<ContentIdentity>();
        public bool AutoStartOnEnter => autoStartOnEnter;

        public bool Prepare(CombatWorld combatWorld, MapDoorSet doorSet, LevelBindingSession binding)
        {
            if (world || !combatWorld || !doorSet || binding == null || !encounterId.IsComplete
                || binding.Context.Scope != combatWorld.Context.Scope || binding.Context.Scope != doorSet.Scope)
                return false;
            var area = GetComponent<PolygonCollider2D>();
            if (!area || !area.isTrigger || !regionEnteredEndpoint.IsComplete)
                return false;
            if (mode != EncounterMode.FreeCombat && !activateEndpoint.IsComplete) return false;
            if (mode != EncounterMode.FreeCombat && !completedEndpoint.IsComplete) return false;
            if (mode == EncounterMode.Boss && (!boss || Enemies.Count != 0)) return false;
            if (mode != EncounterMode.Boss && boss) return false;
            if (mode == EncounterMode.ClearEnemies && Enemies.Count == 0) return false;
            if (mode == EncounterMode.FreeCombat && SealDoors.Count != 0) return false;
            if (mode != EncounterMode.FreeCombat && !doorSet.Reserve(encounterId, SealDoors)) return false;

            var unique = new HashSet<CombatEnemy>();
            foreach (CombatEnemy enemy in Enemies)
                if (!enemy || !unique.Add(enemy) || !enemy.gameObject.activeInHierarchy || !enemy.Initialize(combatWorld))
                    return false;
            foreach (CombatEnemy enemy in Enemies)
            {
                CombatFanAttack fan = enemy.GetComponent<CombatFanAttack>();
                if (fan && !fan.Initialize()) return false;
            }
            if (boss && (!boss.gameObject.activeInHierarchy || !boss.Initialize(combatWorld))) return false;
            world = combatWorld;
            doors = doorSet;
            session = binding;
            scope = binding.Context.Scope;
            world.EnemyDied += OnEnemyDied;
            if (boss) boss.Defeated += OnBossDefeated;
            foreach (CombatEnemy enemy in Enemies) enemy.gameObject.SetActive(false);
            if (mode != EncounterMode.FreeCombat)
            {
                try { actionRegistration = binding.RegisterActionTarget(activateEndpoint, this); }
                catch (Exception exception) { Debug.LogException(exception, this); Dispose(); return false; }
            }
            State = EncounterState.Dormant;
            return true;
        }

        public void OnBindingStarted()
        {
            if (session == null || !session.IsActive) return;
            if (mode == EncounterMode.FreeCombat)
            {
                foreach (CombatEnemy enemy in Enemies) if (enemy) enemy.gameObject.SetActive(true);
                State = EncounterState.Active;
            }
        }

        public LevelActionResult Execute(LevelActionCommand command)
        {
            if (command.Scope != scope) return LevelActionResult.InvalidScope;
            if (command.Kind != LevelEndpointKind.ActivateEncounter || !command.EndpointId.Equals(activateEndpoint))
                return LevelActionResult.TargetMissing;
            return TryStart();
        }

        public LevelActionResult TryStart()
        {
            if (!world || !session.IsActive || !world.IsRunning) return LevelActionResult.RunInactive;
            if (State != EncounterState.Dormant) return LevelActionResult.AlreadySatisfied;
            if (mode == EncounterMode.FreeCombat) return LevelActionResult.ConditionNotMet;
            Collider2D playerCollider = world.Player.GetComponent<Collider2D>();
            if (!doors.CanLock(encounterId, SealDoors, playerCollider)) return LevelActionResult.ConditionNotMet;
            if (mode == EncounterMode.Boss)
            {
                if (!boss) return LevelActionResult.TargetMissing;
                // Never mark started or close doors before this succeeds.
                CombatBoss.ActivationResult result = boss.TryActivate();
                if (result != CombatBoss.ActivationResult.Started)
                    return result == CombatBoss.ActivationResult.NotRunning
                        ? LevelActionResult.RunInactive : LevelActionResult.TargetFailed;
            }
            else
            {
                foreach (CombatEnemy enemy in Enemies)
                {
                    if (!enemy || !enemy.Alive)
                    { DeactivateEnemies(); return LevelActionResult.TargetMissing; }
                    enemy.gameObject.SetActive(true);
                    if (!enemy.isActiveAndEnabled)
                    { DeactivateEnemies(); return LevelActionResult.TargetFailed; }
                }
            }
            // CanLock and TryLock run synchronously on the Unity thread.
            if (!doors.TryLock(encounterId, SealDoors, playerCollider))
            {
                if (mode == EncounterMode.ClearEnemies) DeactivateEnemies();
                else Debug.LogError("Boss activated but a door lock changed unexpectedly; keep the route open.", this);
                return LevelActionResult.TargetFailed;
            }
            State = EncounterState.Active;
            return LevelActionResult.Success;
        }

        public LevelEventResult PlayerEntered(CombatPlayer player)
        {
            if (!world || !session.IsActive || !world.IsRunning || player != world.Player)
                return LevelEventResult.RunInactive;
            if (playerInside) return LevelEventResult.AlreadyPublished;
            playerInside = true;
            LevelEventResult result = session.Publish(scope, regionEnteredEndpoint,
                LevelEndpointKind.RegionEntered, player);
            if (autoStartOnEnter && mode != EncounterMode.FreeCombat && State == EncounterState.Dormant)
                TryStart();
            return result;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            CombatPlayer player = other.GetComponentInParent<CombatPlayer>();
            if (player) PlayerEntered(player);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (playerInside) return;
            CombatPlayer player = other.GetComponentInParent<CombatPlayer>();
            if (player) PlayerEntered(player);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            CombatPlayer player = other.GetComponentInParent<CombatPlayer>();
            if (world && player == world.Player) playerInside = false;
        }

        private void OnEnemyDied(CombatEnemy enemy)
        {
            if (State != EncounterState.Active || mode != EncounterMode.ClearEnemies) return;
            bool assigned = false;
            foreach (CombatEnemy target in Enemies) if (ReferenceEquals(target, enemy)) { assigned = true; break; }
            if (!assigned || !realKills.Add(enemy)) return;
            if (realKills.Count == Enemies.Count) Complete();
        }

        private void OnBossDefeated(CombatBoss target)
        {
            if (State == EncounterState.Active && mode == EncounterMode.Boss && ReferenceEquals(target, boss))
                Complete();
        }

        private void Complete()
        {
            State = EncounterState.Completed;
            doors.Unlock(encounterId, SealDoors);
            PublishCompletion();
        }

        private void PublishCompletion()
        {
            if (completionPublished || State != EncounterState.Completed || !completedEndpoint.IsComplete
                || session == null || !session.IsActive) return;
            LevelEventResult result = session.Publish(scope, completedEndpoint,
                LevelEndpointKind.EncounterCompleted, this);
            completionPublished = result == LevelEventResult.Published
                || result == LevelEventResult.AlreadyPublished;
        }

        private void Update()
        {
            if (State == EncounterState.Completed && !completionPublished) PublishCompletion();
        }

        private void DeactivateEnemies()
        {
            foreach (CombatEnemy enemy in Enemies) if (enemy) enemy.gameObject.SetActive(false);
        }

        public void Dispose()
        {
            actionRegistration?.Dispose(); actionRegistration = null;
            if (world) world.EnemyDied -= OnEnemyDied;
            if (boss) boss.Defeated -= OnBossDefeated;
            world = null; doors = null; session = null; scope = default;
            playerInside = false;
        }

        private void OnDestroy() { Dispose(); }
    }
}
