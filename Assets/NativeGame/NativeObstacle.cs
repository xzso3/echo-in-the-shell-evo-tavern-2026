using UnityEngine;
namespace Echo.NativeGame
{
    public sealed class NativeObstacle : MonoBehaviour
    {
        public static bool Blocked(Vector2 from, Vector2 to)
        {
            foreach (var hit in Physics2D.LinecastAll(from, to))
                if (hit.collider.GetComponent<NativeObstacle>()) return true;
            return false;
        }
    }
}
