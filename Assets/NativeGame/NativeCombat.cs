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
        public void ToggleFire() { if (level.Running) AutoFire = !AutoFire; }
        void Update()
        {
            if (!level || !level.Running || !AutoFire || Time.time < nextShot) return;
            NativeEnemy target = null; float nearest = range * range;
            foreach (var enemy in NativeEnemy.Active)
            {
                if (!enemy || !enemy.Alive) continue;
                float distance = ((Vector2)(enemy.transform.position - actor.transform.position)).sqrMagnitude;
                if (distance < nearest && !NativeObstacle.Blocked(actor.transform.position, enemy.transform.position)) { target = enemy; nearest = distance; }
            }
            if (!target) return;
            Vector2 direction = ((Vector2)(target.transform.position - actor.transform.position)).normalized;
            var shot = Instantiate(projectilePrefab, actor.transform.position + (Vector3)(direction * .48f), Quaternion.identity);
            shot.Launch(direction, damage, this);
            actor.view.flipX = direction.x < 0; nextShot = Time.time + shotInterval;
        }
        public void HitEnemy(NativeEnemy target, float amount)
        {
            if (!level.Running || !target || !target.Alive) return;
            target.ReceiveDamage(amount);
            if (!target.Alive) Kills++;
        }
        public void HitPlayer(float amount)
        { if (level.Running) actor.ReceiveDamage(amount); }
    }
}
