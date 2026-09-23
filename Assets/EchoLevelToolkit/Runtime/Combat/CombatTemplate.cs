using UnityEngine;

namespace Echo.LevelToolkit.Combat
{
    // Authored values copied into actors on spawn; no run state is written to this asset.
    [CreateAssetMenu(menuName = "Echo/Level Toolkit/Combat Template")]
    public sealed class CombatTemplate : ScriptableObject
    {
        public enum Kind { StandardPlayer, ChaseEnemy, OrbitFanEnemy, Boss }
        public Kind kind;
        public string sourceRevision, sourceFingerprint;
        public float speed, health, colliderRadius, hurtCooldown, contactDamage, detectionRange;
        public float range, shotInterval, damage, projectileSpeed, projectileLifetime, projectileRadius;
        public float preferredRange, fanWarmup, fanCooldown, fanShotSpeed, fanShotDamage, fanSpreadDegrees;
        public float armorDamageScale, trackingSeconds, lockSeconds, bossShotInterval;
        public int burstCount;
        public float bossShotSpeed, bossShotDamage, bombWarning, bombInterval, bombRadius, bombDamage;
        public float coreWindowSeconds, retryWindowDelay, coreInteractionRadius;
        public Vector2 muzzleOffset;
    }
}
