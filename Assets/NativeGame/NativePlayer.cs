using Echo.LevelToolkit.Combat;
using UnityEngine;

namespace Echo.NativeGame
{
    // Serialized Native facade; CombatPlayer owns movement, health and hurt timing.
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class NativePlayer : MonoBehaviour
    {
        public NativeRunController run;
        public SpriteRenderer view;
        // Retained for saved Player prefab compatibility; NativeCombat owns the live weapon.
        public NativeProjectile projectilePrefab;
        public float speed = 4.5f, maxHealth = 100;

        CombatPlayer sharedActor;
        Vector2 pendingMoveInput;
        public float Health => sharedActor ? sharedActor.Health : maxHealth;
        public bool Alive => sharedActor ? sharedActor.Alive : maxHealth > 0;
        public Vector2 MoveInput
        {
            get => sharedActor ? sharedActor.MoveInput : pendingMoveInput;
            set
            {
                pendingMoveInput = value;
                if (sharedActor) sharedActor.MoveInput = value;
            }
        }
        internal CombatPlayer SharedActor => sharedActor;

        internal CombatPlayer PrepareSharedActor()
        {
            if (sharedActor || !isActiveAndEnabled || !CombatPlayer.Valid(speed) ||
                !CombatPlayer.Valid(maxHealth)) return null;
            sharedActor = GetComponent<CombatPlayer>();
            if (!sharedActor) return null;
            sharedActor.view = view;
            sharedActor.speed = speed;
            sharedActor.maxHealth = maxHealth;
            sharedActor.hurtCooldown = .65f;
            sharedActor.MoveInput = pendingMoveInput;
            return sharedActor;
        }

        void OnDisable() { if (sharedActor) sharedActor.enabled = false; }
        void OnEnable() { if (sharedActor) sharedActor.enabled = true; }

        public bool TryHeal(float amount)
        {
            return run && run.player == this && sharedActor && sharedActor.TryHeal(amount);
        }

        public void ReceiveDamage(float value)
        {
            if (run && run.player == this && sharedActor) sharedActor.ReceiveDamage(value);
        }
    }
}
