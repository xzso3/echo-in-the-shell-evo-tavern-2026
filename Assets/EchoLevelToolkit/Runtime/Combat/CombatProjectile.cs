using System;
using UnityEngine;

namespace Echo.LevelToolkit.Combat
{
    public sealed class CombatProjectile : MonoBehaviour
    {
        public float speed = 18, lifetime = 1.6f, radius = .09f;
        CombatWorld world;
        Component owner;
        Vector2 direction;
        float damage, expiresAt;
        bool hostile, launched;

        public bool Launch(CombatWorld combatWorld, Component source, Vector2 value, float amount, bool isHostile)
        {
            if (launched || !combatWorld || !combatWorld.CanAdvance || !source || !source.gameObject.activeInHierarchy ||
                value.sqrMagnitude < .0001f || !CombatPlayer.Valid(amount) || !CombatPlayer.Valid(speed) ||
                !CombatPlayer.Valid(lifetime) || !CombatPlayer.Valid(radius)) return false;
            world = combatWorld; owner = source; direction = value.normalized; damage = amount; hostile = isHostile;
            expiresAt = Time.time + lifetime; launched = true;
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            return true;
        }
        void FixedUpdate()
        {
            if (!launched || !world || !world.IsRunning || !OwnerAlive())
            { Destroy(gameObject); return; }
            if (!world.CanAdvance) return;
            if (Time.time >= expiresAt) { Destroy(gameObject); return; }
            float distance = speed * Time.fixedDeltaTime;
            var hits = Physics2D.CircleCastAll(transform.position, radius, direction, distance);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (!hit.collider) continue;
                if (hit.collider.GetComponentInParent<CombatObstacle>()) { Destroy(gameObject); return; }
                if (hostile)
                {
                    var player = hit.collider.GetComponentInParent<CombatPlayer>();
                    if (player && player == world.Player && player.Alive)
                    { player.ReceiveDamage(damage); Destroy(gameObject); return; }
                }
                else
                {
                    var enemy = hit.collider.GetComponentInParent<CombatEnemy>();
                    if (enemy && enemy.World == world && enemy.Alive)
                    {
                        enemy.ReceiveDamage(damage);
                        if (!enemy.Alive && owner is CombatPlayer playerOwner)
                        {
                            var weapon = playerOwner.GetComponent<CombatWeapon>();
                            if (weapon && weapon.World == world) weapon.RecordKill();
                        }
                        Destroy(gameObject); return;
                    }
                    var boss = hit.collider.GetComponentInParent<CombatBoss>();
                    if (boss && boss.World == world && boss.CanBeTargeted)
                    { boss.ReceiveDamage(damage); Destroy(gameObject); return; }
                }
            }
            transform.position += (Vector3)(direction * distance);
        }
        bool OwnerAlive()
        {
            if (!owner || !owner.gameObject.activeInHierarchy) return false;
            if (owner is CombatPlayer player) return player.World == world && player.Alive;
            if (owner is CombatEnemy enemy) return enemy.World == world && enemy.Alive;
            if (owner is CombatBoss boss) return boss.World == world && !boss.IsDefeated && boss.IsEncounterActive;
            return false;
        }
    }
}
