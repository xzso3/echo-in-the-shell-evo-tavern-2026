using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Echo.NativeGame
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class NativeEnemy : MonoBehaviour
    {
        public static readonly HashSet<NativeEnemy> Active = new HashSet<NativeEnemy>();
        public NativeRunController run;
        public SpriteRenderer view;
        public float speed = 1.65f, health = 72, contactDamage = 15, detectionRange = 9;
        public bool Alive => health > 0;
        Rigidbody2D body;
        void Awake() { body = GetComponent<Rigidbody2D>(); }
        void OnEnable() { Active.Add(this); }
        void OnDisable() { Active.Remove(this); }
        void Start() { if (!run) run = FindObjectOfType<NativeRunController>(); }
        void FixedUpdate()
        {
            if (!run || !run.Running || !Alive) { body.velocity = Vector2.zero; return; }
            Vector2 delta = run.player.transform.position - transform.position;
            if (delta.magnitude > detectionRange) { body.velocity = Vector2.zero; return; }
            Vector2 direction = delta.normalized;
            // Short feelers steer around visible obstacles; Physics2D remains the collision authority.
            if (NativeObstacle.Blocked(body.position, body.position + direction * .9f))
            {
                var left = new Vector2(-direction.y, direction.x);
                direction = !NativeObstacle.Blocked(body.position, body.position + left * .9f) ? left : -left;
            }
            body.velocity = direction * speed;
            view.flipX = direction.x < 0; view.sortingOrder = 100 - Mathf.RoundToInt(transform.position.y * 10);
        }
        void OnCollisionStay2D(Collision2D collision)
        { var player = collision.collider.GetComponent<NativePlayer>(); if (player && Alive) player.TakeDamage(contactDamage); }
        public void Hit(float damage)
        {
            if (!Alive) return;
            health -= damage;
            if (!Alive) { if (run) run.RecordKill(); Destroy(gameObject); }
            else StartCoroutine(Flash());
        }
        IEnumerator Flash() { view.color = new Color(1, .35f, .35f); yield return new WaitForSeconds(.08f); if (view) view.color = Color.white; }
    }
}
