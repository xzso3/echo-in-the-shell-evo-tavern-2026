using System.Collections;
using UnityEngine;

namespace Echo.NativeGame
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class NativePlayer : MonoBehaviour
    {
        public NativeRunController run;
        public SpriteRenderer view;
        public NativeProjectile projectilePrefab;
        public float speed = 4.5f, maxHealth = 100, range = 7, shotInterval = .38f, damage = 24;
        public float Health { get; private set; }
        public bool AutoFire { get; private set; } = true;
        public bool Alive => Health > 0;
        public Vector2 MoveInput { get; set; }
        Rigidbody2D body;
        float nextShot, nextHurt;
        void Awake() { body = GetComponent<Rigidbody2D>(); Health = maxHealth; }
        void FixedUpdate() { body.velocity = run && run.Running ? MoveInput * speed : Vector2.zero; }
        void Update()
        {
            if (!run || !run.Running) return;
            if (MoveInput.x != 0) view.flipX = MoveInput.x < 0;
            view.sortingOrder = 100 - Mathf.RoundToInt(transform.position.y * 10);
            if (!AutoFire || Time.time < nextShot) return;
            NativeEnemy target = null; float nearest = range * range;
            foreach (var enemy in NativeEnemy.Active)
            {
                if (!enemy || !enemy.Alive) continue;
                float distance = ((Vector2)(enemy.transform.position - transform.position)).sqrMagnitude;
                if (distance < nearest && !NativeObstacle.Blocked(transform.position, enemy.transform.position)) { target = enemy; nearest = distance; }
            }
            if (!target) return;
            Vector2 direction = ((Vector2)(target.transform.position - transform.position)).normalized;
            var shot = Instantiate(projectilePrefab, transform.position + (Vector3)(direction * .48f), Quaternion.identity);
            shot.Launch(direction, damage, run);
            view.flipX = direction.x < 0; nextShot = Time.time + shotInterval;
        }
        public void ToggleFire() { AutoFire = !AutoFire; }
        public void TakeDamage(float value)
        {
            if (!Alive || !run || !run.Running || Time.time < nextHurt) return;
            Health = Mathf.Max(0, Health - value); nextHurt = Time.time + .65f;
            if (!Alive) { body.velocity = Vector2.zero; view.color = new Color(.5f, .3f, .4f); run.PlayerDied(); }
            else StartCoroutine(Flash());
        }
        IEnumerator Flash() { view.color = new Color(1, .3f, .4f); yield return new WaitForSeconds(.12f); if (Alive) view.color = Color.white; }
    }
}
