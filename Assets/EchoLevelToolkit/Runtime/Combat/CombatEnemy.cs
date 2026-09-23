using System;
using System.Collections;
using UnityEngine;

namespace Echo.LevelToolkit.Combat
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class CombatEnemy : MonoBehaviour
    {
        public enum MovementStyle { Chase, Orbit }
        public SpriteRenderer view;
        public float speed = 1.65f, health = 72, contactDamage = 15, detectionRange = 9;
        public MovementStyle movementStyle;
        public float preferredRange = 4.5f;
        public bool orbitClockwise = true;
        public bool Alive => health > 0 && !deathEmitted;
        public CombatWorld World { get; private set; }
        public event Action<CombatEnemy> Died;
        Rigidbody2D body;
        Color restingColor;
        bool deathEmitted;
        void Awake() { body = GetComponent<Rigidbody2D>(); if (view) restingColor = view.color; }
        public bool Initialize(CombatWorld world)
        {
            if (World || !world || !world.Context.IsRunning || !isActiveAndEnabled || !CombatPlayer.Valid(speed) ||
                !CombatPlayer.Valid(health) || !CombatPlayer.Valid(detectionRange) || contactDamage < 0 ||
                float.IsNaN(contactDamage) || float.IsInfinity(contactDamage) ||
                (movementStyle == MovementStyle.Orbit && !CombatPlayer.Valid(preferredRange))) return false;
            World = world;
            if (!world.Register(this)) { World = null; return false; }
            return true;
        }
        void OnEnable() { if (World && !deathEmitted) World.Register(this); }
        void OnDisable() { if (body) body.velocity = Vector2.zero; if (World) World.Unregister(this); }
        void OnDestroy() { if (World) World.Unregister(this); }
        void FixedUpdate()
        {
            if (!World || !World.IsRunning || !Alive) { body.velocity = Vector2.zero; return; }
            Vector2 delta = World.Player.transform.position - transform.position;
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
            if (CombatSight.Blocked(body.position, body.position + direction * .9f))
            {
                var left = new Vector2(-direction.y, direction.x);
                direction = !CombatSight.Blocked(body.position, body.position + left * .9f) ? left : -left;
            }
            body.velocity = direction * speed;
            if (view) { view.flipX = direction.x < 0; view.sortingOrder = 100 - Mathf.RoundToInt(transform.position.y * 10); }
        }
        void OnCollisionStay2D(Collision2D collision)
        {
            var player = collision.collider.GetComponentInParent<CombatPlayer>();
            if (player && World && player == World.Player && Alive && contactDamage > 0) player.ReceiveDamage(contactDamage);
        }
        public bool ReceiveDamage(float amount)
        {
            if (!World || !World.IsRunning || !Alive || !CombatPlayer.Valid(amount)) return false;
            health = Mathf.Max(0, health - amount);
            if (health <= 0)
            {
                deathEmitted = true; // Set before invoking subscribers; reentry cannot double count.
                Died?.Invoke(this);
                Destroy(gameObject);
            }
            else if (view) StartCoroutine(Flash());
            return true;
        }
        IEnumerator Flash()
        {
            view.color = new Color(1, .35f, .35f);
            yield return new WaitForSeconds(.08f);
            if (view) view.color = restingColor;
        }
    }
}
