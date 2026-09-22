using UnityEngine;

namespace Echo.NativeGame
{
    public sealed class NativeProjectile : MonoBehaviour
    {
        public float speed = 18, lifetime = 1.6f, radius = .09f;
        Vector2 direction;
        float damage;
        NativeCombat combat;
        public void Launch(Vector2 value, float amount, NativeCombat owner)
        {
            direction = value; damage = amount; combat = owner;
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(value.y, value.x) * Mathf.Rad2Deg);
            Destroy(gameObject, lifetime);
        }
        void FixedUpdate()
        {
            if (!combat || !combat.level.Running) { Destroy(gameObject); return; }
            float distance = speed * Time.fixedDeltaTime;
            // Sweeps the entire step against actual Unity colliders instead of tunnelling through thin walls.
            foreach (var hit in Physics2D.CircleCastAll(transform.position, radius, direction, distance))
            {
                if (hit.collider.GetComponent<NativeObstacle>()) { Destroy(gameObject); return; }
                var enemy = hit.collider.GetComponent<NativeEnemy>();
                if (enemy && enemy.Alive) { combat.HitEnemy(enemy, damage); Destroy(gameObject); return; }
            }
            transform.position += (Vector3)(direction * distance);
        }
    }
}
