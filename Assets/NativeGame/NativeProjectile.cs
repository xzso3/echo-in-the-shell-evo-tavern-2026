using Echo.LevelToolkit.Combat;
using UnityEngine;

namespace Echo.NativeGame
{
    // Saved Pulse facade. CombatProjectile is the only moving and hit-resolving component.
    public sealed class NativeProjectile : MonoBehaviour
    {
        public float speed = 18, lifetime = 1.6f, radius = .09f;

        public void Launch(Vector2 value, float amount, NativeCombat owner)
        {
            LaunchShared(value, amount, owner, owner && owner.actor ? owner.actor.SharedActor : null, false);
        }

        public void LaunchHostile(Vector2 value, float amount, NativeCombat owner, NativeBossController source)
        {
            LaunchShared(value, amount, owner, source ? source.SharedActor : null, true);
        }

        public void LaunchHostile(Vector2 value, float amount, NativeCombat owner, NativeEnemy source)
        {
            LaunchShared(value, amount, owner, source ? source.SharedActor : null, true);
        }

        void LaunchShared(Vector2 direction, float amount, NativeCombat owner, Component source, bool hostile)
        {
            var shared = GetComponent<CombatProjectile>();
            if (!shared || !owner || !owner.World || !source)
            {
                Debug.LogError("NativeProjectile: shared combat projectile or owner is missing.", this);
                Destroy(gameObject);
                return;
            }
            shared.speed = speed;
            shared.lifetime = lifetime;
            shared.radius = radius;
            transform.SetParent(owner.World.transform, true);
            if (!shared.Launch(owner.World, source, direction, amount, hostile)) Destroy(gameObject);
        }
    }
}
