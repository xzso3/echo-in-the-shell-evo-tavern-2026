using UnityEngine;
namespace PlagueSurvivor
{
    [ExecuteAlways, RequireComponent(typeof(Camera))]
    public sealed class BossArenaCamera : MonoBehaviour
    {
        // Reserve top/bottom space for HUD, while retaining the full 18 x 12 arena at 16:9 and 16:10.
        void LateUpdate()
        {
            var cameraComponent = GetComponent<Camera>();
            cameraComponent.orthographicSize = Mathf.Max(8.4f, 10.2f / cameraComponent.aspect);
            transform.position = new Vector3(0, .5f, -10);
        }
    }
}
