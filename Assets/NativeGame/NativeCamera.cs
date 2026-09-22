using UnityEngine;
namespace Echo.NativeGame
{
    public sealed class NativeCamera : MonoBehaviour
    {
        public Transform target;
        public Vector2 worldHalfSize = new Vector2(15, 9);
        public float smoothing = 7;
        Camera view;
        void Awake() { view = GetComponent<Camera>(); }
        void LateUpdate()
        {
            if (!target) return;
            float x = Mathf.Max(0, worldHalfSize.x - view.orthographicSize * view.aspect), y = Mathf.Max(0, worldHalfSize.y - view.orthographicSize);
            var at = new Vector3(Mathf.Clamp(target.position.x, -x, x), Mathf.Clamp(target.position.y, -y, y), -10);
            transform.position = Vector3.Lerp(transform.position, at, 1 - Mathf.Exp(-smoothing * Time.deltaTime));
        }
    }
}
