using System;
using System.Collections;
using UnityEngine;

namespace Echo.LevelToolkit.Combat
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class CombatPlayer : MonoBehaviour
    {
        public SpriteRenderer view;
        public float speed = 4.5f, maxHealth = 100, hurtCooldown = .65f;
        public float Health { get; private set; }
        public bool Alive => Health > 0;
        public Vector2 MoveInput { get; set; }
        public event Action<CombatPlayer> Died;
        public CombatWorld World { get; private set; }
        Rigidbody2D body;
        float nextHurt;
        bool deathEmitted;
        void Awake() { body = GetComponent<Rigidbody2D>(); Health = maxHealth; }
        public bool Initialize(CombatWorld world)
        {
            if (World || !world || !isActiveAndEnabled || !Valid(speed) || !Valid(maxHealth) || !Valid(hurtCooldown)) return false;
            World = world;
            Health = maxHealth;
            return true;
        }
        void FixedUpdate() { body.velocity = World && World.IsRunning ? MoveInput * speed : Vector2.zero; }
        void Update()
        {
            if (!World || !World.IsRunning || !view) return;
            if (MoveInput.x != 0) view.flipX = MoveInput.x < 0;
            view.sortingOrder = 100 - Mathf.RoundToInt(transform.position.y * 10);
        }
        public bool TryHeal(float amount)
        {
            if (!isActiveAndEnabled || !World || !World.IsRunning || !Alive || !Valid(amount) || Health >= maxHealth) return false;
            float healed = Mathf.Min(maxHealth, Health + amount);
            if (healed <= Health) return false;
            Health = healed;
            return true;
        }
        public bool ReceiveDamage(float value)
        {
            if (!isActiveAndEnabled || !World || !World.IsRunning || !Alive || !Valid(value) || Time.time < nextHurt) return false;
            Health = Mathf.Max(0, Health - value);
            nextHurt = Time.time + hurtCooldown;
            if (!Alive)
            {
                body.velocity = Vector2.zero;
                if (view) view.color = new Color(.5f, .3f, .4f);
                if (!deathEmitted) { deathEmitted = true; Died?.Invoke(this); World.Context.Run.ReportPlayerDeath(); }
            }
            else if (view) StartCoroutine(Flash());
            return true;
        }
        IEnumerator Flash()
        {
            view.color = new Color(1, .3f, .4f);
            yield return new WaitForSeconds(.12f);
            if (view && Alive) view.color = Color.white;
        }
        public static bool Valid(float value) => value > 0 && !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
