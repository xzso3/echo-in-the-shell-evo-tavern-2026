using Echo.LevelToolkit.Combat;
using UnityEngine;

namespace Echo.NativeGame
{
    // Keeps the saved Native scene contract while CombatWeapon owns fire and kills.
    public sealed class NativeCombat : MonoBehaviour
    {
        public NativeRunController level;
        public NativePlayer actor;
        public NativeProjectile projectilePrefab;
        public float range = 7, shotInterval = .38f, damage = 24;

        CombatWeapon sharedWeapon;
        internal CombatWorld World => sharedWeapon ? sharedWeapon.World : null;
        public bool AutoFire => sharedWeapon ? sharedWeapon.AutoFire : true;
        public int Kills => sharedWeapon ? sharedWeapon.Kills : 0;

        internal bool InitializeSharedWeapon(CombatWorld world)
        {
            if (sharedWeapon || !isActiveAndEnabled || !level || !actor || !projectilePrefab ||
                actor.SharedActor != world.Player) return false;
            sharedWeapon = actor.GetComponent<CombatWeapon>();
            var projectile = projectilePrefab.GetComponent<CombatProjectile>();
            if (!sharedWeapon || !projectile) return false;
            sharedWeapon.projectilePrefab = projectile;
            sharedWeapon.range = range;
            sharedWeapon.shotInterval = shotInterval;
            sharedWeapon.damage = damage;
            return sharedWeapon.Initialize(world);
        }

        void OnDisable() { if (sharedWeapon) sharedWeapon.enabled = false; }
        void OnEnable() { if (sharedWeapon) sharedWeapon.enabled = true; }

        void Update()
        {
            if (!sharedWeapon || !level || !level.Running) return;
            // Equipment changes these legacy serialized fields during play.
            sharedWeapon.range = range;
            sharedWeapon.shotInterval = shotInterval;
            sharedWeapon.damage = damage;
        }

        public void ToggleFire() { if (sharedWeapon && level && level.IsCombatAdvancing) sharedWeapon.ToggleFire(); }

        public void HitEnemy(NativeEnemy target, float amount)
        {
            if (World && level && level.IsCombatAdvancing && target && target.run == level)
                target.ReceiveDamage(amount);
        }

        public void HitBoss(NativeBossController target, float amount)
        {
            if (World && level && level.IsCombatAdvancing && target && target.level == level)
                target.ReceiveDamage(amount);
        }

        public void HitPlayer(float amount)
        {
            if (World && level && level.IsCombatAdvancing && actor && actor.Alive)
                actor.ReceiveDamage(amount);
        }
    }
}
