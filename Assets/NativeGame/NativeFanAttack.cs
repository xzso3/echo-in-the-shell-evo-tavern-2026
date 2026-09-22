using UnityEngine;

namespace Echo.NativeGame
{
    // Combat for the Arc Sentry. The NativeEnemy actor owns health and movement.
    [RequireComponent(typeof(NativeEnemy))]
    public sealed class NativeFanAttack : MonoBehaviour
    {
        public NativeProjectile projectilePrefab;
        public float range = 6.5f;
        public float warmupSeconds = .75f;
        public float cooldownSeconds = 2.4f;
        public float shotSpeed = 7.5f;
        public float shotDamage = 11;
        public float spreadDegrees = 18;

        NativeEnemy actor;
        SpriteRenderer view;
        LineRenderer warning;
        Color restingColor;
        Vector2 lockedDirection;
        float fireAt, nextAttack;
        bool charging;

        void Awake()
        {
            actor = GetComponent<NativeEnemy>();
            view = actor.view;
            if (view) restingColor = view.color;
            warning = new GameObject("Arc Sentry aim warning").AddComponent<LineRenderer>();
            warning.transform.SetParent(transform, false);
            warning.useWorldSpace = true;
            warning.positionCount = 2;
            warning.sharedMaterial = view ? view.sharedMaterial : null;
            warning.startWidth = warning.endWidth = .055f;
            warning.startColor = warning.endColor = new Color(1, .35f, .7f, .9f);
            warning.sortingOrder = 210;
            warning.enabled = false;
        }

        void OnEnable() { nextAttack = Time.time + 1; }
        void OnDisable() { CancelCharge(); }

        void Update()
        {
            var run = actor.run;
            if (!run || !run.Running || !run.combat || !run.player || !actor.Alive || !projectilePrefab)
            { CancelCharge(); return; }

            if (charging)
            {
                UpdateWarning();
                if (view) view.color = Color.Lerp(restingColor, new Color(1, .25f, .7f), .55f + .45f * Mathf.Sin(Time.time * 18));
                if (Time.time >= fireAt)
                {
                    Fire(run.combat);
                    CancelCharge();
                    nextAttack = Time.time + cooldownSeconds;
                }
                return;
            }

            if (Time.time < nextAttack) return;
            Vector2 delta = (Vector2)run.player.transform.position - (Vector2)transform.position;
            if (delta.sqrMagnitude < .01f || delta.sqrMagnitude > range * range || NativeObstacle.Blocked(transform.position, run.player.transform.position)) return;
            lockedDirection = delta.normalized;
            charging = true;
            fireAt = Time.time + warmupSeconds;
            warning.enabled = true;
            UpdateWarning();
        }

        void UpdateWarning()
        {
            if (!warning) return;
            Vector2 start = (Vector2)transform.position + lockedDirection * .53f;
            Vector2 end = start + lockedDirection * range;
            foreach (var hit in Physics2D.RaycastAll(start, lockedDirection, range))
                if (hit.collider.GetComponentInParent<NativeObstacle>() && hit.distance < Vector2.Distance(start, end)) end = hit.point;
            warning.SetPosition(0, start);
            warning.SetPosition(1, end);
        }

        void Fire(NativeCombat combat)
        {
            Vector2 origin = (Vector2)transform.position + lockedDirection * .53f;
            if (NativeObstacle.Blocked(transform.position, origin)) return;
            for (int index = -1; index <= 1; index++)
            {
                Vector2 direction = Quaternion.Euler(0, 0, spreadDegrees * index) * lockedDirection;
                var shot = Instantiate(projectilePrefab, origin, Quaternion.identity);
                shot.speed = shotSpeed;
                shot.lifetime = range / Mathf.Max(.1f, shotSpeed);
                var sprite = shot.GetComponent<SpriteRenderer>();
                if (sprite) sprite.color = new Color(1, .28f, .78f);
                shot.LaunchHostile(direction, shotDamage, combat, actor);
            }
        }

        void CancelCharge()
        {
            charging = false;
            if (warning) warning.enabled = false;
            if (view) view.color = restingColor;
        }
    }
}
