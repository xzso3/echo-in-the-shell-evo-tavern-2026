using System.Collections.Generic;
using Echo.LevelToolkit.Combat;
using UnityEngine;

namespace Echo.NativeGame
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class NativeEnemy : MonoBehaviour
    {
        public enum MovementStyle { Chase, Orbit }
        // Legacy readers may enumerate this set; CombatWorld owns the actual actor registry.
        public static readonly HashSet<NativeEnemy> Active = new HashSet<NativeEnemy>();
        public NativeRunController run;
        public SpriteRenderer view;
        public float speed = 1.65f, health = 72, contactDamage = 15, detectionRange = 9;
        public MovementStyle movementStyle;
        public float preferredRange = 4.5f;
        public bool orbitClockwise = true;

        CombatEnemy sharedActor;
        public bool Alive => sharedActor ? sharedActor.Alive : health > 0;
        internal CombatEnemy SharedActor => sharedActor;

        void OnEnable()
        {
            Active.Add(this);
            if (sharedActor && sharedActor.Alive) sharedActor.enabled = true;
        }

        void OnDisable()
        {
            Active.Remove(this);
            if (sharedActor) sharedActor.enabled = false;
        }

        void OnDestroy()
        {
            if (sharedActor) sharedActor.Died -= OnSharedDied;
        }

        void Start()
        {
            if (!sharedActor && run && run.combatIntegration && run.combatIntegration.IsInitialized)
                run.combatIntegration.RegisterEnemy(this);
        }

        internal bool InitializeSharedActor(CombatWorld world)
        {
            if (sharedActor) return sharedActor.World == world;
            if (!world || !run || !isActiveAndEnabled) return false;
            sharedActor = GetComponent<CombatEnemy>();
            if (!sharedActor) return false;
            sharedActor.view = view;
            sharedActor.speed = speed;
            sharedActor.health = health;
            sharedActor.contactDamage = contactDamage;
            sharedActor.detectionRange = detectionRange;
            sharedActor.movementStyle = (CombatEnemy.MovementStyle)movementStyle;
            sharedActor.preferredRange = preferredRange;
            sharedActor.orbitClockwise = orbitClockwise;
            if (!sharedActor.Initialize(world)) return false;
            sharedActor.Died += OnSharedDied;

            var fan = GetComponent<NativeFanAttack>();
            return !fan || fan.InitializeSharedAttack();
        }

        void LateUpdate()
        {
            if (sharedActor) health = sharedActor.health;
        }

        void OnSharedDied(CombatEnemy actor)
        {
            health = 0;
        }

        public void ReceiveDamage(float amount)
        {
            if (!sharedActor || !run || !run.IsCombatAdvancing) return;
            sharedActor.ReceiveDamage(amount);
            health = sharedActor.health;
        }
    }
}
