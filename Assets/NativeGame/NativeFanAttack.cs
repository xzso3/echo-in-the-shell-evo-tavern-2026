using Echo.LevelToolkit.Combat;
using UnityEngine;

namespace Echo.NativeGame
{
    // Serialized Arc Sentry facade; CombatFanAttack owns charging and projectiles.
    [RequireComponent(typeof(NativeEnemy))]
    public sealed class NativeFanAttack : MonoBehaviour
    {
        public NativeProjectile projectilePrefab;
        public float range = 6.5f;
        public float warmupSeconds = .75f;
        public float cooldownSeconds = 2.4f;
        public float shotSpeed = 7.5f;
        public float shotDamage = 11;
        public float spreadDegrees = 18;

        CombatFanAttack sharedAttack;

        internal bool InitializeSharedAttack()
        {
            if (sharedAttack) return sharedAttack.isActiveAndEnabled;
            if (!isActiveAndEnabled || !projectilePrefab) return false;
            var actor = GetComponent<NativeEnemy>();
            if (!actor || !actor.SharedActor || !actor.SharedActor.World) return false;
            sharedAttack = GetComponent<CombatFanAttack>();
            var projectile = projectilePrefab.GetComponent<CombatProjectile>();
            if (!sharedAttack || !projectile) return false;
            sharedAttack.projectilePrefab = projectile;
            sharedAttack.range = range;
            sharedAttack.warmupSeconds = warmupSeconds;
            sharedAttack.cooldownSeconds = cooldownSeconds;
            sharedAttack.shotSpeed = shotSpeed;
            sharedAttack.shotDamage = shotDamage;
            sharedAttack.spreadDegrees = spreadDegrees;
            return sharedAttack.Initialize();
        }

        void OnDisable() { if (sharedAttack) sharedAttack.enabled = false; }
        void OnEnable() { if (sharedAttack) sharedAttack.enabled = true; }
    }
}
