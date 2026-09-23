using UnityEngine;

namespace Echo.LevelToolkit.Combat
{
    [RequireComponent(typeof(CombatPlayer))]
    public sealed class CombatWeapon : MonoBehaviour
    {
        public CombatProjectile projectilePrefab;
        public float range = 7, shotInterval = .38f, damage = 24;
        public bool AutoFire { get; private set; } = true;
        public int Kills { get; private set; }
        public CombatWorld World { get; private set; }
        CombatPlayer actor;
        float nextShot;
        public bool Initialize(CombatWorld world)
        {
            if (World || !world || !world.IsRunning || !projectilePrefab || !CombatPlayer.Valid(range) ||
                !CombatPlayer.Valid(shotInterval) || !CombatPlayer.Valid(damage)) return false;
            actor = GetComponent<CombatPlayer>();
            if (actor != world.Player) return false;
            World = world;
            return true;
        }
        internal void RecordKill() { Kills++; }
        public void ToggleFire() { if (World && World.IsRunning) AutoFire = !AutoFire; }
        void Update()
        {
            if (!World || !World.IsRunning || !AutoFire || Time.time < nextShot) return;
            bool found = false; Vector2 aimPoint = default; float nearest = range * range;
            foreach (var enemy in World.Enemies)
            {
                if (!enemy || !enemy.Alive) continue;
                float distance = ((Vector2)(enemy.transform.position - actor.transform.position)).sqrMagnitude;
                if (distance < nearest && !CombatSight.Blocked(actor.transform.position, enemy.transform.position))
                { found = true; aimPoint = enemy.transform.position; nearest = distance; }
            }
            foreach (var boss in World.Bosses)
            {
                if (!boss || !boss.CanBeTargeted) continue;
                float distance = (boss.AimPoint - (Vector2)actor.transform.position).sqrMagnitude;
                if (distance < nearest && !CombatSight.Blocked(actor.transform.position, boss.AimPoint))
                { found = true; aimPoint = boss.AimPoint; nearest = distance; }
            }
            if (!found) return;
            Vector2 direction = (aimPoint - (Vector2)actor.transform.position).normalized;
            Vector2 origin = (Vector2)actor.transform.position + direction * .48f;
            if (CombatSight.Blocked(actor.transform.position, origin)) return;
            var shot = Instantiate(projectilePrefab, origin, Quaternion.identity, World.transform);
            shot.gameObject.SetActive(true);
            if (!shot.Launch(World, actor, direction, damage, false)) { Destroy(shot.gameObject); return; }
            if (actor.view) actor.view.flipX = direction.x < 0;
            nextShot = Time.time + shotInterval;
        }
    }
}
