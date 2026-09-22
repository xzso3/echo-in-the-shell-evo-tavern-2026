using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace PlagueSurvivor
{
    public sealed class PlagueGame : MonoBehaviour
    {
        [Header("Arena")]
        public Vector2 arenaHalfSize = new Vector2(14, 8);
        public float spawnSafeDistance = 6;
        public float spawnInterval = 1.15f;
        public int enemyLimit = 65;
        public CyberCityWorld city;
        [Header("Optional independent Boss encounter")]
        public BossEncounterController bossEncounter;
        public Vector2 PlayerPosition { get { return playerPosition; } }
        public bool IsDead { get { return dead; } }
        public int ProjectileCount { get { return shots.Count; } }
        [Header("Survivor")]
        public SpriteRenderer player;
        public float playerSpeed = 4.5f;
        public float maxHealth = 100;
        public float attackRange = 9;
        public float attackInterval = .48f;
        public float boltDamage = 22;
        [Header("Enemy Health")]
        [Min(1)] public float meleeBaseHealth = 58;
        [Min(1)] public float rangedBaseHealth = 82;
        [Header("Enemy prefabs")]
        public SpriteRenderer meleePrefab;
        public SpriteRenderer rangedPrefab;
        public SpriteRenderer boltPrefab;
        public SpriteRenderer acidPrefab;
        [Header("UI")]
        public TMP_Text status;
        public TMP_Text timer;
        public TMP_Text message;
        public UnityEngine.UI.Image healthFill;
        public GameObject overlay;
        public TMP_Text overlayText;
        public float Health { get; private set; }
        public float Elapsed { get; private set; }
        public int Kills { get; private set; }
        public int EnemyCount { get { return enemies.Count; } }
        [Header("Equipment")]
        public PlayerEquipment equipment = new PlayerEquipment();
        public EquipmentCatalog equipmentCatalog;
        public EquipmentLoot Loot { get; private set; }
        public bool EquipmentOpen { get; private set; }
        public bool Paused { get { return manuallyPaused || EquipmentOpen || DialogueOpen; } }
        [Header("Dialogue")]
        public DialogueScript testDialogue;
        public DialoguePanel Dialogue { get; private set; }
        public bool DialogueOpen { get { return Dialogue && Dialogue.IsOpen; } }
        public float EffectiveMoveSpeed { get { return equipment.MoveSpeed(playerSpeed); } }
        public float EffectiveAttackInterval { get { return equipment.AttackInterval(attackInterval); } }
        public float EffectiveDamage { get { return equipment.Damage(boltDamage); } }
        bool manuallyPaused;
        EquipmentPanel equipmentPanel;
        public int ShotsFired { get; private set; }
        public int AcidShotsFired { get; private set; }
        public int SpawnCount { get; private set; }
        public float ClosestSpawn { get; private set; }
        public int DamageEvents { get; private set; }

        sealed class Enemy
        {
            public SpriteRenderer view;
            public Vector2 position;
            public bool ranged;
            public float health, cooldown, age, flash;
        }
        sealed class Shot
        {
            public SpriteRenderer view;
            public Vector2 position, velocity;
            public bool hostile;
            public float lifetime, damage;
        }
        readonly List<Enemy> enemies = new List<Enemy>();
        readonly List<Shot> shots = new List<Shot>();
        Transform actors;
        float fireClock, spawnClock, invulnerability;
        Vector2 playerPosition;
        bool dead;
        void Start()
        {
            actors = new GameObject("Runtime Actors").transform;
            if (!equipmentCatalog) equipmentCatalog = Resources.Load<EquipmentCatalog>("EquipmentCatalog");
            if (!equipmentCatalog) Debug.LogError("EquipmentCatalog is missing. Run inventory asset setup.");
            Loot = gameObject.AddComponent<EquipmentLoot>();
            Loot.Initialize(this);
            equipmentPanel = gameObject.AddComponent<EquipmentPanel>();
            equipmentPanel.Initialize(this);
            if (!bossEncounter)
            {
                if (!testDialogue) testDialogue = Resources.Load<DialogueScript>("TestDialogue");
                Dialogue = gameObject.AddComponent<DialoguePanel>();
                Dialogue.Initialize(this);
            }
            if (bossEncounter) bossEncounter.Initialize(this);
            Restart();
        }
        public void Restart()
        {
            if (Dialogue) Dialogue.Close();
            foreach (var e in enemies) if (e.view) Destroy(e.view.gameObject);
            foreach (var s in shots) if (s.view) Destroy(s.view.gameObject);
            enemies.Clear(); shots.Clear();
            if (Loot) Loot.Clear();
            playerPosition = bossEncounter ? bossEncounter.config.playerSpawn : Vector2.zero;
            player.transform.position = playerPosition;
            player.color = Color.white;
            Health = maxHealth; Elapsed = 0; Kills = 0;
            ShotsFired = 0; AcidShotsFired = 0; SpawnCount = 0; DamageEvents = 0;
            ClosestSpawn = float.PositiveInfinity;
            fireClock = .3f; spawnClock = 1; invulnerability = 0;
            dead = false; manuallyPaused = false; EquipmentOpen = false;
            equipment.Reset();
            if (equipmentPanel) equipmentPanel.SetVisible(false);
            overlay.SetActive(false);
            if (city) city.UpdateNavigation(playerPosition);
            if (bossEncounter) bossEncounter.ResetEncounter();
            else for (int i = 0; i < 5; i++) SpawnEnemy(i == 4);
            UpdateHUD();
        }
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.R)) { Restart(); return; }
            if (DialogueOpen) { Dialogue.HandleInput(); return; }
            if (Input.GetKeyDown(KeyCode.F) && !bossEncounter && !dead && !EquipmentOpen)
            { StartDialogue(testDialogue); return; }
            if (Input.GetKeyDown(KeyCode.E) && !dead && !(bossEncounter && bossEncounter.Finished)) SetEquipmentOpen(!EquipmentOpen);
            if (Input.GetKeyDown(KeyCode.Escape) && !dead && !(bossEncounter && bossEncounter.Finished))
            {
                if (EquipmentOpen) { SetEquipmentOpen(false); return; }
                manuallyPaused = !manuallyPaused;
                overlay.SetActive(manuallyPaused);
                overlayText.text = "PAUSED\n<size=22>ESC to continue   /   R to restart</size>";
            }
            if (dead || Paused) return;
            Tick(Mathf.Min(Time.deltaTime, .05f), new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")));
        }
        // One simulation step also permits deterministic editor smoke checks.
        public void Tick(float dt, Vector2 input)
        {
            if (dead || Paused || dt <= 0 || (bossEncounter && bossEncounter.Finished)) return;
            if (!bossEncounter || bossEncounter.CombatActive) Elapsed += dt;
            invulnerability = Mathf.Max(0, invulnerability - dt);
            MovePlayer(input, dt);
            if (bossEncounter)
            {
                bossEncounter.Tick(dt);
                if (dead || bossEncounter.Finished) { UpdateHUD(); return; }
            }
            if (city) city.UpdateNavigation(playerPosition);
            spawnClock -= dt;
            if (!bossEncounter && spawnClock <= 0)
            {
                if (enemies.Count < enemyLimit) SpawnEnemy(Random.value < .28f);
                spawnClock = Mathf.Max(.42f, spawnInterval - Elapsed * .003f);
            }
            fireClock -= dt;
            if (fireClock <= 0 && (!bossEncounter || bossEncounter.CombatActive))
            {
                Enemy nearest = null; float distance = attackRange * attackRange;
                foreach (var e in enemies)
                {
                    float d = (e.position - playerPosition).sqrMagnitude;
                    if (d < distance && (!city || city.LineClear(playerPosition, e.position,.2f))) { nearest = e; distance = d; }
                }
                if (nearest != null)
                {
                    Fire(playerPosition + Vector2.up * .4f, nearest.position + Vector2.up * .4f, false);
                    fireClock = EffectiveAttackInterval;
                }
                else if (bossEncounter && Vector2.Distance(playerPosition, bossEncounter.BossPosition) <= attackRange)
                {
                    Fire(playerPosition + Vector2.up * .4f, bossEncounter.BossPosition, false);
                    fireClock = EffectiveAttackInterval;
                }
            }
            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                e.age += dt; e.cooldown -= dt; e.flash -= dt;
                Vector2 delta = playerPosition - e.position;
                float distance = delta.magnitude;
                Vector2 direction = distance > .001f ? delta / distance : Vector2.zero;
                Vector2 movement = city ? city.Chase(e.position,playerPosition) : direction;
                if (e.ranged)
                {
                    bool sight = !city || city.LineClear(e.position,playerPosition,.2f);
                    if (sight && distance < 4) movement = -direction;
                    else if (sight && distance < 6) movement = Vector2.zero;
                    if (sight && distance < 9 && e.cooldown <= 0 && e.age > .7f)
                    {
                        Fire(e.position + Vector2.up * .4f, playerPosition + Vector2.up * .4f, true);
                        e.cooldown = 2.4f;
                    }
                }
                // Local separation prevents the horde collapsing into one sprite.
                Vector2 separation = Vector2.zero;
                foreach (var other in enemies)
                {
                    if (other == e) continue;
                    Vector2 gap = e.position - other.position;
                    float d = gap.sqrMagnitude;
                    if (d > .0001f && d < .64f) separation += gap.normalized * (1 - Mathf.Sqrt(d) / .8f);
                }
                Vector2 step = Vector2.ClampMagnitude(movement + separation, 1) * (e.ranged ? 1.25f : 1.75f) * dt;
                e.position = city ? city.Move(e.position,step) : ClampToArena(e.position+step);
                e.view.transform.position = e.position;
                e.view.flipX = delta.x < 0;
                e.view.sortingOrder = 100 - Mathf.RoundToInt(e.position.y * 10);
                e.view.color = e.age < .7f ? new Color(.65f, 1, .65f, .45f + e.age * .7f) : e.flash > 0 ? new Color(1,.45f,.3f) : Color.white;
                e.view.transform.localScale = Vector3.one * (1 + Mathf.Sin(e.age * 9) * .018f);
                if (!e.ranged && distance < .72f && e.cooldown <= 0 && e.age > .7f)
                {
                    DamagePlayer(12); e.cooldown = 1;
                }
            }
            for (int i = shots.Count - 1; i >= 0; i--)
            {
                Shot shot = shots[i];
                Vector2 previous = shot.position;
                shot.position += shot.velocity * dt;
                shot.lifetime -= dt;
                bool hit = city && !city.LineClear(previous,shot.position,.08f);
                if (!hit && shot.hostile)
                {
                    if (SegmentDistance(playerPosition + Vector2.up * .4f, previous, shot.position) < .42f)
                    { DamagePlayer(16); hit = true; }
                }
                else if (!hit)
                {
                    if (bossEncounter && bossEncounter.CombatActive && SegmentDistance(bossEncounter.BossPosition, previous, shot.position) <= bossEncounter.config.hitRadius)
                    { bossEncounter.ReceiveDamage(shot.damage); hit = true; }
                    for (int n = enemies.Count - 1; n >= 0; n--)
                    {
                        var e = enemies[n];
                        if (e.age < .7f || SegmentDistance(e.position + Vector2.up * .4f, previous, shot.position) > .46f) continue;
                        e.health -= shot.damage; e.flash = .12f;
                        if (e.health <= 0) { Loot.TryDrop(e.position); Destroy(e.view.gameObject); enemies.RemoveAt(n); Kills++; }
                        hit = true; break;
                    }
                }
                shot.view.transform.position = shot.position;
                if (hit || shot.lifetime <= 0 || Mathf.Abs(shot.position.x) > arenaHalfSize.x + 1 || Mathf.Abs(shot.position.y) > arenaHalfSize.y + 1)
                { Destroy(shot.view.gameObject); shots.RemoveAt(i); }
            }
            if (bossEncounter) bossEncounter.ResolveState();
            if (!dead && Loot) Loot.Tick(dt, playerPosition);
            UpdateHUD();
        }
        public Vector2 ClampToArena(Vector2 p)
        {
            return new Vector2(Mathf.Clamp(p.x, -arenaHalfSize.x + .55f, arenaHalfSize.x - .55f),
                Mathf.Clamp(p.y, -arenaHalfSize.y + .55f, arenaHalfSize.y - .55f));
        }
        public void MovePlayer(Vector2 input, float dt)
        {
            Vector2 step = Vector2.ClampMagnitude(input, 1) * EffectiveMoveSpeed * dt;
            playerPosition = bossEncounter ? bossEncounter.MovePlayer(playerPosition, step) : city ? city.Move(playerPosition,step) : ClampToArena(playerPosition+step);
            player.transform.position = playerPosition;
            player.sortingOrder = 100 - Mathf.RoundToInt(playerPosition.y * 10);
            if (Mathf.Abs(input.x) > .1f) player.flipX = input.x < 0;
            player.transform.localScale = new Vector3(1, 1 + (input.sqrMagnitude > .01f ? Mathf.Sin(Elapsed * 14) * .018f : 0), 1);
            player.color = invulnerability > 0 && Mathf.FloorToInt(invulnerability * 16) % 2 == 0 ? new Color(1,.45f,.4f) : Color.white;
        }
        public bool TrySpawnPosition(out Vector2 point)
        {
            for (int attempt = 0; attempt < 64; attempt++)
            {
                point = new Vector2(Random.Range(-arenaHalfSize.x + .8f, arenaHalfSize.x - .8f),
                    Random.Range(-arenaHalfSize.y + .8f, arenaHalfSize.y - .8f));
                if (Vector2.Distance(point, playerPosition) >= spawnSafeDistance && (!city || (city.IsFree(point,.6f) && city.Reachable(point)))) return true;
            }
            point = Vector2.zero; return false;
        }
        public void SetEquipmentOpen(bool open)
        {
            if (dead || (bossEncounter && bossEncounter.Finished) || (open && DialogueOpen)) return;
            EquipmentOpen = open;
            overlay.SetActive(!open && manuallyPaused);
            if (equipmentPanel) equipmentPanel.SetVisible(open);
        }

        public bool StartDialogue(DialogueScript script)
        {
            if (bossEncounter || dead || EquipmentOpen || DialogueOpen || !Dialogue) return false;
            if (!Dialogue.Begin(script)) return false;
            overlay.SetActive(false);
            return true;
        }
        public void CloseDialogue()
        {
            if (Dialogue) Dialogue.Close();
            if (!dead) overlay.SetActive(manuallyPaused);
        }

        public bool EquipItem(int index)
        {
            if (dead || !EquipmentOpen) return false;
            float previousInterval = EffectiveAttackInterval;
            if (!equipment.Equip(index)) return false;
            // Preserve cooldown progress so repeatedly swapping cannot grant free shots.
            fireClock *= EffectiveAttackInterval / previousInterval;
            if (equipmentPanel) equipmentPanel.Refresh();
            return true;
        }
        public bool UnequipItem(EquipmentSlot slot)
        {
            if (dead || !EquipmentOpen) return false;
            float previousInterval = EffectiveAttackInterval;
            if (!equipment.Unequip(slot)) return false;
            fireClock *= EffectiveAttackInterval / previousInterval;
            return true;
        }
        public bool DeleteInventoryItem(int index)
        { return !dead && EquipmentOpen && equipment.Delete(index); }
        public float GetEnemyHealth(bool ranged)
        {
            return Mathf.Max(1, ranged ? rangedBaseHealth : meleeBaseHealth);
        }
        public void SpawnEnemy(bool ranged)
        {
            if (bossEncounter) return;
            Vector2 position;
            if (enemies.Count >= enemyLimit || !TrySpawnPosition(out position)) return;
            var view = Instantiate(ranged ? rangedPrefab : meleePrefab, actors);
            var outlines = GetComponent<PlagueSpriteOutlines>();
            if (outlines) outlines.Attach(view, ranged ? 2 : 1);
            view.name = city ? (ranged ? "Hijacked Security Robot" : "Augmented Enforcer") : (ranged ? "Acid Spitter" : "Plague Walker");
            view.transform.position = position;
            enemies.Add(new Enemy { view = view, position = position, ranged = ranged, health = GetEnemyHealth(ranged), cooldown = 1.4f });
            ClosestSpawn = Mathf.Min(ClosestSpawn, Vector2.Distance(position, playerPosition));
            SpawnCount++;
        }
        void Fire(Vector2 origin, Vector2 target, bool hostile)
        {
            Vector2 direction = (target - origin).normalized;
            var view = Instantiate(hostile ? acidPrefab : boltPrefab, actors);
            view.transform.position = origin;
            view.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            view.sortingOrder = 300;
            shots.Add(new Shot { view = view, position = origin, velocity = direction * (hostile ? 4 : 13), hostile = hostile, lifetime = hostile ? 4 : 1.3f, damage = hostile ? 16 : EffectiveDamage });
            if (hostile) AcidShotsFired++; else ShotsFired++;
        }
        public void DamagePlayer(float damage)
        {
            if (dead || invulnerability > 0 || (bossEncounter && (Paused || !bossEncounter.CombatActive)) || damage <= 0) return;
            Health = Mathf.Max(0, Health - damage);
            DamageEvents++;
            invulnerability = .65f;
            if (Health <= 0)
            {
                dead = true; EquipmentOpen = false;
                if (Dialogue) Dialogue.Close();
                if (equipmentPanel) equipmentPanel.SetVisible(false);
                overlay.SetActive(true);
                overlayText.text = (city ? "CONNECTION LOST" : "THE TOWN HAS FALLEN") + "\n<size=24>Survived " + Mathf.FloorToInt(Elapsed) + "s  /  " + Kills + " neutralized\nPress R to try again</size>";
            }
        }
        public bool Heal(float amount)
        {
            if (dead || Paused || amount <= 0 || Health >= maxHealth) return false;
            Health = Mathf.Min(maxHealth, Health + amount);
            return true;
        }
        public void ClearProjectiles()
        {
            foreach (var shot in shots) if (shot.view) { shot.view.gameObject.SetActive(false); Destroy(shot.view.gameObject); }
            shots.Clear();
        }
        public void ShowEncounterResult(string text)
        {
            EquipmentOpen = false; manuallyPaused = false;
            if (equipmentPanel) equipmentPanel.SetVisible(false);
            overlay.SetActive(true); overlayText.text = text;
        }
        public static float SegmentDistance(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            return Vector2.Distance(point, a + ab * Mathf.Clamp01(Vector2.Dot(point - a, ab) / Mathf.Max(.00001f, ab.sqrMagnitude)));
        }
        void UpdateHUD()
        {
            status.text = (city ? "INTEGRITY   " : "VITALITY   ") + Mathf.CeilToInt(Health) + " / " + maxHealth + (city ? "     |     NEUTRALIZED   " : "     |     SLAIN   ") + Kills;
            timer.text = Mathf.FloorToInt(Elapsed / 60).ToString("00") + ":" + Mathf.FloorToInt(Elapsed % 60).ToString("00");
            healthFill.fillAmount = Health / maxHealth;
            message.text = Loot && Loot.NearbyBagFull ? "BACKPACK FULL  24 / 24    E Manage inventory    Nearby loot stays on the ground"
                : "WASD Move   TAB Map   E Bag " + equipment.Count + "/24   F Dialogue   ESC Pause   R Restart";
            if (bossEncounter) bossEncounter.UpdateHUD();
        }
    }
}
