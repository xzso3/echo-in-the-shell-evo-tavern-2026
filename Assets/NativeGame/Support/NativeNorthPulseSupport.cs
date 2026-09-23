using UnityEngine;

namespace Echo.NativeGame
{
    // One local tactical effect. Combat owns damage; this component only owns its one-use state.
    public sealed class NativeNorthPulseSupport : MonoBehaviour
    {
        public NativeRunController level;
        public NativeEnemy target;
        public float damage = 48;
        public bool Used { get; private set; }

        public bool CanExecute => !Used && isActiveAndEnabled && level && level.Running &&
            level.combat && level.combat.level == level && level.player && level.player.Alive &&
            target && target.isActiveAndEnabled && target.run == level && target.Alive &&
            target.gameObject.scene == level.gameObject.scene &&
            damage > 0 && !float.IsNaN(damage) && !float.IsInfinity(damage);

        public bool TryExecute()
        {
            if (!CanExecute) return false;
            float before = target.health;
            level.combat.HitEnemy(target, damage);
            if (!(target.health < before)) return false;
            Used = true;
            return true;
        }
    }
}
