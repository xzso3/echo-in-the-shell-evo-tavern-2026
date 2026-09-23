using System;
using Echo.LevelToolkit.Binding;
using Echo.LevelToolkit.Combat;
using Echo.LevelToolkit.Foundation;
using UnityEngine;

namespace Echo.LevelToolkit.Level
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class LevelExit : MonoBehaviour, ILevelActionTarget
    {
        [SerializeField] private ContentIdentity exitId;
        [SerializeField] private ContentIdentity reachedEndpoint;
        [SerializeField] private ContentIdentity unlockEndpoint;
        [SerializeField] private GameObject lockedVisual;
        private LevelBindingSession session;
        private CombatWorld world;
        private RuntimeScope scope;
        private IDisposable registration;
        private bool deliveredInside;
        public ContentIdentity ExitId => exitId;
        public ContentIdentity ReachedEndpoint => reachedEndpoint;
        public ContentIdentity UnlockEndpoint => unlockEndpoint;
        public bool IsUnlocked { get; private set; }

        public bool Prepare(CombatWorld combatWorld, LevelBindingSession binding)
        {
            if (session != null || !combatWorld || binding == null || !exitId.IsComplete
                || !reachedEndpoint.IsComplete || !unlockEndpoint.IsComplete
                || combatWorld.Context.Scope != binding.Context.Scope || !GetComponent<Collider2D>().isTrigger)
                return false;
            if (!binding.Catalog.TryGet(reachedEndpoint, out var reached)
                || reached.Kind != LevelEndpointKind.ExitReached || !reached.Allows(binding.Mode)
                || !binding.Catalog.TryGet(unlockEndpoint, out var unlock)
                || unlock.Kind != LevelEndpointKind.UnlockExit || !unlock.Allows(binding.Mode))
                return false;
            world = combatWorld;
            session = binding;
            scope = binding.Context.Scope;
            IsUnlocked = false;
            if (lockedVisual) lockedVisual.SetActive(true);
            try { registration = binding.RegisterActionTarget(unlockEndpoint, this); }
            catch (Exception exception) { Debug.LogException(exception, this); Dispose(); return false; }
            return true;
        }

        public LevelActionResult Execute(LevelActionCommand command)
        {
            if (command.Scope != scope) return LevelActionResult.InvalidScope;
            if (command.Kind != LevelEndpointKind.UnlockExit || !command.EndpointId.Equals(unlockEndpoint))
                return LevelActionResult.TargetMissing;
            if (!world || !world.IsRunning || !session.IsActive) return LevelActionResult.RunInactive;
            if (IsUnlocked) return LevelActionResult.AlreadySatisfied;
            IsUnlocked = true;
            if (lockedVisual) lockedVisual.SetActive(false);
            return LevelActionResult.Success;
        }

        public LevelEventResult PlayerReached(CombatPlayer player)
        {
            if (!world || !world.IsRunning || !session.IsActive || player != world.Player)
                return LevelEventResult.RunInactive;
            if (!IsUnlocked) return LevelEventResult.ModeDenied;
            if (deliveredInside) return LevelEventResult.AlreadyPublished;
            LevelEventResult result = session.Publish(scope, reachedEndpoint, LevelEndpointKind.ExitReached, player);
            deliveredInside = result == LevelEventResult.Published;
            return result;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            CombatPlayer player = other.GetComponentInParent<CombatPlayer>();
            if (player) PlayerReached(player);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!IsUnlocked) return;
            CombatPlayer player = other.GetComponentInParent<CombatPlayer>();
            if (player) PlayerReached(player);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            CombatPlayer player = other.GetComponentInParent<CombatPlayer>();
            if (world && player == world.Player) deliveredInside = false;
        }

        public void Dispose()
        {
            registration?.Dispose(); registration = null;
            session = null; world = null; scope = default;
            IsUnlocked = false;
            deliveredInside = false;
        }
        private void OnDestroy() { Dispose(); }
    }
}
