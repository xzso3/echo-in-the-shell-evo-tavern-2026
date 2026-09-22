using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Echo.NativeGame
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class NativeEnemy : MonoBehaviour
    {
        public enum MovementStyle { Chase, Orbit }
        public static readonly HashSet<NativeEnemy> Active = new HashSet<NativeEnemy>();
        public NativeRunController run;
        public SpriteRenderer view;
        public float speed = 1.65f, health = 72, contactDamage = 15, detectionRange = 9;
        public MovementStyle movementStyle;
        public float preferredRange = 4.5f;
        public bool orbitClockwise = true;
        public bool Alive => health > 0;
        Rigidbody2D body;
        Color restingColor;
        void Awake() { body = GetComponent<Rigidbody2D>(); if (view) restingColor = view.color; }
        void OnEnable() { Active.Add(this); }
        void OnDisable() { Active.Remove(this); }
        void Start() { if (!run) run = FindObjectOfType<NativeRunController>(); }
        void FixedUpdate()
        {
            if (!run || !run.Running || !Alive) { body.velocity = Vector2.zero; return; }
            Vector2 delta = run.player.transform.position - transform.position;
            float distance = delta.magnitude;
            if (distance > detectionRange) { body.velocity = Vector2.zero; return; }
            Vector2 direction = delta.normalized;
            if (movementStyle == MovementStyle.Orbit && distance > .01f)
            {
                Vector2 tangent = orbitClockwise ? new Vector2(direction.y, -direction.x) : new Vector2(-direction.y, direction.x);
                if (distance < preferredRange - 1) direction = (-direction * 1.5f + tangent * .3f).normalized;
                else if (distance > preferredRange + 1) direction = (direction + tangent * .3f).normalized;
                else direction = (tangent + direction * Mathf.Clamp((distance - preferredRange) * .8f, -.7f, .7f)).normalized;
            }
            // Short feelers steer around visible obstacles; Physics2D remains the collision authority.
            if (NativeObstacle.Blocked(body.position, body.position + direction * .9f))
            {
                var left = new Vector2(-direction.y, direction.x);
                direction = !NativeObstacle.Blocked(body.position, body.position + left * .9f) ? left : -left;
            }
            body.velocity = direction * speed;
            if (view) { view.flipX = direction.x < 0; view.sortingOrder = 100 - Mathf.RoundToInt(transform.position.y * 10); }
        }
        void OnCollisionStay2D(Collision2D collision)
        { var player = collision.collider.GetComponent<NativePlayer>(); if (player && Alive && run && run.combat && contactDamage > 0) run.combat.HitPlayer(contactDamage); }
        public void ReceiveDamage(float damage)
        {
            if (!Alive) return;
            health -= damage;
            if (!Alive) { Destroy(gameObject); }
            else if (view) StartCoroutine(Flash());
        }
        IEnumerator Flash() { view.color = new Color(1, .35f, .35f); yield return new WaitForSeconds(.08f); if (view) view.color = restingColor; }
    }
}
