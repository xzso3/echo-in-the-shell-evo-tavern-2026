using UnityEngine;

namespace Echo.LevelToolkit.Map
{
    // Minimal isolated scene probe. It makes no claim about Native player clearance.
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class ChunkMapPreviewWalker : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float speed = 4.5f;
        private Rigidbody2D body;
        private Vector2 movement;

        private void Awake() => body = GetComponent<Rigidbody2D>();

        private void Update()
        {
            movement = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (movement.sqrMagnitude > 1f) movement.Normalize();
        }

        private void FixedUpdate() => body.MovePosition(body.position + movement * speed * Time.fixedDeltaTime);
    }
}
