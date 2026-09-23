using System;
using System.Collections.Generic;
using UnityEngine;

namespace Echo.LevelToolkit.Combat
{
    // Runtime-only boss. Visual sprites are optional except the core/pivot needed to present E timing.
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class CombatBoss : MonoBehaviour
    {
        public enum AttackStage { Dormant, Startup, Tracking, LockedBurst, Bombardment, Recovery, CoreWindow, Defeated }
        public enum ActivationResult { Started, AlreadyActive, NotRunning, MissingReference, InvalidConfiguration, Defeated }
        public CombatProjectile hostileProjectilePrefab;
        public SpriteRenderer baseView, turretView, coreView, damageView;
        public Transform turretPivot;
        public Sprite wreckSprite, burstSprite;
        public Sprite[] explosionSprites;
        public float maxArmor = 1400, armorDamageScale = .45f;
        public float trackingSeconds = .9f, lockSeconds = .55f, shotInterval = .22f;
        public int burstCount = 3;
        public float shotSpeed = 8, shotDamage = 12;
        public float bombWarning = 1.25f, bombInterval = .7f, bombRadius = 1.05f, bombDamage = 18;
        public float coreWindowSeconds = 6, retryWindowDelay = 8, coreInteractionRadius = 1.9f;
        public Vector2 muzzleOffset = new Vector2(1.05f, .18f);
        public const float WeakpointSupportSeconds = 3;
        public event Action<CombatBoss> Defeated;
        public CombatWorld World { get; private set; }
        public float Armor { get; private set; }
        public bool ArmorBroken => Armor <= 0;
        public bool IsDefeated { get; private set; }
        public bool IsEncounterActive => encounterActive;
        public bool CanBeTargeted => encounterActive && isActiveAndEnabled && World && World.IsRunning && !IsDefeated && !ArmorBroken;
        public bool CanEnableWeakpointSupport => CanRun && supportWindowBonus == 0;
        public Vector2 AimPoint => transform.position;
        public AttackStage Stage { get; private set; } = AttackStage.Dormant;
        public string Cue { get; private set; } = "战斗机体 / 待机";
        public float ArmorFraction => Mathf.Clamp01(Armor / Mathf.Max(1, maxArmor));
        public float CoreSecondsRemaining => Stage == AttackStage.CoreWindow ? Mathf.Max(0, EffectiveCoreWindowSeconds - stageAge) : 0;
        float EffectiveCoreWindowSeconds => coreWindowSeconds + supportWindowBonus;
        bool CanRun => encounterActive && isActiveAndEnabled && World && World.IsRunning && !IsDefeated;

        sealed class Bomb
        {
            internal Vector2 position; internal float age; internal bool exploded;
            internal LineRenderer ring; internal SpriteRenderer explosion;
        }
        readonly List<Bomb> bombs = new List<Bomb>();
        Transform hazards;
        LineRenderer aim;
        Vector2 lockedDirection = Vector2.right;
        float stageAge, nextShot, nextBomb, nextCoreAt, supportWindowBonus;
        int shots, bombsPlaced, attackNumber;
        bool encounterActive, emittedDefeat;
        static Color Amber => new Color(1, .65f, .15f);
        static Color Danger => new Color(1, .2f, .13f);
        void Awake() { Armor = maxArmor; }
        public bool Initialize(CombatWorld world)
        {
            if (World || !world || !isActiveAndEnabled || !ValidConfiguration()) return false;
            World = world;
            Armor = maxArmor;
            if (!world.Register(this)) { World = null; return false; }
            return true;
        }
        void OnEnable() { if (World && !IsDefeated) World.Register(this); }
        void OnDisable()
        {
            if (World) World.Unregister(this);
            ClearHazards();
            if (encounterActive && !IsDefeated) SetStage(AttackStage.Recovery, "攻击间隙 / 抓紧开火");
        }
        void OnDestroy() { if (World) World.Unregister(this); }
        bool ValidConfiguration()
        {
            return hostileProjectilePrefab && baseView && turretView && turretPivot && coreView &&
                CombatPlayer.Valid(maxArmor) && CombatPlayer.Valid(armorDamageScale) && armorDamageScale <= 1 &&
                CombatPlayer.Valid(trackingSeconds) && CombatPlayer.Valid(lockSeconds) && CombatPlayer.Valid(shotInterval) && burstCount > 0 &&
                CombatPlayer.Valid(shotSpeed) && CombatPlayer.Valid(shotDamage) && CombatPlayer.Valid(bombWarning) &&
                CombatPlayer.Valid(bombInterval) && CombatPlayer.Valid(bombRadius) && CombatPlayer.Valid(bombDamage) &&
                CombatPlayer.Valid(coreWindowSeconds) && CombatPlayer.Valid(retryWindowDelay) && CombatPlayer.Valid(coreInteractionRadius) &&
                !float.IsNaN(muzzleOffset.x) && !float.IsNaN(muzzleOffset.y) && !float.IsInfinity(muzzleOffset.x) && !float.IsInfinity(muzzleOffset.y);
        }
        public ActivationResult TryActivate()
        {
            if (IsDefeated) return ActivationResult.Defeated;
            if (encounterActive) return ActivationResult.AlreadyActive;
            if (!World || !World.IsRunning || !isActiveAndEnabled) return ActivationResult.NotRunning;
            if (!hostileProjectilePrefab || !baseView || !turretView || !turretPivot || !coreView) return ActivationResult.MissingReference;
            if (!ValidConfiguration()) return ActivationResult.InvalidConfiguration;
            encounterActive = true; attackNumber = 0;
            SetStage(AttackStage.Startup, "战斗机体 / 击破外壳");
            return ActivationResult.Started;
        }
        public bool TryEnableWeakpointSupport()
        {
            if (!CanEnableWeakpointSupport) return false;
            supportWindowBonus = WeakpointSupportSeconds;
            return true;
        }
        void Update()
        {
            if (!encounterActive || IsDefeated) return;
            if (!World || !World.IsRunning) { ClearHazards(); return; }
            stageAge += Time.deltaTime;
            if (damageView) damageView.enabled = ArmorFraction < .5f;
            if (coreView) coreView.color = Stage == AttackStage.CoreWindow
                ? Color.Lerp(Color.cyan, Color.white, .5f + .5f * Mathf.Sin(Time.time * 7)) : Amber;
            UpdateBombs();
            if (ArmorBroken && Stage != AttackStage.CoreWindow && Time.time >= nextCoreAt) { OpenCore(); return; }
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
        Vector2 Muzzle => (Vector2)transform.position + lockedDirection * muzzleOffset.x +
            new Vector2(-lockedDirection.y, lockedDirection.x) * muzzleOffset.y;
        void AimAtPlayer()
        {
            Vector2 delta = (Vector2)World.Player.transform.position - (Vector2)transform.position;
            float angle = Mathf.Atan2(delta.y, delta.x) - Mathf.Asin(Mathf.Clamp(muzzleOffset.y / Mathf.Max(.01f, delta.magnitude), -1, 1));
            lockedDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            turretPivot.rotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
            UpdateAim(Amber, .04f);
        }
        void UpdateAim(Color color, float width)
        {
            if (!aim) return;
            Vector2 start = Muzzle;
            aim.startColor = aim.endColor = color; aim.startWidth = aim.endWidth = width;
            aim.SetPosition(0, start);
            aim.SetPosition(1, CombatSight.EndAtObstacle(start, lockedDirection, 16));
        }
        void FirePulse()
        {
            var shot = Instantiate(hostileProjectilePrefab, Muzzle, Quaternion.identity, HazardRoot());
            shot.speed = shotSpeed; shot.lifetime = 3; shot.radius = .11f;
            var sprite = shot.GetComponent<SpriteRenderer>();
            if (sprite) { if (burstSprite) sprite.sprite = burstSprite; sprite.color = Color.white; }
            shot.gameObject.SetActive(true);
            if (!shot.Launch(World, this, lockedDirection, shotDamage, true)) Destroy(shot.gameObject);
        }
        void PlaceBomb()
        {
            var bomb = new Bomb { position = World.Player.transform.position, ring = MakeLine("Fixed blast boundary", Amber, .065f) };
            bomb.ring.loop = true; bomb.ring.positionCount = 48;
            for (int i = 0; i < 48; i++)
            {
                float angle = i * Mathf.PI * 2 / 48;
                bomb.ring.SetPosition(i, bomb.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * bombRadius);
            }
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
                        foreach (var hit in Physics2D.OverlapCircleAll(bomb.position, bombRadius))
                            if (hit.GetComponentInParent<CombatPlayer>() == World.Player &&
                                !CombatSight.Blocked(bomb.position, World.Player.transform.position))
                            { World.Player.ReceiveDamage(bombDamage); break; }
                        if (explosionSprites != null && explosionSprites.Length > 0 && explosionSprites[0])
                        {
                            var visual = new GameObject("Boss blast"); visual.transform.SetParent(HazardRoot(), true);
                            visual.transform.position = bomb.position; visual.transform.localScale = Vector3.one * bombRadius / 2;
                            bomb.explosion = visual.AddComponent<SpriteRenderer>(); bomb.explosion.sprite = explosionSprites[0];
                            bomb.explosion.sortingOrder = 240;
                        }
                    }
                }
                else
                {
                    if (bomb.explosion && explosionSprites != null && explosionSprites.Length > 0)
                        bomb.explosion.sprite = explosionSprites[Mathf.Min((int)((bomb.age - bombWarning) * 12), explosionSprites.Length - 1)];
                    if (bomb.age >= bombWarning + .35f)
                    {
                        if (bomb.ring) Destroy(bomb.ring.gameObject);
                        if (bomb.explosion) Destroy(bomb.explosion.gameObject);
                        bombs.RemoveAt(i);
                    }
                }
            }
        }
        Transform HazardRoot()
        {
            if (!hazards) { hazards = new GameObject("Boss encounter hazards").transform; hazards.SetParent(transform, true); }
            return hazards;
        }
        LineRenderer MakeLine(string label, Color color, float width)
        {
            var line = new GameObject(label).AddComponent<LineRenderer>(); line.transform.SetParent(HazardRoot(), false);
            line.useWorldSpace = true; line.sharedMaterial = baseView.sharedMaterial;
            line.startColor = line.endColor = color; line.startWidth = line.endWidth = width; line.sortingOrder = 210;
            return line;
        }
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
        public bool ReceiveDamage(float amount)
        {
            if (!CanBeTargeted || !CombatPlayer.Valid(amount)) return false;
            Armor = Mathf.Max(0, Armor - amount * armorDamageScale);
            if (ArmorBroken) OpenCore();
            return true;
        }
        public bool CanInteractCore(CombatPlayer player)
        {
            return CanRun && ArmorBroken && Stage == AttackStage.CoreWindow && stageAge < EffectiveCoreWindowSeconds &&
                player && player == World.Player && player.Alive &&
                Vector2.Distance(player.transform.position, AimPoint) <= coreInteractionRadius &&
                !CombatSight.Blocked(player.transform.position, AimPoint);
        }
        public bool InteractCore(CombatPlayer player)
        {
            if (!CanInteractCore(player)) return false;
            IsDefeated = true;
            SetStage(AttackStage.Defeated, "战斗机体 / 核心已断开"); ClearHazards();
            turretPivot.gameObject.SetActive(false); if (damageView) damageView.enabled = false;
            if (wreckSprite) baseView.sprite = wreckSprite;
            var collider = GetComponent<CircleCollider2D>(); if (collider) collider.enabled = false;
            if (!emittedDefeat) { emittedDefeat = true; Defeated?.Invoke(this); }
            return true;
        }
    }
}
