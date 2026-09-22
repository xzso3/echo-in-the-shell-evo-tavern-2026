using System;
using System.Collections.Generic;
using UnityEngine;
using PlagueSurvivor;

namespace Echo.NativeGame
{
    [RequireComponent(typeof(NativeBossActor), typeof(CircleCollider2D))]
    public sealed class NativeBossController : MonoBehaviour, INativeBossEncounter
    {
        public enum AttackStage { Dormant, Startup, Tracking, LockedBurst, Bombardment, Recovery, CoreWindow, Defeated }
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
        public event Action Defeated;
        public bool IsDefeated => actor && actor.IsDefeated;
        public bool CanBeTargeted => encounterActive && isActiveAndEnabled && level && level.Running && actor && !actor.IsDefeated && !actor.ArmorBroken;
        public Vector2 AimPoint => transform.position;
        public AttackStage Stage { get; private set; }
        public string Cue { get; private set; } = "战斗机体 / 待机";
        public float ArmorFraction => actor ? Mathf.Clamp01(actor.Armor / Mathf.Max(1, actor.maxArmor)) : 0;
        public float CoreSecondsRemaining => Stage == AttackStage.CoreWindow ? Mathf.Max(0, EffectiveCoreWindowSeconds - stageAge) : 0;

        float supportWindowBonus;
        public const float WeakpointSupportSeconds = 3;
        float EffectiveCoreWindowSeconds => coreWindowSeconds + supportWindowBonus;
        public bool CanEnableWeakpointSupport => encounterActive && isActiveAndEnabled && level && level.Running &&
            actor && !actor.IsDefeated && supportWindowBonus == 0;
        // A real per-encounter timing change: current (if open) and all future windows gain three seconds.
        public bool TryEnableWeakpointSupport()
        {
            if (!CanEnableWeakpointSupport) return false;
            supportWindowBonus = WeakpointSupportSeconds;
            return true;
        }

        sealed class Bomb
        {
            internal Vector2 position; internal float age; internal bool exploded;
            internal LineRenderer ring; internal SpriteRenderer explosion;
        }
        readonly List<Bomb> bombs = new List<Bomb>();
        Transform hazards;
        LineRenderer aim;
        Vector2 lockedDirection = Vector2.right;
        float stageAge, nextShot, nextBomb, nextCoreAt;
        int shots, bombsPlaced, attackNumber;
        bool encounterActive, emittedDefeat;
        static Color Amber => new Color(1, .65f, .15f);
        static Color Danger => new Color(1, .2f, .13f);
        void Awake()
        {
            if (!actor) actor = GetComponent<NativeBossActor>();
            Stage = AttackStage.Dormant;
        }
        void OnEnable() { Active.Add(this); }
        void Start() { if (!level) level = FindObjectOfType<NativeRunController>(); }
        void OnDisable()
        {
            Active.Remove(this); ClearHazards();
            if (encounterActive && !IsDefeated) SetStage(AttackStage.Recovery, "攻击间隙 / 抓紧开火");
        }
        public void ActivateEncounter()
        {
            if (encounterActive || IsDefeated) return;
            if (!level) level = FindObjectOfType<NativeRunController>();
            if (!level || !level.Running || !level.player || !level.combat || !actor || !art || !hostileProjectilePrefab || !baseView || !turretView || !turretPivot || !coreView)
            { Debug.LogError("NativeBossController: assign Level, Actor, BossArena art, projectile and visual references before activation.", this); return; }
            encounterActive = true; attackNumber = 0;
            SetStage(AttackStage.Startup, "战斗机体 / 击破外壳");
        }
        void Update()
        {
            if (!encounterActive || IsDefeated) return;
            if (!level || !level.Running) { ClearHazards(); return; }
            stageAge += Time.deltaTime;
            if (damageView) damageView.enabled = ArmorFraction < .5f;
            coreView.color = Stage == AttackStage.CoreWindow ? Color.Lerp(Color.cyan, Color.white, .5f + .5f * Mathf.Sin(Time.time * 7)) : Amber;
            UpdateBombs();
            if (actor.ArmorBroken && Stage != AttackStage.CoreWindow && Time.time >= nextCoreAt) { OpenCore(); return; }
            switch (Stage)
            {
                case AttackStage.Startup: if (stageAge >= 1.6f) BeginAttack(); break;
                case AttackStage.Tracking:
                    AimAtPlayer();
                    if (stageAge >= trackingSeconds) { SetStage(AttackStage.LockedBurst, "瞄准已锁定 / 向侧面躲开射线"); shots = 0; nextShot = lockSeconds; UpdateAim(Color.white, .075f); }
                    break;
                case AttackStage.LockedBurst:
                    if (shots < burstCount && stageAge >= nextShot) { FirePulse(); shots++; nextShot += shotInterval; }
                    if (shots >= burstCount) { RemoveAim(); SetStage(AttackStage.Recovery, "攻击间隙 / 抓紧开火"); }
                    break;
                case AttackStage.Bombardment:
                    if (bombsPlaced < 3 && stageAge >= nextBomb) { PlaceBomb(); bombsPlaced++; nextBomb += bombInterval; }
                    if (bombsPlaced >= 3 && bombs.Count == 0) SetStage(AttackStage.Recovery, "攻击间隙 / 抓紧开火");
                    break;
                case AttackStage.Recovery: if (stageAge >= 1.15f) BeginAttack(); break;
                case AttackStage.CoreWindow:
                    if (stageAge >= EffectiveCoreWindowSeconds) { nextCoreAt = Time.time + retryWindowDelay; BeginAttack(); }
                    break;
            }
        }
        void SetStage(AttackStage stage, string cue) { Stage = stage; stageAge = 0; Cue = cue; }
        void BeginAttack()
        {
            if ((attackNumber++ % 2) == 0)
            {
                SetStage(AttackStage.Tracking, "正在追踪 / 等待锁定后向侧面闪避");
                aim = MakeLine("Boss aim warning", Amber, .04f); aim.positionCount = 2; AimAtPlayer();
            }
            else { SetStage(AttackStage.Bombardment, "轰炸预警 / 离开橙色区域"); bombsPlaced = 0; nextBomb = 0; }
        }
        Vector2 Muzzle => (Vector2)transform.position + lockedDirection * muzzleOffset.x + new Vector2(-lockedDirection.y, lockedDirection.x) * muzzleOffset.y;
        void AimAtPlayer()
        {
            Vector2 delta = (Vector2)level.player.transform.position - (Vector2)transform.position;
            float angle = Mathf.Atan2(delta.y, delta.x) - Mathf.Asin(Mathf.Clamp(muzzleOffset.y / Mathf.Max(.01f, delta.magnitude), -1, 1));
            lockedDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            turretPivot.rotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
            UpdateAim(Amber, .04f);
        }
        void UpdateAim(Color color, float width)
        {
            if (!aim) return;
            Vector2 start = Muzzle, end = start + lockedDirection * 16;
            foreach (var hit in Physics2D.RaycastAll(start, lockedDirection, 16))
                if (hit.collider.GetComponentInParent<NativeObstacle>()) { end = hit.point; break; }
            aim.startColor = aim.endColor = color; aim.startWidth = aim.endWidth = width;
            aim.SetPosition(0, start); aim.SetPosition(1, end);
        }
        void FirePulse()
        {
            var shot = Instantiate(hostileProjectilePrefab, Muzzle, Quaternion.identity, HazardRoot());
            shot.speed = shotSpeed; shot.lifetime = 3; shot.radius = .11f;
            var sprite = shot.GetComponent<SpriteRenderer>(); if (sprite) { sprite.sprite = art.projectile; sprite.color = Color.white; }
            shot.LaunchHostile(lockedDirection, shotDamage, level.combat, this);
        }
        void PlaceBomb()
        {
            var bomb = new Bomb { position = level.player.transform.position, ring = MakeLine("Fixed blast boundary", Amber, .065f) };
            bomb.ring.loop = true; bomb.ring.positionCount = 48;
            for (int i = 0; i < 48; i++) { float a = i * Mathf.PI * 2 / 48; bomb.ring.SetPosition(i, bomb.position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * bombRadius); }
            bombs.Add(bomb);
        }
        void UpdateBombs()
        {
            for (int i = bombs.Count - 1; i >= 0; i--)
            {
                var bomb = bombs[i]; bomb.age += Time.deltaTime;
                if (!bomb.exploded)
                {
                    float progress = Mathf.Clamp01(bomb.age / bombWarning);
                    bomb.ring.startColor = bomb.ring.endColor = Color.Lerp(Amber, Danger, progress);
                    bomb.ring.startWidth = bomb.ring.endWidth = .045f + .065f * progress;
                    if (bomb.age >= bombWarning)
                    {
                        bomb.exploded = true;
                        // Same radius as the warning; actual player collider determines overlap.
                        foreach (var hit in Physics2D.OverlapCircleAll(bomb.position, bombRadius))
                            if (hit.GetComponentInParent<NativePlayer>() == level.player && !NativeObstacle.Blocked(bomb.position, level.player.transform.position)) { level.combat.HitPlayer(bombDamage); break; }
                        if (art.explosions != null && art.explosions.Length > 0)
                        {
                            bomb.explosion = BossArenaVisual.Sprite("Boss blast", art.explosions[0], bomb.position, HazardRoot(), art, 240);
                            bomb.explosion.transform.localScale = Vector3.one * bombRadius / 2;
                        }
                    }
                }
                else
                {
                    if (bomb.explosion && art.explosions.Length > 0) bomb.explosion.sprite = art.explosions[Mathf.Min((int)((bomb.age - bombWarning) * 12), art.explosions.Length - 1)];
                    if (bomb.age >= bombWarning + .35f) { if (bomb.ring) Destroy(bomb.ring.gameObject); if (bomb.explosion) Destroy(bomb.explosion.gameObject); bombs.RemoveAt(i); }
                }
            }
        }
        Transform HazardRoot()
        {
            if (!hazards) { hazards = new GameObject("Boss encounter hazards").transform; hazards.SetParent(transform, true); }
            return hazards;
        }
        LineRenderer MakeLine(string label, Color color, float width) => BossArenaVisual.Line(label, HazardRoot(), art, color, width, 60);
        void RemoveAim() { if (aim) { aim.gameObject.SetActive(false); Destroy(aim.gameObject); aim = null; } }
        void ClearHazards()
        {
            bombs.Clear(); aim = null;
            if (hazards) { hazards.gameObject.SetActive(false); Destroy(hazards.gameObject); hazards = null; }
        }
        void OpenCore()
        {
            ClearHazards(); SetStage(AttackStage.CoreWindow, "核心已暴露 / 靠近并按 E");
            turretPivot.rotation = Quaternion.identity;
        }
        public void ReceiveDamage(float amount)
        {
            if (!CanBeTargeted) return;
            actor.ReceiveDamage(amount);
            if (actor.ArmorBroken) OpenCore();
        }
        public bool CanInteractCore(NativePlayer player)
        {
            return isActiveAndEnabled && encounterActive && !IsDefeated && actor && actor.ArmorBroken && Stage == AttackStage.CoreWindow && stageAge < EffectiveCoreWindowSeconds &&
                level && level.Running && player && player == level.player && player.Alive &&
                Vector2.Distance(player.transform.position, AimPoint) <= coreInteractionRadius && !NativeObstacle.Blocked(player.transform.position, AimPoint);
        }
        public bool InteractCore(NativePlayer player)
        {
            if (!CanInteractCore(player) || !actor.FinishCore()) return false;
            SetStage(AttackStage.Defeated, "战斗机体 / 核心已断开"); ClearHazards();
            turretPivot.gameObject.SetActive(false); if (damageView) damageView.enabled = false;
            baseView.sprite = art.wreck; var collider = GetComponent<CircleCollider2D>(); if (collider) collider.enabled = false;
            if (!emittedDefeat) { emittedDefeat = true; Defeated?.Invoke(); }
            return true;
        }
    }
}
