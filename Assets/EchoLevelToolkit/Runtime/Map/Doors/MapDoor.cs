using Echo.LevelToolkit.Foundation;
using UnityEngine;

namespace Echo.LevelToolkit.Map.Doors
{
    // Geometry belongs to the placed map; MapDoorSet alone owns its open state.
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class MapDoor : MonoBehaviour
    {
        [SerializeField] private ContentIdentity doorId;
        [SerializeField] private ContentIdentity setOpenEndpoint;
        [SerializeField] private BoxCollider2D blocker;
        [SerializeField] private bool initiallyOpen = true;
        [SerializeField] private GameObject openVisual;
        [SerializeField] private GameObject closedVisual;

        public ContentIdentity DoorId => doorId;
        public ContentIdentity SetOpenEndpoint => setOpenEndpoint;
        public bool InitiallyOpen => initiallyOpen;
        public BoxCollider2D Blocker => blocker;

        internal void ApplyOpen(bool open)
        {
            if (blocker) blocker.enabled = !open;
            if (openVisual) openVisual.SetActive(open);
            if (closedVisual) closedVisual.SetActive(!open);
        }

        // Box geometry remains available while the blocker is disabled.
        public Bounds ClosureBounds
        {
            get
            {
                if (!blocker) return default;
                Vector2 half = blocker.size * .5f;
                Vector2 offset = blocker.offset;
                Transform shape = blocker.transform;
                Vector3 first = shape.TransformPoint(offset + new Vector2(-half.x, -half.y));
                var bounds = new Bounds(first, Vector3.zero);
                bounds.Encapsulate(shape.TransformPoint(offset + new Vector2(-half.x, half.y)));
                bounds.Encapsulate(shape.TransformPoint(offset + new Vector2(half.x, -half.y)));
                bounds.Encapsulate(shape.TransformPoint(offset + new Vector2(half.x, half.y)));
                return bounds;
            }
        }
    }
}
