using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PlagueSurvivor.Editor
{
    // Explicit MCP Play Mode verification; no test cheats are present in the shipped runtime.
    public static class BossArenaVerification
    {
        static readonly BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        static PlagueGame game;
        static BossEncounterController boss;
        static List<string> passed;
        static void Check(bool condition, string description)
        { if (!condition) throw new Exception("Boss verification failed: " + description); passed.Add(description); }
        static void Place(Vector2 p)
        { typeof(PlagueGame).GetField("playerPosition", Hidden).SetValue(game, p); game.player.transform.position = p; }
        static void Step(float seconds, Vector2 input)
        { for (int i = 0; i < Mathf.CeilToInt(seconds / .02f); i++) game.Tick(.02f, input); }
        static void StartFight()
        {
            game.Restart(); Place(boss.config.startPosition); game.Tick(.02f, Vector2.zero);
            Step(boss.config.startupSeconds + .02f, Vector2.zero);
        }
        public static string[] Run()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Enter BossArena Play Mode first.");
            game = UnityEngine.Object.FindObjectOfType<PlagueGame>(); boss = game.bossEncounter;
            if (!boss) throw new InvalidOperationException("Open BossArena.");
            passed = new List<string>(); bool wasEnabled = game.enabled; float range = game.attackRange;
            try
            {
                game.enabled = false; game.Restart();
                Check(game.city == null && game.EnemyCount == 0 && game.SpawnCount == 0, "Independent arena has no city collision or ordinary enemies");
                Check(game.equipment.Count == 0 && game.Loot.Count == 3, "Empty inventory, three real Common equipment drops");
                Check(boss.State == BossEncounterState.Preparation && boss.Health == 2600 && !game.Dialogue, "Dormant Boss and disabled dialogue");
                Step(1, Vector2.zero);
                Check(game.equipment.Count == 3 && game.Loot.Count == 0, "Preparation supplies use actual magnet pickup");
                Check(game.ShotsFired == 0 && boss.Warden.HazardCount == 0 && game.Elapsed == 0, "Preparation has no attacks or combat timer");
                Check(!game.StartDialogue(game.testDialogue), "Boss encounter rejects direct dialogue entry");
                float before = game.Health; game.DamagePlayer(20); Check(game.Health == before, "Preparation cannot take damage");
                Check(Vector2.Distance(boss.config.playerSpawn, boss.config.startPosition) > boss.config.startRadius, "Spawn does not activate the pad");
                Vector2 clipped = boss.MovePlayer(new Vector2(8, 5), new Vector2(100, 100));
                Check(clipped.x <= 8.621f && clipped.y <= 5.621f, "Arena edges respect player radius");
                Vector2 blocked = boss.MovePlayer(new Vector2(0, -2), Vector2.up * 2);
                Check(blocked.magnitude >= boss.config.bodyRadius + boss.config.playerRadius - .001f, "Central circle blocks movement without contact damage");
                StartFight(); Check(boss.State == BossEncounterState.PhaseOne, "Start pad runs countdown then enters phase one");
                Step(.7f, Vector2.zero); Check(boss.Health < boss.config.health && game.ShotsFired > 0, "Real auto-fired projectile damages Boss");
                game.SetEquipmentOpen(true);
                float hp = boss.Health, timer = game.Elapsed, clock = boss.Warden.SkillClock; Vector2 pos = game.PlayerPosition;
                Step(3, Vector2.one); game.DamagePlayer(20); boss.ReceiveDamage(500);
                Check(boss.Health == hp && game.Elapsed == timer && boss.Warden.SkillClock == clock && game.PlayerPosition == pos, "Inventory freezes movement, projectiles, skills and damage");
                game.SetEquipmentOpen(false);
                typeof(PlagueGame).GetField("manuallyPaused", Hidden).SetValue(game, true);
                Step(1, Vector2.zero); Check(game.Elapsed == timer, "ESC manual pause freezes encounter");
                game.SetEquipmentOpen(true); game.SetEquipmentOpen(false); Check(game.Paused, "Closing inventory preserves manual pause");
                typeof(PlagueGame).GetField("manuallyPaused", Hidden).SetValue(game, false);
                Step(.04f, Vector2.zero); Check(game.Elapsed > timer, "Unpause resumes original progress");
                game.attackRange = 0; game.ClearProjectiles();
                StartFight(); game.ClearProjectiles(); game.DamagePlayer(16); game.DamagePlayer(20);
                Check(game.Health == 84 && game.DamageEvents == 1, "All Boss damage shares player invulnerability");
                boss.ReceiveDamage(2100); boss.ResolveState();
                Check(boss.State == BossEncounterState.Transition && boss.Health == 500, "Crossing several thresholds retains legal damage and triggers transition");
                Check(boss.Supplies.PendingCount == 3 && boss.Warden.HazardCount == 0 && game.ProjectileCount == 0, "Transition clears dangers and queues all three threshold rewards");
                boss.ReceiveDamage(200); Check(boss.Health == 500, "Short transition protects Boss without healing");
                Step(.3f, Vector2.zero); game.SetEquipmentOpen(true); float deliveryClock = boss.Supplies.DeliveryClock;
                Step(2, Vector2.zero); Check(boss.Supplies.DeliveryClock == deliveryClock && boss.State == BossEncounterState.Transition, "Supply and phase countdown freeze in inventory");
                game.SetEquipmentOpen(false); Step(3, Vector2.zero);
                Check(boss.PhaseTwo && boss.State == BossEncounterState.PhaseTwo && boss.Supplies.Delivered == 3, "Finite queued deliveries finish and phase two starts");
                Check(boss.Supplies.MedicalCount == 2, "75 and 25 percent each drop one medical pack");
                boss.Supplies.ObserveHealth(.19f); Check(boss.Supplies.PendingCount == 0 && boss.Supplies.Delivered == 3, "Repeated thresholds never duplicate rewards");
                game.Heal(1000); Place(boss.config.supplyPoints[0]); boss.Supplies.Tick(.02f, true);
                Check(game.Health == 100 && boss.Supplies.MedicalCount == 2, "Full-health medical remains on ground");
                typeof(PlagueGame).GetField("invulnerability", Hidden).SetValue(game, 0f); game.DamagePlayer(10); boss.Supplies.Tick(.02f, true);
                Check(game.Health == 100 && boss.Supplies.MedicalCount == 1 && boss.Supplies.MedicalPicked == 1, "Medical heals to cap without inventory space");
                boss.ReceiveDamage(5000); boss.ResolveState();
                Check(boss.State == BossEncounterState.Victory && boss.Warden.HazardCount == 0 && game.ProjectileCount == 0 && boss.Supplies.PendingCount == 0 && game.Loot.Count == 0, "Victory clears all combat and supply objects");
                game.SetEquipmentOpen(true); Check(!game.EquipmentOpen, "Result screen prevents equipment editing");
                game.Restart(); Check(boss.State == BossEncounterState.Preparation && boss.Health == 2600 && game.Loot.Count == 3 && game.equipment.Count == 0 && boss.Supplies.Delivered == 0, "Restart resets inventory, rewards, phase and HP");
                StartFight(); boss.ReceiveDamage(5000); boss.ResolveState();
                Check(boss.State == BossEncounterState.Victory && !boss.PhaseTwo && boss.Supplies.PendingCount == 0, "Lethal burst wins immediately without forced phase lock");
                StartFight(); boss.ReceiveDamage(5000); game.DamagePlayer(500); boss.ResolveState();
                Check(boss.State == BossEncounterState.Defeat, "Same-step mutual death consistently prioritizes player defeat");
                VerifySkills();
                VerifyFullBag();
                for (int i = 0; i < 5; i++) { game.Restart(); Step(.02f, Vector2.zero); }
                Check(UnityEngine.Object.FindObjectsOfType<WardenBossController>().Length == 1 && boss.Warden.HazardCount == 0 && boss.Supplies.PendingCount == 0, "Five restarts retain one Boss controller and no old hazards");
                return passed.ToArray();
            }
            finally { game.attackRange = range; game.Restart(); game.enabled = wasEnabled; }
        }
        static void VerifySkills()
        {
            StartFight(); Place(new Vector2(4, 0)); boss.Warden.BeginSkill(WardenSkill.Burst);
            Step(.65f, Vector2.zero);
            Vector3 locked = GameObject.Find("Turret pivot").transform.eulerAngles;
            Step(.2f, Vector2.up);
            Check(GameObject.Find("Turret pivot").transform.eulerAngles == locked, "Burst direction stops tracking during final lock window");
            Step(3, Vector2.up); Check(boss.Warden.BurstHits == 0, "Naked sidestep evades locked burst");
            StartFight(); Place(new Vector2(4, 0)); boss.Warden.BeginSkill(WardenSkill.Bombardment);
            Step(3, Vector2.zero);
            Check(boss.Warden.BombHits == 1 && game.Health == 80, "Each bomb resolves once and consecutive marks share invulnerability");
            StartFight(); Place(new Vector2(4, 0)); boss.Warden.BeginSkill(WardenSkill.Bombardment);
            Step(3, Vector2.up); Check(boss.Warden.BombHits == 0, "Naked movement evades fixed bombs");
            StartFight(); Place(new Vector2(4, 3.5f)); boss.Warden.BeginSkill(WardenSkill.Grid);
            Step(2.5f, Vector2.zero); Check(boss.Warden.GridHits == 1 && game.Health == 82, "Grid damages once per activation even after invulnerability expires");
            StartFight(); Place(new Vector2(4, 3.5f)); boss.Warden.BeginSkill(WardenSkill.Grid);
            Step(.55f, Vector2.down); Step(2, Vector2.zero); Check(boss.Warden.GridHits == 0, "Naked player escapes active strip before activation");
            boss.ReceiveDamage(1400); boss.ResolveState(); Step(3, Vector2.zero);
            boss.Warden.BeginSkill(WardenSkill.Grid); bool first = boss.Warden.VerticalGrid;
            boss.Warden.BeginSkill(WardenSkill.Grid); Check(first != boss.Warden.VerticalGrid, "Phase two alternates grid orientation");
            // From a grid point, maximum exit distance is half strip width plus player radius.
            float margin = boss.config.gridWarning - (boss.config.gridWidth / 2 + boss.config.playerRadius) / game.playerSpeed;
            Check(margin > .9f && boss.config.gridCenter * 2 - boss.config.gridWidth > 2.5f + 2 * boss.config.playerRadius,
                "Grid has reaction margin and connected central safe corridor after radius allowance");
            Check(boss.config.bombWarning - (boss.config.bombRadius + boss.config.playerRadius) / game.playerSpeed > .7f,
                "Bomb escape at base movement speed retains reaction margin");
        }
        static void VerifyFullBag()
        {
            game.Restart(); game.Loot.Clear();
            for (int i = 0; i < 24; i++) game.equipment.TryAdd(game.equipmentCatalog.Create(EquipmentSlot.Feet, EquipmentQuality.Common));
            game.Loot.Spawn(game.equipmentCatalog.Create(EquipmentSlot.Weapon, EquipmentQuality.Rare), game.PlayerPosition);
            Step(.1f, Vector2.zero); Check(game.Loot.Count == 1 && game.Loot.NearbyBagFull, "Full inventory leaves supply equipment on ground with HUD feedback");
            game.SetEquipmentOpen(true); game.DeleteInventoryItem(0); game.SetEquipmentOpen(false); Step(.1f, Vector2.zero);
            Check(game.Loot.Count == 0 && game.equipment.Count == 24, "Freed slot collects preserved equipment");
        }
        public static string RunPlayableLoop(bool equipped)
        {
            game = UnityEngine.Object.FindObjectOfType<PlagueGame>(); boss = game.bossEncounter;
            bool enabled = game.enabled; game.enabled = false;
            try
            {
                game.Restart(); Step(1, Vector2.zero);
                if (equipped)
                {
                    game.SetEquipmentOpen(true);
                    for (int i = 0; i < PlayerEquipment.Capacity; i++) if (game.equipment.At(i) != null) game.EquipItem(i);
                    game.SetEquipmentOpen(false);
                }
                Place(boss.config.startPosition); game.Tick(.02f, Vector2.zero);
                float orbit = -Mathf.PI / 2;
                var observed = new HashSet<string>();
                for (int i = 0; i < 7000 && !boss.Finished; i++)
                {
                    Vector2 p = game.PlayerPosition, target;
                    if (boss.CombatActive && !boss.Warden.Recovering && boss.Warden.Skill == WardenSkill.Grid)
                        target = boss.Warden.VerticalGrid ? new Vector2(0, p.y >= 0 ? 4.7f : -4.7f) : new Vector2(p.x >= 0 ? 4.7f : -4.7f, 0);
                    else
                    {
                        orbit = Mathf.Atan2(p.y, p.x) + .35f;
                        target = new Vector2(Mathf.Cos(orbit), Mathf.Sin(orbit)) * 4.7f;
                    }
                    if (boss.CombatActive) observed.Add((boss.PhaseTwo ? "P2 " : "P1 ") + boss.Warden.Skill);
                    game.Tick(.02f, Vector2.ClampMagnitude((target - p) * 4, 1));
                }
                string report = (equipped ? "Common gear" : "Naked") + ": " + boss.State + ", combat=" + game.Elapsed.ToString("0.00") + "s, HP=" + game.Health +
                    ", damage events=" + game.DamageEvents + ", shots=" + game.ShotsFired + ", burst/bomb/grid=" + boss.Warden.BurstHits + "/" + boss.Warden.BombHits + "/" + boss.Warden.GridHits +
                    ", supplies=" + boss.Supplies.Delivered + ", observed=" + string.Join(",", observed);
                if (boss.State != BossEncounterState.Victory) throw new Exception(report);
                return report;
            }
            finally { game.Restart(); game.enabled = enabled; }
        }
    }
}
