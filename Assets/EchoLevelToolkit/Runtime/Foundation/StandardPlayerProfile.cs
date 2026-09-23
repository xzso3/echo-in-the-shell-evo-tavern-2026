using UnityEngine;

namespace Echo.LevelToolkit.Foundation
{
    // A source-traced, read-only reference for authoring. Each host copies values into
    // its own actor; the shared asset is never used as mutable run state.
    [CreateAssetMenu(menuName = "Echo/Level Toolkit/Standard Player Profile")]
    public sealed class StandardPlayerProfile : ScriptableObject
    {
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private string sourceRevision;
        [SerializeField] private string sourceFingerprint;
        [SerializeField] private float moveSpeed;
        [SerializeField] private float colliderRadius;
        [SerializeField] private float maxHealth;
        [SerializeField] private float hurtCooldown;
        [SerializeField] private float weaponRange;
        [SerializeField] private float shotInterval;
        [SerializeField] private float weaponDamage;

        public GameObject PlayerPrefab => playerPrefab;
        public string SourceRevision => sourceRevision;
        public string SourceFingerprint => sourceFingerprint;
        public float MoveSpeed => moveSpeed;
        public float ColliderRadius => colliderRadius;
        public float MaxHealth => maxHealth;
        public float HurtCooldown => hurtCooldown;
        public float WeaponRange => weaponRange;
        public float ShotInterval => shotInterval;
        public float WeaponDamage => weaponDamage;

        public bool IsReady => playerPrefab && !string.IsNullOrWhiteSpace(sourceRevision)
            && !string.IsNullOrWhiteSpace(sourceFingerprint)
            && Positive(moveSpeed) && Positive(colliderRadius) && Positive(maxHealth)
            && Positive(hurtCooldown) && Positive(weaponRange)
            && Positive(shotInterval) && Positive(weaponDamage);

        private static bool Positive(float value) => value > 0 && !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
