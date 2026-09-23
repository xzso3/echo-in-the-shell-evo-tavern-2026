using Echo.LevelToolkit.Combat;
using UnityEngine;

namespace Echo.NativeGame
{
    // Saved Boss actor facade; CombatBoss is the sole shell and terminal state owner.
    [DisallowMultipleComponent]
    public sealed class NativeBossActor : MonoBehaviour
    {
        [Min(1)] public float maxArmor = 1400;
        [Range(.1f, 1)] public float armorDamageScale = .45f;

        CombatBoss sharedActor;
        public float Armor => sharedActor ? sharedActor.Armor : maxArmor;
        public bool ArmorBroken => sharedActor && sharedActor.ArmorBroken;
        public bool IsDefeated => sharedActor && sharedActor.IsDefeated;

        internal void BindSharedActor(CombatBoss actor) { sharedActor = actor; }

        public void ReceiveDamage(float amount)
        {
            if (sharedActor) sharedActor.ReceiveDamage(amount);
        }

        // Preserve the old internal entry, including its player/range validation.
        internal bool FinishCore()
        {
            var controller = GetComponent<NativeBossController>();
            return controller && controller.level && controller.InteractCore(controller.level.player);
        }
    }
}
