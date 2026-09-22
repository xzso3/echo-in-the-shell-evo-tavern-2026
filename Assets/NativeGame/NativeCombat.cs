using UnityEngine;
namespace Echo.NativeGame
{
    public sealed class NativeCombat : MonoBehaviour
    {
        public NativeRunController level;
        public NativePlayer actor;
        public NativeProjectile projectilePrefab;
        public float range = 7, shotInterval = .38f, damage = 24;
        public bool AutoFire { get; private set; } = true;
        public int Kills { get; private set; }
        float nextShot;
        public void ToggleFire() { if (level && level.Running) AutoFire = !AutoFire; }
        void Update()
        {
            if (!level || !level.Running || !actor || !actor.Alive || !AutoFire || Time.time < nextShot || !projectilePrefab) return;
            bool found = false; Vector2 aimPoint = default; float nearest = range * range;
            foreach (var enemy in NativeEnemy.Active)
            {
                if (!enemy || !enemy.Alive || enemy.run != level) continue;
                float distance = ((Vector2)(enemy.transform.position - actor.transform.position)).sqrMagnitude;
                if (distance < nearest && !NativeObstacle.Blocked(actor.transform.position, enemy.transform.position))
                { found = true; aimPoint = enemy.transform.position; nearest = distance; }
            }
            foreach (var boss in NativeBossController.Active)
            {
                if (!boss || boss.level != level || !boss.CanBeTargeted) continue;
                float distance = (boss.AimPoint - (Vector2)actor.transform.position).sqrMagnitude;
                if (distance < nearest && !NativeObstacle.Blocked(actor.transform.position, boss.AimPoint))
                { found = true; aimPoint = boss.AimPoint; nearest = distance; }
            }
            if (!found) return;
            Vector2 direction = (aimPoint - (Vector2)actor.transform.position).normalized;
            Vector2 origin = (Vector2)actor.transform.position + direction * .48f;
            if (NativeObstacle.Blocked(actor.transform.position, origin)) return;
            var shot = Instantiate(projectilePrefab, origin, Quaternion.identity);
            shot.Launch(direction, damage, this);
            if (actor.view) actor.view.flipX = direction.x < 0;
            nextShot = Time.time + shotInterval;
        }
        public void HitEnemy(NativeEnemy target, float amount)
        {
            if (!level || !level.Running || !target || !target.Alive || target.run != level) return;
            target.ReceiveDamage(amount);
            if (!target.Alive) Kills++;
        }
        public void HitBoss(NativeBossController target, float amount)
        {
            if (level && level.Running && target && target.level == level && target.CanBeTargeted) target.ReceiveDamage(amount);
            // Decisive E action and encounter completion are owned by Boss/scene ECA, not the kill counter.
        }
        public void HitPlayer(float amount)
        { if (level && level.Running && actor && actor.Alive) actor.ReceiveDamage(amount); }
    }
}
