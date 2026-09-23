using Echo.LevelToolkit.Combat;
using Echo.LevelToolkit.Foundation;
using UnityEngine;

namespace Echo.LevelToolkit.Preview
{
    // Minimal first slice: player movement, automatic Pulse, one Chase enemy, true death.
    public sealed class CombatSliceHost : MonoBehaviour, ILevelRunContext
    {
        public CombatTemplate standardPlayer, chaseEnemy;
        public RunId RunId { get; private set; }
        public bool IsRunning { get; private set; }
        public Transform Player => player ? player.transform : null;
        CombatWorld world;
        CombatPlayer player;
        CombatWeapon weapon;
        Sprite sprite;
        Texture2D texture;
        int realKills;
        string status = "WASD move · Space toggle fire · R restart";
        void Start()
        {
            if (!standardPlayer || standardPlayer.kind != CombatTemplate.Kind.StandardPlayer ||
                !chaseEnemy || chaseEnemy.kind != CombatTemplate.Kind.ChaseEnemy)
            { status = "Missing StandardPlayer or ChaseEnemy template"; Debug.LogError(status, this); return; }
            RunId = RunId.New();
            world = new GameObject("Combat world").AddComponent<CombatWorld>();
            world.transform.SetParent(transform, false);
            texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point; texture.SetPixel(0, 0, Color.white); texture.Apply();
            sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1);
            var shotObject = new GameObject("Pulse template"); shotObject.transform.SetParent(transform, false);
            shotObject.AddComponent<SpriteRenderer>().sprite = sprite;
            shotObject.transform.localScale = new Vector3(.23f, .1f, 1);
            var shot = shotObject.AddComponent<CombatProjectile>();
            shot.speed = standardPlayer.projectileSpeed; shot.lifetime = standardPlayer.projectileLifetime;
            shot.radius = standardPlayer.projectileRadius; shotObject.SetActive(false);
            var playerObject = new GameObject("Standard player"); playerObject.transform.position = new Vector3(0, -2, 0);
            playerObject.transform.SetParent(world.transform, true);
            var playerView = playerObject.AddComponent<SpriteRenderer>(); playerView.sprite = sprite;
            playerView.color = new Color(.25f, .9f, 1);
            playerObject.transform.localScale = Vector3.one * standardPlayer.colliderRadius * 2;
            var playerBody = playerObject.AddComponent<Rigidbody2D>(); playerBody.gravityScale = 0; playerBody.freezeRotation = true;
            playerObject.AddComponent<CircleCollider2D>().radius = .5f;
            player = playerObject.AddComponent<CombatPlayer>(); player.view = playerView;
            player.speed = standardPlayer.speed; player.maxHealth = standardPlayer.health;
            player.hurtCooldown = standardPlayer.hurtCooldown;
            weapon = playerObject.AddComponent<CombatWeapon>(); weapon.projectilePrefab = shot;
            weapon.range = standardPlayer.range; weapon.shotInterval = standardPlayer.shotInterval;
            weapon.damage = standardPlayer.damage;
            IsRunning = true;
            var scope = RuntimeScope.New(RunId, new ContentIdentity("echo", "combat-preview", "slice"));
            if (!world.Initialize(new LevelInstanceContext(this, scope), player) || !weapon.Initialize(world))
            { Fail("CombatPreviewSlice: player/weapon initialization failed"); return; }
            world.EnemyDied += OnEnemyDied;
            var enemyObject = new GameObject("Security Drone"); enemyObject.transform.position = new Vector3(3, 1, 0);
            enemyObject.transform.SetParent(world.transform, true);
            var enemyView = enemyObject.AddComponent<SpriteRenderer>(); enemyView.sprite = sprite;
            enemyView.color = new Color(1, .5f, .25f);
            enemyObject.transform.localScale = Vector3.one * chaseEnemy.colliderRadius * 2;
            var enemyBody = enemyObject.AddComponent<Rigidbody2D>(); enemyBody.gravityScale = 0; enemyBody.freezeRotation = true;
            enemyObject.AddComponent<CircleCollider2D>().radius = .5f;
            var enemy = enemyObject.AddComponent<CombatEnemy>(); enemy.view = enemyView;
            enemy.speed = chaseEnemy.speed; enemy.health = chaseEnemy.health;
            enemy.contactDamage = chaseEnemy.contactDamage; enemy.detectionRange = chaseEnemy.detectionRange;
            if (!enemy.Initialize(world)) { Fail("CombatPreviewSlice: enemy initialization failed"); return; }
            var floor = new GameObject("Floor"); floor.transform.SetParent(transform, false);
            var floorView = floor.AddComponent<SpriteRenderer>(); floorView.sprite = sprite;
            floorView.color = new Color(.06f, .09f, .15f); floorView.sortingOrder = -100;
            floor.transform.localScale = new Vector3(16, 12, 1);
            if (!Camera.main)
            {
                var cameraObject = new GameObject("Preview camera"); cameraObject.tag = "MainCamera";
                cameraObject.transform.position = new Vector3(0, 0, -10);
                cameraObject.transform.SetParent(transform, true);
                var camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true;
                camera.orthographicSize = 6.5f; camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.03f, .05f, .1f);
            }
        }
        void Fail(string message) { IsRunning = false; status = message; Debug.LogError(message, this); }
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.R)) { Restart(); return; }
            if (!IsRunning) return;
            player.MoveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
            if (Input.GetKeyDown(KeyCode.Space)) weapon.ToggleFire();
        }
        public void ReportPlayerDeath() { IsRunning = false; status = "Player died. Press R to restart."; }
        void OnEnemyDied(CombatEnemy enemy) { realKills++; status = "Real enemy death: " + realKills + ". Press R to restart."; }
        void Restart()
        {
            IsRunning = false;
            if (world) world.EnemyDied -= OnEnemyDied;
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                child.SetActive(false); Destroy(child);
            }
            if (sprite) Destroy(sprite); if (texture) Destroy(texture);
            world = null; player = null; weapon = null; realKills = 0;
            status = "WASD move · Space toggle fire · R restart";
            Start();
        }
        void OnDestroy()
        {
            if (world) world.EnemyDied -= OnEnemyDied;
            if (sprite) Destroy(sprite); if (texture) Destroy(texture);
        }
        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(12, 12, 510, 75), GUI.skin.box);
            GUILayout.Label(status);
            if (player) GUILayout.Label("HP " + player.Health.ToString("0") + "  Kills " + realKills);
            GUILayout.EndArea();
        }
    }
}
