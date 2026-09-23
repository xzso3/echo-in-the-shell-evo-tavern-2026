using Echo.LevelToolkit.Binding;
using Echo.LevelToolkit.Combat;
using UnityEngine;

namespace Echo.NativeGame
{
    // Physical E source for an authored level endpoint. The placement's emitter is
    // already bound to its RuntimeScope by NativeLevelHost before HUD registration.
    [RequireComponent(typeof(LevelEventEmitter))]
    public sealed class NativeScopedInteraction : MonoBehaviour
    {
        public LevelEventEmitter emitter;
        [Min(.1f)] public float radius = 1.65f;
        public string prompt = "E  /  互动";
        public bool oneShot;
        public bool Used { get; private set; }
        public string Prompt => string.IsNullOrEmpty(prompt) ? "E  /  互动" : prompt;

        void Awake() { if (!emitter) emitter = GetComponent<LevelEventEmitter>(); }

        public bool CanReach(CombatPlayer player)
        {
            return isActiveAndEnabled && gameObject.scene.isLoaded && !Used && emitter &&
                (emitter.Kind == LevelEndpointKind.InteractionConfirmed || emitter.Kind == LevelEndpointKind.ExitReached) &&
                player && player.Alive && player.World && player.World.IsRunning && radius > 0 &&
                Vector2.Distance(player.transform.position, transform.position) <= radius &&
                !CombatSight.Blocked(player.transform.position, transform.position);
        }

        public bool Use(CombatPlayer player)
        {
            if (!CanReach(player)) return false;
            var result = emitter.Raise(player);
            if (result != LevelEventResult.Published) return false;
            if (oneShot) Used = true;
            return true;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
