using System;
using System.Collections.Generic;
using Echo.LevelToolkit.Foundation;
using UnityEngine;

namespace Echo.LevelToolkit.Combat
{
    // One world is bound to one placed level instance. No static actor registry is used.
    public sealed class CombatWorld : MonoBehaviour
    {
        readonly List<CombatEnemy> enemies = new List<CombatEnemy>();
        readonly List<CombatBoss> bosses = new List<CombatBoss>();
        public LevelInstanceContext Context { get; private set; }
        public CombatPlayer Player { get; private set; }
        public event Action<CombatEnemy> EnemyDied;
        public bool IsRunning => Context != null && Context.IsRunning && Player && Player.Alive;
        // A live world still accepts support while scaled combat is stopped.
        public bool CanAdvance => IsRunning && Time.timeScale > 0f;
        public IReadOnlyList<CombatEnemy> Enemies => enemies;
        public IReadOnlyList<CombatBoss> Bosses => bosses;

        public bool Initialize(LevelInstanceContext context, CombatPlayer player)
        {
            if (Context != null || context == null || !player || context.Player != player.transform) return false;
            Context = context;
            Player = player;
            return player.Initialize(this);
        }

        public bool Register(CombatEnemy enemy)
        {
            if (!enemy || Context == null || !enemy.isActiveAndEnabled || enemies.Contains(enemy)) return false;
            enemies.Add(enemy);
            enemy.Died += OnEnemyDied;
            return true;
        }
        public bool Register(CombatBoss boss)
        {
            if (!boss || Context == null || !boss.isActiveAndEnabled || bosses.Contains(boss)) return false;
            bosses.Add(boss);
            return true;
        }
        public void Unregister(CombatEnemy enemy)
        {
            if (!enemies.Remove(enemy)) return;
            if (enemy) enemy.Died -= OnEnemyDied;
        }
        void OnEnemyDied(CombatEnemy enemy) { EnemyDied?.Invoke(enemy); }
        public void Unregister(CombatBoss boss) { bosses.Remove(boss); }
        void OnDestroy()
        {
            foreach (var enemy in enemies) if (enemy) enemy.Died -= OnEnemyDied;
            enemies.Clear(); bosses.Clear(); Context = null; Player = null;
        }
    }

    // Put this marker on the Collider2D or any parent, including a Tilemap parent.
    public sealed class CombatObstacle : MonoBehaviour { }

    public static class CombatSight
    {
        public static bool Blocked(Vector2 from, Vector2 to)
        {
            foreach (var hit in Physics2D.LinecastAll(from, to))
                if (hit.collider && hit.collider.GetComponentInParent<CombatObstacle>()) return true;
            return false;
        }
        public static Vector2 EndAtObstacle(Vector2 start, Vector2 direction, float distance)
        {
            var end = start + direction * distance;
            float nearest = distance;
            foreach (var hit in Physics2D.RaycastAll(start, direction, distance))
                if (hit.collider && hit.collider.GetComponentInParent<CombatObstacle>() && hit.distance < nearest)
                { nearest = hit.distance; end = hit.point; }
            return end;
        }
    }
}
