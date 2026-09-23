using Echo.LevelToolkit.Combat;
using UnityEngine;
namespace Echo.NativeGame
{
    public sealed class NativeObstacle : MonoBehaviour
    {
        void Awake()
        {
            // Existing scene colliders gain the shared parent-aware sight marker at runtime.
            if (!GetComponent<CombatObstacle>()) gameObject.AddComponent<CombatObstacle>();
        }

        public static bool Blocked(Vector2 from, Vector2 to)
        {
            return CombatSight.Blocked(from, to);
        }
    }
}
