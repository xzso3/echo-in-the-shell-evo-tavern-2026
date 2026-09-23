using UnityEngine;

namespace Echo.LevelToolkit.Combat
{
    [RequireComponent(typeof(CombatEnemy))]
    public sealed class CombatFanAttack : MonoBehaviour
    {
        public CombatProjectile projectilePrefab;
        public float range = 6.5f, warmupSeconds = .75f, cooldownSeconds = 2.4f;
        public float shotSpeed = 7.5f, shotDamage = 11, spreadDegrees = 18;
        CombatEnemy actor;
        SpriteRenderer view;
        LineRenderer warning;
        Color restingColor;
        Vector2 lockedDirection;
        float fireAt, nextAttack;
        bool charging;
        public bool Initialize()
        {
            actor = GetComponent<CombatEnemy>();
            if (!actor || !actor.World || !projectilePrefab || !CombatPlayer.Valid(range) ||
                !CombatPlayer.Valid(warmupSeconds) || !CombatPlayer.Valid(cooldownSeconds) ||
                !CombatPlayer.Valid(shotSpeed) || !CombatPlayer.Valid(shotDamage) || spreadDegrees < 0 ||
                float.IsNaN(spreadDegrees) || float.IsInfinity(spreadDegrees)) return false;
            view = actor.view;
            if (view) restingColor = view.color;
            warning = new GameObject("Arc Sentry aim warning").AddComponent<LineRenderer>();
            warning.transform.SetParent(transform, false); warning.useWorldSpace = true; warning.positionCount = 2;
            warning.sharedMaterial = view ? view.sharedMaterial : null;
            warning.startWidth = warning.endWidth = .055f;
            warning.startColor = warning.endColor = new Color(1, .35f, .7f, .9f);
            warning.sortingOrder = 210; warning.enabled = false;
            nextAttack = Time.time + 1;
            return true;
        }
        void OnDisable() { CancelCharge(); }
        void Update()
        {
            if (!actor || !actor.World || !actor.World.IsRunning || !actor.Alive || !projectilePrefab)
            { CancelCharge(); return; }
            if (charging)
            {
                UpdateWarning();
                if (view) view.color = Color.Lerp(restingColor, new Color(1, .25f, .7f), .55f + .45f * Mathf.Sin(Time.time * 18));
                if (Time.time >= fireAt) { Fire(); CancelCharge(); nextAttack = Time.time + cooldownSeconds; }
                return;
            }
            if (Time.time < nextAttack) return;
            Vector2 delta = (Vector2)actor.World.Player.transform.position - (Vector2)transform.position;
            if (delta.sqrMagnitude < .01f || delta.sqrMagnitude > range * range ||
                CombatSight.Blocked(transform.position, actor.World.Player.transform.position)) return;
            lockedDirection = delta.normalized; charging = true; fireAt = Time.time + warmupSeconds;
            warning.enabled = true; UpdateWarning();
        }
        void UpdateWarning()
        {
            if (!warning) return;
            Vector2 start = (Vector2)transform.position + lockedDirection * .53f;
            warning.SetPosition(0, start);
            warning.SetPosition(1, CombatSight.EndAtObstacle(start, lockedDirection, range));
        }
        void Fire()
        {
            Vector2 origin = (Vector2)transform.position + lockedDirection * .53f;
            if (CombatSight.Blocked(transform.position, origin)) return;
            for (int index = -1; index <= 1; index++)
            {
                Vector2 direction = Quaternion.Euler(0, 0, spreadDegrees * index) * lockedDirection;
                var shot = Instantiate(projectilePrefab, origin, Quaternion.identity, actor.World.transform);
                shot.speed = shotSpeed; shot.lifetime = range / Mathf.Max(.1f, shotSpeed);
                var sprite = shot.GetComponent<SpriteRenderer>();
                if (sprite) sprite.color = new Color(1, .28f, .78f);
                shot.gameObject.SetActive(true);
                if (!shot.Launch(actor.World, actor, direction, shotDamage, true)) Destroy(shot.gameObject);
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
