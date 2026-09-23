using System.Collections.Generic;
using Echo.LevelToolkit.Combat;
using Echo.LevelToolkit.Foundation;
using UnityEngine;

namespace Echo.LevelToolkit.Preview
{
    // This scene needs only the toolkit runtime. It never loads Native Quest, Narrative or HUD.
    public sealed class CombatPreviewHost : MonoBehaviour, ILevelRunContext
    {
        public CombatTemplate standardPlayer, chaseEnemy, orbitFanEnemy, bossTemplate;
        public RunId RunId { get; private set; }
        public bool IsRunning { get; private set; }
        public Transform Player => player ? player.transform : null;
        CombatWorld world;
        CombatPlayer player;
        CombatWeapon weapon;
        CombatBoss boss;
        CombatProjectile projectileTemplate;
        readonly List<Object> generatedArt = new List<Object>();
        int realKills;
        bool bossActivated, bossDefeated, startupFailed;
        string status = "WASD move · Space toggle fire · E core · R restart";
        void Start()
        {
            if (!TemplatesReady()) { Fail("CombatPreview: assign all four CombatTemplate assets of the correct kind."); return; }
            RunId = RunId.New();
            var content = new ContentIdentity("echo", "combat-preview", "arena");
            var scope = RuntimeScope.New(RunId, content);
            world = new GameObject("Combat world").AddComponent<CombatWorld>();
            world.transform.SetParent(transform, false);
            var sprite = MakeSprite();
            projectileTemplate = NewProjectileTemplate(sprite);
            player = NewPlayer(sprite);
            if (!player) { Fail("CombatPreview: player creation failed."); return; }
            IsRunning = true;
            var context = new LevelInstanceContext(this, scope);
            if (!world.Initialize(context, player)) { Fail("CombatPreview: world initialization failed."); return; }
            weapon = player.GetComponent<CombatWeapon>();
            weapon.projectilePrefab = projectileTemplate;
            if (!weapon.Initialize(world)) { Fail("CombatPreview: weapon initialization failed."); return; }
            world.EnemyDied += OnEnemyDied;
            if (!NewEnemy(sprite, chaseEnemy, new Vector2(4, 0), false) ||
                !NewEnemy(sprite, orbitFanEnemy, new Vector2(-4, 1), true))
            { Fail("CombatPreview: enemy initialization failed."); return; }
            boss = NewBoss(sprite);
            if (!boss) { Fail("CombatPreview: boss initialization failed."); return; }
            boss.Defeated += OnBossDefeated;
            MakeArena(sprite);
            if (!Camera.main) NewCamera();
        }
        bool TemplatesReady() => standardPlayer && standardPlayer.kind == CombatTemplate.Kind.StandardPlayer &&
            chaseEnemy && chaseEnemy.kind == CombatTemplate.Kind.ChaseEnemy &&
            orbitFanEnemy && orbitFanEnemy.kind == CombatTemplate.Kind.OrbitFanEnemy &&
            bossTemplate && bossTemplate.kind == CombatTemplate.Kind.Boss;
        void Fail(string message) { startupFailed = true; IsRunning = false; status = message; Debug.LogError(message, this); }
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.R)) { Restart(); return; }
            if (!IsRunning || !player) return;
            player.MoveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
            if (Input.GetKeyDown(KeyCode.Space)) weapon.ToggleFire();
            if (Input.GetKeyDown(KeyCode.E) && boss && boss.InteractCore(player)) status = "Boss core disconnected. Press R to restart.";
            if (!bossActivated && realKills >= 2 && boss)
            {
                var result = boss.TryActivate();
                if (result == CombatBoss.ActivationResult.Started) { bossActivated = true; status = "Boss active: break shell, then approach and press E."; }
                else Fail("CombatPreview: boss activation failed: " + result);
            }
        }
        public void ReportPlayerDeath()
        {
            if (!IsRunning) return;
            IsRunning = false; status = "Signal lost. Press R to restart.";
        }
        void OnEnemyDied(CombatEnemy enemy) { realKills++; status = "Real kills: " + realKills + "/2"; }
        void OnBossDefeated(CombatBoss target) { bossDefeated = true; status = "Boss core disconnected. Press R to restart."; }
        void Restart()
        {
            IsRunning = false;
            if (world) world.EnemyDied -= OnEnemyDied;
            if (boss) boss.Defeated -= OnBossDefeated;
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
            foreach (var art in generatedArt) if (art) Destroy(art);
            generatedArt.Clear();
            world = null; player = null; weapon = null; boss = null; projectileTemplate = null;
            realKills = 0; bossActivated = false; bossDefeated = false; startupFailed = false;
            status = "WASD move · Space toggle fire · E core · R restart";
            Start();
        }
        void OnDestroy()
        {
            if (world) world.EnemyDied -= OnEnemyDied;
            if (boss) boss.Defeated -= OnBossDefeated;
            foreach (var art in generatedArt) if (art) Destroy(art);
        }
        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(12, 12, 550, 110), GUI.skin.box);
            GUILayout.Label(status);
            if (player) GUILayout.Label("HP " + player.Health.ToString("0") + " / " + player.maxHealth +
                "  Kills " + realKills + "  Boss " + (bossDefeated ? "defeated" : bossActivated ? boss.Stage.ToString() : "dormant"));
            if (startupFailed) GUILayout.Label("Preview setup failed; see Console.");
            GUILayout.EndArea();
        }
        Sprite MakeSprite()
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point; texture.SetPixel(0, 0, Color.white); texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1);
            generatedArt.Add(texture); generatedArt.Add(sprite);
            return sprite;
        }
        CombatProjectile NewProjectileTemplate(Sprite sprite)
        {
            var go = new GameObject("Projectile template"); go.transform.SetParent(transform, false);
            var view = go.AddComponent<SpriteRenderer>(); view.sprite = sprite;
            go.transform.localScale = new Vector3(.23f, .1f, 1);
            var shot = go.AddComponent<CombatProjectile>();
            shot.speed = standardPlayer.projectileSpeed; shot.lifetime = standardPlayer.projectileLifetime;
            shot.radius = standardPlayer.projectileRadius;
            go.SetActive(false);
            return shot;
        }
        CombatPlayer NewPlayer(Sprite sprite)
        {
            var go = new GameObject("Standard player"); go.transform.position = new Vector3(0, -3, 0);
            go.transform.SetParent(world.transform, true);
            var view = go.AddComponent<SpriteRenderer>(); view.sprite = sprite; view.color = new Color(.25f, .9f, 1);
            go.transform.localScale = Vector3.one * standardPlayer.colliderRadius * 2;
            var body = go.AddComponent<Rigidbody2D>(); body.gravityScale = 0; body.freezeRotation = true;
            go.AddComponent<CircleCollider2D>().radius = .5f;
            var result = go.AddComponent<CombatPlayer>(); result.view = view;
            result.speed = standardPlayer.speed; result.maxHealth = standardPlayer.health;
            result.hurtCooldown = standardPlayer.hurtCooldown;
            var combat = go.AddComponent<CombatWeapon>(); combat.range = standardPlayer.range;
            combat.shotInterval = standardPlayer.shotInterval; combat.damage = standardPlayer.damage;
            return result;
        }
        bool NewEnemy(Sprite sprite, CombatTemplate template, Vector2 position, bool fan)
        {
            var go = new GameObject(fan ? "Arc Sentry" : "Security Drone"); go.transform.position = position;
            go.transform.SetParent(world.transform, true);
            var view = go.AddComponent<SpriteRenderer>(); view.sprite = sprite;
            view.color = fan ? new Color(1, .25f, .75f) : new Color(1, .5f, .25f);
            go.transform.localScale = Vector3.one * template.colliderRadius * 2;
            var body = go.AddComponent<Rigidbody2D>(); body.gravityScale = 0; body.freezeRotation = true;
            go.AddComponent<CircleCollider2D>().radius = .5f;
            var enemy = go.AddComponent<CombatEnemy>(); enemy.view = view;
            enemy.speed = template.speed; enemy.health = template.health; enemy.contactDamage = template.contactDamage;
            enemy.detectionRange = template.detectionRange; enemy.preferredRange = template.preferredRange;
            enemy.movementStyle = fan ? CombatEnemy.MovementStyle.Orbit : CombatEnemy.MovementStyle.Chase;
            if (!enemy.Initialize(world)) return false;
            if (fan)
            {
                var attack = go.AddComponent<CombatFanAttack>(); attack.projectilePrefab = projectileTemplate;
                attack.range = template.range; attack.warmupSeconds = template.fanWarmup; attack.cooldownSeconds = template.fanCooldown;
                attack.shotSpeed = template.fanShotSpeed; attack.shotDamage = template.fanShotDamage;
                attack.spreadDegrees = template.fanSpreadDegrees;
                if (!attack.Initialize()) return false;
            }
            return true;
        }
        CombatBoss NewBoss(Sprite sprite)
        {
            var go = new GameObject("Development Boss"); go.transform.position = new Vector3(0, 4, 0);
            go.transform.SetParent(world.transform, true);
            var collider = go.AddComponent<CircleCollider2D>(); collider.radius = bossTemplate.colliderRadius;
            var result = go.AddComponent<CombatBoss>();
            var baseObject = new GameObject("Base"); baseObject.transform.SetParent(go.transform, false);
            result.baseView = baseObject.AddComponent<SpriteRenderer>(); result.baseView.sprite = sprite;
            result.baseView.color = new Color(.6f, .65f, .75f); result.baseView.sortingOrder = 4;
            baseObject.transform.localScale = new Vector3(2, 1.4f, 1);
            var pivot = new GameObject("Turret"); pivot.transform.SetParent(go.transform, false);
            result.turretPivot = pivot.transform;
            result.turretView = pivot.AddComponent<SpriteRenderer>(); result.turretView.sprite = sprite;
            result.turretView.color = new Color(.9f, .7f, .3f); result.turretView.sortingOrder = 5;
            pivot.transform.localScale = new Vector3(.8f, .3f, 1);
            var core = new GameObject("Core"); core.transform.SetParent(go.transform, false);
            result.coreView = core.AddComponent<SpriteRenderer>(); result.coreView.sprite = sprite;
            result.coreView.sortingOrder = 6; core.transform.localScale = Vector3.one * .4f;
            result.hostileProjectilePrefab = projectileTemplate;
            result.maxArmor = bossTemplate.health; result.armorDamageScale = bossTemplate.armorDamageScale;
            result.trackingSeconds = bossTemplate.trackingSeconds; result.lockSeconds = bossTemplate.lockSeconds;
            result.shotInterval = bossTemplate.bossShotInterval; result.burstCount = bossTemplate.burstCount;
            result.shotSpeed = bossTemplate.bossShotSpeed; result.shotDamage = bossTemplate.bossShotDamage;
            result.bombWarning = bossTemplate.bombWarning; result.bombInterval = bossTemplate.bombInterval;
            result.bombRadius = bossTemplate.bombRadius; result.bombDamage = bossTemplate.bombDamage;
            result.coreWindowSeconds = bossTemplate.coreWindowSeconds; result.retryWindowDelay = bossTemplate.retryWindowDelay;
            result.coreInteractionRadius = bossTemplate.coreInteractionRadius; result.muzzleOffset = bossTemplate.muzzleOffset;
            return result.Initialize(world) ? result : null;
        }
        void MakeArena(Sprite sprite)
        {
            var floor = new GameObject("Floor"); floor.transform.SetParent(transform, false);
            var view = floor.AddComponent<SpriteRenderer>(); view.sprite = sprite;
            view.color = new Color(.06f, .09f, .15f); view.sortingOrder = -100;
            floor.transform.localScale = new Vector3(18, 14, 1);
        }
        void NewCamera()
        {
            var go = new GameObject("Preview camera"); go.tag = "MainCamera"; go.transform.position = new Vector3(0, 0, -10);
            go.transform.SetParent(transform, true); var camera = go.AddComponent<Camera>(); camera.orthographic = true;
            camera.orthographicSize = 7.5f; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.03f, .05f, .1f);
        }
    }
}
