using UnityEngine;

namespace Echo.NativeGame
{
    public sealed class NativeProjectile : MonoBehaviour
    {
        public float speed = 18, lifetime = 1.6f, radius = .09f;
        Vector2 direction;
        float damage;
        NativeCombat combat;
        NativeBossController bossOwner;
        bool hostile;
        public void Launch(Vector2 value, float amount, NativeCombat owner)
        {
            direction = value.normalized; damage = amount; combat = owner;
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(value.y, value.x) * Mathf.Rad2Deg);
            Destroy(gameObject, lifetime);
        }
        public void LaunchHostile(Vector2 value, float amount, NativeCombat owner, NativeBossController source)
        { hostile = true; bossOwner = source; Launch(value, amount, owner); }
        void FixedUpdate()
        {
            if (!combat || !combat.level || !combat.level.Running || (hostile && (!bossOwner || !bossOwner.isActiveAndEnabled || bossOwner.IsDefeated)))
            { Destroy(gameObject); return; }
            float distance = speed * Time.fixedDeltaTime;
            // Whole-step Unity sweep; both teams stop at the nearest real obstacle or valid recipient.
            var hits = Physics2D.CircleCastAll(transform.position, radius, direction, distance);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (hit.collider.GetComponentInParent<NativeObstacle>()) { Destroy(gameObject); return; }
                if (hostile)
                {
                    var player = hit.collider.GetComponentInParent<NativePlayer>();
                    if (player && player == combat.actor && player.Alive) { combat.HitPlayer(damage); Destroy(gameObject); return; }
                }
                else
                {
                    var enemy = hit.collider.GetComponentInParent<NativeEnemy>();
                    if (enemy && enemy.run == combat.level && enemy.Alive) { combat.HitEnemy(enemy, damage); Destroy(gameObject); return; }
                    var boss = hit.collider.GetComponentInParent<NativeBossController>();
                    if (boss && boss.level == combat.level && boss.CanBeTargeted) { combat.HitBoss(boss, damage); Destroy(gameObject); return; }
                }
            }
            transform.position += (Vector3)(direction * distance);
        }
    }
}
