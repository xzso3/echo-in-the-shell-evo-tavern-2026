using UnityEngine;

namespace Echo.NativeGame
{
    // Actor owns the stationary Boss's shell health and terminal life state.
    [DisallowMultipleComponent]
    public sealed class NativeBossActor : MonoBehaviour
    {
        [Min(1)] public float maxArmor = 1400;
        [Range(.1f, 1)] public float armorDamageScale = .45f;
        public float Armor { get; private set; }
        public bool ArmorBroken => Armor <= 0;
        public bool IsDefeated { get; private set; }
        void Awake() { Armor = maxArmor; }
        public void ReceiveDamage(float amount)
        {
            if (IsDefeated || ArmorBroken || amount <= 0 || float.IsNaN(amount) || float.IsInfinity(amount)) return;
            Armor = Mathf.Max(0, Armor - amount * armorDamageScale);
        }
        // Only the encounter's validated nearby E action calls this. Bullets never finish the Boss.
        internal bool FinishCore()
        {
            if (IsDefeated || !ArmorBroken) return false;
            IsDefeated = true;
            return true;
        }
    }
}
