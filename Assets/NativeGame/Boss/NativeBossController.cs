using System;
using System.Collections.Generic;
using Echo.LevelToolkit.Combat;
using PlagueSurvivor;
using UnityEngine;

namespace Echo.NativeGame
{
    [RequireComponent(typeof(NativeBossActor), typeof(CircleCollider2D))]
    public sealed class NativeBossController : MonoBehaviour, INativeBossEncounter
    {
        public enum AttackStage { Dormant, Startup, Tracking, LockedBurst, Bombardment, Recovery, CoreWindow, Defeated }
        // Legacy readers still use this set; CombatWorld owns combat membership.
        public static readonly HashSet<NativeBossController> Active = new HashSet<NativeBossController>();
        public NativeRunController level;
        public NativeBossActor actor;
        [Tooltip("Existing BossArena art/config is reused for sprites and material only.")]
        public BossArenaConfig art;
        public NativeProjectile hostileProjectilePrefab;
        public SpriteRenderer baseView, turretView, coreView, damageView;
        public Transform turretPivot;
        [Min(.2f)] public float trackingSeconds = .9f, lockSeconds = .55f;
        [Min(.1f)] public float shotInterval = .22f;
        [Min(1)] public int burstCount = 3;
        [Min(1)] public float shotSpeed = 8, shotDamage = 12;
        [Min(.3f)] public float bombWarning = 1.25f, bombInterval = .7f;
        [Min(.3f)] public float bombRadius = 1.05f;
        [Min(1)] public float bombDamage = 18;
        [Min(1)] public float coreWindowSeconds = 6, retryWindowDelay = 8;
        [Min(1.3f)] public float coreInteractionRadius = 1.9f;
        public Vector2 muzzleOffset = new Vector2(1.05f, .18f);

        CombatBoss sharedActor;
        public event Action Defeated;
        public bool IsDefeated => sharedActor && sharedActor.IsDefeated;
        public bool CanBeTargeted => sharedActor && sharedActor.CanBeTargeted;
        public Vector2 AimPoint => sharedActor ? sharedActor.AimPoint : (Vector2)transform.position;
        public AttackStage Stage => sharedActor ? (AttackStage)sharedActor.Stage : AttackStage.Dormant;
        public string Cue => sharedActor ? sharedActor.Cue : "战斗机体 / 待机";
        public float ArmorFraction => sharedActor ? sharedActor.ArmorFraction : 1;
        public float CoreSecondsRemaining => sharedActor ? sharedActor.CoreSecondsRemaining : 0;
        public const float WeakpointSupportSeconds = CombatBoss.WeakpointSupportSeconds;
        public bool CanEnableWeakpointSupport => sharedActor && sharedActor.CanEnableWeakpointSupport;
        internal CombatBoss SharedActor => sharedActor;

        void OnEnable()
        {
            Active.Add(this);
            if (sharedActor && !sharedActor.IsDefeated) sharedActor.enabled = true;
        }

        void OnDisable()
        {
            Active.Remove(this);
            if (sharedActor) sharedActor.enabled = false;
        }

        void OnDestroy()
        {
            if (sharedActor) sharedActor.Defeated -= OnSharedDefeated;
        }

        internal bool InitializeSharedActor(CombatWorld world)
        {
            if (sharedActor) return sharedActor.World == world;
            if (!world || !level || !isActiveAndEnabled || !art || !hostileProjectilePrefab) return false;
            if (!actor) actor = GetComponent<NativeBossActor>();
            sharedActor = GetComponent<CombatBoss>();
            var projectile = hostileProjectilePrefab.GetComponent<CombatProjectile>();
            if (!actor || !sharedActor || !projectile) return false;

            sharedActor.hostileProjectilePrefab = projectile;
            sharedActor.baseView = baseView;
            sharedActor.turretView = turretView;
            sharedActor.coreView = coreView;
            sharedActor.damageView = damageView;
            sharedActor.turretPivot = turretPivot;
            sharedActor.wreckSprite = art.wreck;
            sharedActor.burstSprite = art.projectile;
            sharedActor.explosionSprites = art.explosions;
            sharedActor.maxArmor = actor.maxArmor;
            sharedActor.armorDamageScale = actor.armorDamageScale;
            sharedActor.trackingSeconds = trackingSeconds;
            sharedActor.lockSeconds = lockSeconds;
            sharedActor.shotInterval = shotInterval;
            sharedActor.burstCount = burstCount;
            sharedActor.shotSpeed = shotSpeed;
            sharedActor.shotDamage = shotDamage;
            sharedActor.bombWarning = bombWarning;
            sharedActor.bombInterval = bombInterval;
            sharedActor.bombRadius = bombRadius;
            sharedActor.bombDamage = bombDamage;
            sharedActor.coreWindowSeconds = coreWindowSeconds;
            sharedActor.retryWindowDelay = retryWindowDelay;
            sharedActor.coreInteractionRadius = coreInteractionRadius;
            sharedActor.muzzleOffset = muzzleOffset;
            if (!sharedActor.Initialize(world)) return false;

            actor.BindSharedActor(sharedActor);
            sharedActor.Defeated += OnSharedDefeated;
            return true;
        }

        void OnSharedDefeated(CombatBoss defeated)
        {
            Defeated?.Invoke();
        }

        public CombatBoss.ActivationResult TryActivateEncounter()
        {
            if (!level || !level.Running) return CombatBoss.ActivationResult.NotRunning;
            if (!sharedActor) return CombatBoss.ActivationResult.MissingReference;
            return sharedActor.TryActivate();
        }

        public bool CancelUncommittedActivation()
        {
            return sharedActor && sharedActor.CancelUncommittedActivation();
        }

        public void ActivateEncounter()
        {
            var result = TryActivateEncounter();
            if (result != CombatBoss.ActivationResult.Started && result != CombatBoss.ActivationResult.AlreadyActive)
                Debug.LogError("NativeBossController: activation failed: " + result, this);
        }

        public bool TryEnableWeakpointSupport()
        {
            return sharedActor && sharedActor.TryEnableWeakpointSupport();
        }

        public void ReceiveDamage(float amount)
        {
            if (sharedActor) sharedActor.ReceiveDamage(amount);
        }

        public bool CanInteractCore(NativePlayer player)
        {
            return level && level.Running && player && player == level.player && sharedActor &&
                sharedActor.CanInteractCore(player.SharedActor);
        }

        public bool InteractCore(NativePlayer player)
        {
            return CanInteractCore(player) && sharedActor.InteractCore(player.SharedActor);
        }
    }
}
