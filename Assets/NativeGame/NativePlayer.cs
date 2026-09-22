using System.Collections;
using UnityEngine;
namespace Echo.NativeGame
{
    // Actor: Unity object identity, movement, health and hurt immunity. Weapon state belongs to Combat.
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class NativePlayer : MonoBehaviour
    {
        public NativeRunController run;
        public SpriteRenderer view;
        public float speed = 4.5f, maxHealth = 100;
        public float Health { get; private set; }
        public bool Alive => Health > 0;
        public Vector2 MoveInput { get; set; }
        Rigidbody2D body;
        float nextHurt;
        void Awake() { body = GetComponent<Rigidbody2D>(); Health = maxHealth; }
        void FixedUpdate() { body.velocity = run && run.Running ? MoveInput * speed : Vector2.zero; }
        void Update()
        {
            if (!run || !run.Running) return;
            if (MoveInput.x != 0) view.flipX = MoveInput.x < 0;
            view.sortingOrder = 100 - Mathf.RoundToInt(transform.position.y * 10);
        }
        public void ReceiveDamage(float value)
        {
            if (!Alive || !run || !run.Running || Time.time < nextHurt) return;
            Health = Mathf.Max(0, Health - value); nextHurt = Time.time + .65f;
            if (!Alive) { body.velocity = Vector2.zero; view.color = new Color(.5f, .3f, .4f); run.PlayerDied(); }
            else StartCoroutine(Flash());
        }
        IEnumerator Flash() { view.color = new Color(1, .3f, .4f); yield return new WaitForSeconds(.12f); if (Alive) view.color = Color.white; }
    }
}
