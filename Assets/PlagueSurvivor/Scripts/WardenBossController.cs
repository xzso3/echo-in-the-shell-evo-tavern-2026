using System.Collections.Generic;
using UnityEngine;

namespace PlagueSurvivor
{
    public enum WardenSkill { Burst, Bombardment, Grid }
    public sealed class WardenBossController : MonoBehaviour
    {
        sealed class Pulse { public SpriteRenderer view; public Vector2 position, velocity; }
        sealed class Bomb { public Vector2 position; public float age; public bool exploded; public Transform root; public LineRenderer progress; public SpriteRenderer effect; }
        BossEncounterController encounter;
        BossArenaConfig config;
        PlagueGame game;
        Transform body, turretRoot, hazards, grid;
        SpriteRenderer baseView, turretView, core, damage;
        LineRenderer aim;
        SpriteRenderer aimEnd;
        readonly List<Pulse> pulses = new List<Pulse>();
        readonly List<Bomb> bombs = new List<Bomb>();
        float clock, recoveryClock, nextShot, nextBomb, angle, animationClock;
        int emitted, bombCount, sequence, gridActivations;
        bool recovering, gridOn, gridHit, vertical;
        public WardenSkill Skill { get; private set; }
        public bool Recovering { get { return recovering; } }
        public int HazardCount { get { return pulses.Count + bombs.Count + (grid ? 1 : 0) + (aim ? 1 : 0); } }
        public int BurstHits { get; private set; }
        public int BombHits { get; private set; }
        public int GridHits { get; private set; }
        public string Cue { get; private set; }
        public float SkillClock { get { return clock; } }
        public bool VerticalGrid { get { return vertical; } }
        public void Initialize(BossEncounterController owner, PlagueGame playerGame)
        {
            encounter = owner; config = owner.config; game = playerGame;
            body = new GameObject("WARDEN-01").transform; body.SetParent(transform, false);
            baseView = BossArenaVisual.Sprite("Deployment base", config.baseSprite, Vector2.zero, body, config, 90);
            turretRoot = new GameObject("Turret pivot").transform; turretRoot.SetParent(body, false);
            turretView = BossArenaVisual.Sprite("Turret", config.turret, Vector2.zero, turretRoot, config, 110);
            core = BossArenaVisual.Sprite("Core", config.core, Vector2.zero, turretRoot, config, 112);
            core.transform.localPosition = new Vector3(-.3125f, .03125f, 0);
            damage = BossArenaVisual.Sprite("Overload cracks", config.damageOverlay, Vector2.zero, turretRoot, config, 113);
            hazards = new GameObject("Boss Hazards").transform; hazards.SetParent(transform, false);
        }
        public void ResetBoss()
        {
            ClearHazards(); sequence = 0; gridActivations = 0; animationClock = 0;
            BurstHits = BombHits = GridHits = 0;
            baseView.sprite = config.baseSprite; turretRoot.gameObject.SetActive(true); damage.gameObject.SetActive(false);
            turretRoot.rotation = Quaternion.identity; core.color = Color.cyan; Cue = "SYSTEM DORMANT";
        }
        public void BeginPhase()
        { sequence = 0; BeginSkill(WardenSkill.Burst); }
        public void ShowOverload()
        { damage.gameObject.SetActive(true); core.color = new Color(1, .3f, .08f); }
        public void BeginSkill(WardenSkill skill)
        {
            ClearHazards(); Skill = skill; clock = 0; emitted = 0; nextShot = config.trackSeconds + config.lockSeconds;
            nextBomb = 0; bombCount = encounter.PhaseTwo ? 3 : 2; recovering = false; gridOn = gridHit = false;
            if (skill == WardenSkill.Burst)
            {
                aim = BossArenaVisual.Line("Tracking laser", hazards, config, BossArenaVisual.Warning, .035f);
                aimEnd = BossArenaVisual.Sprite("Aim endpoint", config.aimTracking, game.PlayerPosition, hazards, config, 65);
                TrackPlayer(); Cue = "LOCK-ON  /  WAIT FOR LOCK, THEN SIDESTEP";
            }
            if (skill == WardenSkill.Bombardment) Cue = "BOMBARDMENT  /  KEEP MOVING";
            if (skill == WardenSkill.Grid)
            {
                vertical = encounter.PhaseTwo && (++gridActivations % 2 == 1);
                grid = BossArenaVisual.Grid(vertical, hazards, config, false);
                Cue = vertical ? "VERTICAL GRID  /  MOVE TO CLEAR LANES" : "HORIZONTAL GRID  /  MOVE TO CLEAR LANES";
            }
        }
        Vector2 Direction { get { return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)); } }
        Vector2 Muzzle { get { return (Vector2)(Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg) * config.muzzleOffset); } }
        void TrackPlayer()
        {
            // Offset the bearing so the muzzle ray, not the pivot ray, passes through the target.
            Vector2 d = game.PlayerPosition + Vector2.up * .4f;
            angle = Mathf.Atan2(d.y, d.x) - Mathf.Asin(Mathf.Clamp(config.muzzleOffset.y / Mathf.Max(.01f, d.magnitude), -1, 1));
            turretRoot.rotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
        }
        public void Tick(float dt)
        {
            animationClock += dt;
            damage.gameObject.SetActive(encounter.PhaseTwo);
            Color tint = encounter.PhaseTwo ? new Color(1, .3f, .08f) : new Color(.08f, .9f, 1);
            core.color = Color.Lerp(tint, Color.white, .2f + .15f * Mathf.Sin(animationClock * 5));
            if (recovering)
            {
                // Supply telegraphs reserve recovery time, so a new attack never covers a landing.
                recoveryClock -= dt;
                if (recoveryClock <= 0 && !encounter.Supplies.DeliveryPending) { sequence = (sequence + 1) % 3; BeginSkill((WardenSkill)sequence); }
                return;
            }
            clock += dt;
            if (Skill == WardenSkill.Burst) TickBurst(dt);
            else if (Skill == WardenSkill.Bombardment) TickBombs(dt);
            else TickGrid();
        }
        void TickBurst(float dt)
        {
            if (clock <= config.trackSeconds) TrackPlayer();
            float fireTime = config.trackSeconds + config.lockSeconds;
            if (aim)
            {
                bool locked = clock >= config.trackSeconds;
                aim.startColor = aim.endColor = locked ? Color.white : BossArenaVisual.Warning;
                aim.startWidth = aim.endWidth = locked ? .075f : .035f;
                Vector2 origin = Muzzle, dir = Direction;
                float tx = Mathf.Abs(dir.x) < .001f ? 100 : ((dir.x > 0 ? config.halfSize.x : -config.halfSize.x) - origin.x) / dir.x;
                float ty = Mathf.Abs(dir.y) < .001f ? 100 : ((dir.y > 0 ? config.halfSize.y : -config.halfSize.y) - origin.y) / dir.y;
                Vector2 end = origin + dir * Mathf.Min(tx, ty);
                BossArenaVisual.Segment(aim, origin, end); aimEnd.transform.position = end;
                aimEnd.sprite = locked ? config.aimLocked : config.aimTracking;
                if (locked) Cue = "LOCKED  /  SIDESTEP NOW";
            }
            if (emitted < config.burstCount && clock >= nextShot)
            {
                var view = BossArenaVisual.Sprite("Hostile pulse", config.projectile, Muzzle, hazards, config, 310);
                view.transform.rotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
                pulses.Add(new Pulse { view = view, position = Muzzle, velocity = Direction * config.shotSpeed });
                emitted++; nextShot += config.shotInterval;
                if (emitted == config.burstCount) { BossArenaVisual.Remove(aim.transform); aim = null; BossArenaVisual.Remove(aimEnd.transform); aimEnd = null; }
            }
            for (int i = pulses.Count - 1; i >= 0; i--)
            {
                var p = pulses[i]; Vector2 old = p.position; p.position += p.velocity * dt;
                bool hit = PlagueGame.SegmentDistance(game.PlayerPosition + Vector2.up * .4f, old, p.position) < .42f;
                if (hit) { int before = game.DamageEvents; game.DamagePlayer(config.shotDamage); BurstHits += game.DamageEvents - before; }
                if (hit || Mathf.Abs(p.position.x) > config.halfSize.x || Mathf.Abs(p.position.y) > config.halfSize.y)
                { BossArenaVisual.Remove(p.view.transform); pulses.RemoveAt(i); }
                else p.view.transform.position = p.position;
            }
            if (emitted == config.burstCount && pulses.Count == 0) Recover(config.burstRecovery);
        }
        void TickBombs(float dt)
        {
            if (emitted < bombCount && clock >= nextBomb)
            {
                Vector2 p = game.PlayerPosition;
                var root = BossArenaVisual.Circle("Fixed bomb warning", p, config.bombRadius, hazards, config, BossArenaVisual.Warning);
                var progress = BossArenaVisual.Line("Fuse progress", root, config, Color.white, .085f, 62);
                BossArenaVisual.Segment(progress, p + Vector2.left * .3f, p + Vector2.right * .3f);
                bombs.Add(new Bomb { position = p, root = root, progress = progress });
                emitted++; nextBomb += config.bombInterval;
            }
            for (int i = bombs.Count - 1; i >= 0; i--)
            {
                var b = bombs[i]; b.age += dt;
                if (!b.exploded)
                {
                    float t = Mathf.Clamp01(b.age / config.bombWarning);
                    BossArenaVisual.Segment(b.progress, b.position + Vector2.left * .35f, b.position + Vector2.left * .35f + Vector2.right * .7f * t);
                    if (b.age >= config.bombWarning)
                    {
                        b.exploded = true;
                        foreach (var line in b.root.GetComponentsInChildren<LineRenderer>()) line.startColor = line.endColor = BossArenaVisual.Active;
                        if (Vector2.Distance(game.PlayerPosition, b.position) <= config.bombRadius + config.playerRadius)
                        { int before = game.DamageEvents; game.DamagePlayer(config.bombDamage); BombHits += game.DamageEvents - before; }
                        b.effect = BossArenaVisual.Sprite("Explosion", config.explosions[0], b.position, b.root, config, 75);
                        b.effect.transform.localScale = Vector3.one * config.bombRadius / 2;
                        b.effect.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                    }
                }
                else
                {
                    int frame = Mathf.Clamp((int)((b.age - config.bombWarning) * 12), 0, config.explosions.Length - 1);
                    b.effect.sprite = config.explosions[frame];
                    if (b.age > config.bombWarning + .34f) { BossArenaVisual.Remove(b.root); bombs.RemoveAt(i); }
                }
            }
            if (emitted == bombCount && bombs.Count == 0) Recover(config.bombRecovery);
        }
        public bool InGrid(Vector2 point)
        { return Mathf.Abs(Mathf.Abs(vertical ? point.x : point.y) - config.gridCenter) <= config.gridWidth / 2 + config.playerRadius; }
        void TickGrid()
        {
            if (!gridOn && clock >= config.gridWarning)
            { BossArenaVisual.Remove(grid); grid = BossArenaVisual.Grid(vertical, hazards, config, true); gridOn = true; Cue = "GRID LIVE  /  HOLD THE CLEAR LANE"; }
            if (gridOn && !gridHit && clock < config.gridWarning + config.gridActive && InGrid(game.PlayerPosition))
            { int before = game.DamageEvents; game.DamagePlayer(config.gridDamage); if (game.DamageEvents > before) { gridHit = true; GridHits++; } }
            if (clock >= config.gridWarning + config.gridActive) { BossArenaVisual.Remove(grid); grid = null; Recover(config.gridRecovery); }
        }
        void Recover(float duration)
        { recovering = true; recoveryClock = duration * (encounter.PhaseTwo ? config.phaseTwoRecovery : 1); Cue = "COOLING  /  ATTACK OR COLLECT SUPPLIES"; }
        public void ClearHazards()
        {
            if (hazards) for (int i = hazards.childCount - 1; i >= 0; i--) BossArenaVisual.Remove(hazards.GetChild(i));
            pulses.Clear(); bombs.Clear(); aim = null; aimEnd = null; grid = null; recovering = false;
        }
        public void SetWreck()
        { ClearHazards(); turretRoot.gameObject.SetActive(false); baseView.sprite = config.wreck; }
    }
}
