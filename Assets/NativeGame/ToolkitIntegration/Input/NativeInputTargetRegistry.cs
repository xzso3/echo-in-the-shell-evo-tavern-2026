using System.Collections.Generic;
using Echo.LevelToolkit.Combat;
using Echo.LevelToolkit.Foundation;
using UnityEngine;

namespace Echo.NativeGame
{
    // One explicit registration per mounted level. The host changes ActiveScope at a
    // crossing; a nearby object in another mounted instance is never an input target.
    public sealed class NativeInputTargetRegistry
    {
        public struct Selection
        {
            public NativeInteraction Interaction;
            public NativeScopedInteraction ScopedInteraction;
            public NativeBossController Boss;
            public CombatBoss CombatBoss;
            public bool HasTarget => Interaction || ScopedInteraction || Boss || CombatBoss;
        }

        sealed class Entry
        {
            internal NativeInteraction interaction;
            internal NativeScopedInteraction scopedInteraction;
            internal NativeBossController boss;
            internal CombatBoss combatBoss;
            internal int order;
        }

        sealed class Instance
        {
            internal LevelInstanceContext context;
            internal NativeRunController level;
            internal CombatPlayer combatPlayer;
            internal CombatWeapon combatWeapon;
            internal Transform ownerRoot;
            internal readonly List<Entry> entries = new List<Entry>();
            internal int nextOrder;
        }

        readonly Dictionary<RuntimeScope, Instance> instances = new Dictionary<RuntimeScope, Instance>();
        RuntimeScope activeScope;
        bool hasActiveScope;
        public bool IntegratedMode { get; private set; }
        public bool HasActiveScope => hasActiveScope && instances.ContainsKey(activeScope);
        public RuntimeScope ActiveScope => HasActiveScope ? activeScope : default(RuntimeScope);
        public CombatPlayer ActiveCombatPlayer => HasActiveScope ? instances[activeScope].combatPlayer : null;
        public CombatWeapon ActiveCombatWeapon => HasActiveScope ? instances[activeScope].combatWeapon : null;

        public bool RegisterInstance(LevelInstanceContext context, NativeRunController level)
        {
            if (context == null || !level || !level.player || level.player.run != level ||
                context.Run.Player != level.player.transform || instances.ContainsKey(context.Scope)) return false;
            instances.Add(context.Scope, new Instance { context = context, level = level });
            IntegratedMode = true;
            return true;
        }

        public bool RegisterToolkitInstance(LevelInstanceContext context, NativeRunController level,
            CombatPlayer player, CombatWeapon weapon, Transform ownerRoot)
        {
            if (context == null || !level || !player || !player.World || !ownerRoot ||
                !player.transform.IsChildOf(ownerRoot) ||
                player.World.Context != context || context.Run.Player != player.transform ||
                weapon && weapon.World != player.World || instances.ContainsKey(context.Scope)) return false;
            instances.Add(context.Scope, new Instance
            { context = context, level = level, combatPlayer = player, combatWeapon = weapon,
                ownerRoot = ownerRoot });
            IntegratedMode = true;
            return true;
        }

        public bool RegisterInteraction(RuntimeScope scope, NativeInteraction target)
        {
            Instance instance;
            if (!instances.TryGetValue(scope, out instance) || !target || target.run != instance.level ||
                instance.combatPlayer || Contains(instance, target, null)) return false;
            instance.entries.Add(new Entry { interaction = target, order = instance.nextOrder++ });
            return true;
        }

        public bool RegisterBoss(RuntimeScope scope, NativeBossController target)
        {
            Instance instance;
            if (!instances.TryGetValue(scope, out instance) || !target || target.level != instance.level ||
                instance.combatPlayer || Contains(instance, null, target)) return false;
            instance.entries.Add(new Entry { boss = target, order = instance.nextOrder++ });
            return true;
        }

        public bool RegisterInteraction(RuntimeScope scope, NativeScopedInteraction target)
        {
            Instance instance;
            if (!instances.TryGetValue(scope, out instance) || !instance.combatPlayer || !target ||
                !instance.ownerRoot || !target.transform.IsChildOf(instance.ownerRoot) ||
                Contains(instance, target, null)) return false;
            instance.entries.Add(new Entry { scopedInteraction = target, order = instance.nextOrder++ });
            return true;
        }

        public bool RegisterBoss(RuntimeScope scope, CombatBoss target)
        {
            Instance instance;
            if (!instances.TryGetValue(scope, out instance) || !instance.combatPlayer || !target ||
                target.World != instance.combatPlayer.World || Contains(instance, null, target)) return false;
            instance.entries.Add(new Entry { combatBoss = target, order = instance.nextOrder++ });
            return true;
        }

        static bool Contains(Instance instance, NativeInteraction interaction, NativeBossController boss)
        {
            foreach (var entry in instance.entries)
                if (interaction && entry.interaction == interaction || boss && entry.boss == boss) return true;
            return false;
        }
        static bool Contains(Instance instance, NativeScopedInteraction interaction, CombatBoss boss)
        {
            foreach (var entry in instance.entries)
                if (interaction && entry.scopedInteraction == interaction || boss && entry.combatBoss == boss) return true;
            return false;
        }

        public void UnregisterInteraction(RuntimeScope scope, NativeInteraction target)
        {
            Instance instance;
            if (instances.TryGetValue(scope, out instance))
                instance.entries.RemoveAll(entry => entry.interaction == target);
        }

        public void UnregisterBoss(RuntimeScope scope, NativeBossController target)
        {
            Instance instance;
            if (instances.TryGetValue(scope, out instance))
                instance.entries.RemoveAll(entry => entry.boss == target);
        }

        public void UnregisterInteraction(RuntimeScope scope, NativeScopedInteraction target)
        {
            Instance instance;
            if (instances.TryGetValue(scope, out instance))
                instance.entries.RemoveAll(entry => entry.scopedInteraction == target);
        }

        public void UnregisterBoss(RuntimeScope scope, CombatBoss target)
        {
            Instance instance;
            if (instances.TryGetValue(scope, out instance))
                instance.entries.RemoveAll(entry => entry.combatBoss == target);
        }

        public void UnregisterInstance(RuntimeScope scope)
        {
            instances.Remove(scope);
            if (hasActiveScope && activeScope == scope) hasActiveScope = false;
            if (instances.Count == 0) IntegratedMode = false;
        }

        public bool SetActiveInstance(RuntimeScope scope)
        {
            if (!instances.ContainsKey(scope)) return false;
            activeScope = scope; hasActiveScope = true;
            return true;
        }

        public void ClearActiveInstance() { hasActiveScope = false; }

        bool TryGetActive(NativeRunController level, NativePlayer player, out Instance instance)
        {
            instance = null;
            if (!HasActiveScope || !instances.TryGetValue(activeScope, out instance) ||
                instance.level != level || !level || !level.Running || !instance.context.Run.IsRunning)
                return false;
            if (instance.combatPlayer)
                return instance.combatPlayer.isActiveAndEnabled && instance.combatPlayer.Alive &&
                    instance.combatPlayer.World && instance.combatPlayer.World.IsRunning &&
                    instance.context.Run.Player == instance.combatPlayer.transform;
            return player && level.player == player && player.run == level &&
                instance.context.Run.Player == player.transform;
        }

        public bool IsActiveRunReady(NativeRunController level, NativePlayer player)
        {
            Instance instance;
            return TryGetActive(level, player, out instance);
        }

        public Selection FindTarget(NativeRunController level, NativePlayer player)
        {
            Instance instance;
            if (!TryGetActive(level, player, out instance)) return default(Selection);
            var result = default(Selection);
            Vector2 playerPosition = instance.combatPlayer ?
                (Vector2)instance.combatPlayer.transform.position : (Vector2)player.transform.position;
            float bestDistance = float.PositiveInfinity;
            int bestPriority = int.MaxValue, bestOrder = int.MaxValue;
            foreach (var entry in instance.entries)
            {
                var interaction = entry.interaction;
                var scopedInteraction = entry.scopedInteraction;
                var boss = entry.boss;
                var combatBoss = entry.combatBoss;
                if (interaction)
                {
                    if (interaction.run != level || interaction.Used || !interaction.CanReach(player)) continue;
                }
                else if (scopedInteraction)
                {
                    if (!instance.combatPlayer || !scopedInteraction.CanReach(instance.combatPlayer)) continue;
                }
                else if (boss)
                {
                    if (boss.level != level || !boss.gameObject.scene.isLoaded || !boss.CanInteractCore(player)) continue;
                }
                else if (combatBoss)
                {
                    if (!instance.combatPlayer || combatBoss.World != instance.combatPlayer.World ||
                        !combatBoss.gameObject.scene.isLoaded || !combatBoss.CanInteractCore(instance.combatPlayer)) continue;
                }
                else continue;
                Vector2 point = interaction ? (Vector2)interaction.transform.position : scopedInteraction ?
                    (Vector2)scopedInteraction.transform.position : boss ? boss.AimPoint : combatBoss.AimPoint;
                float distance = (point - playerPosition).sqrMagnitude;
                int priority = boss || combatBoss ? 0 : 1; // Core wins an exact distance tie.
                if (distance > bestDistance || distance == bestDistance &&
                    (priority > bestPriority || priority == bestPriority && entry.order >= bestOrder)) continue;
                bestDistance = distance; bestPriority = priority; bestOrder = entry.order;
                result = new Selection { Interaction = interaction, ScopedInteraction = scopedInteraction,
                    Boss = boss, CombatBoss = combatBoss };
            }
            return result;
        }

        public NativeBossController FindSupportBoss(NativeRunController level, NativePlayer player)
        {
            Instance instance;
            if (!TryGetActive(level, player, out instance) || instance.combatPlayer) return null;
            NativeBossController best = null;
            float bestDistance = float.PositiveInfinity;
            int bestOrder = int.MaxValue;
            foreach (var entry in instance.entries)
            {
                var candidate = entry.boss;
                if (!candidate || candidate.level != level || !candidate.gameObject.scene.isLoaded ||
                    !candidate.CanEnableWeakpointSupport) continue;
                float distance = ((Vector2)candidate.AimPoint - (Vector2)player.transform.position).sqrMagnitude;
                if (distance > bestDistance || distance == bestDistance && entry.order >= bestOrder) continue;
                best = candidate; bestDistance = distance; bestOrder = entry.order;
            }
            return best;
        }

        public CombatBoss FindCombatSupportBoss(NativeRunController level, NativePlayer player)
        {
            Instance instance;
            if (!TryGetActive(level, player, out instance) || !instance.combatPlayer) return null;
            CombatBoss best = null;
            float bestDistance = float.PositiveInfinity;
            int bestOrder = int.MaxValue;
            foreach (var entry in instance.entries)
            {
                var candidate = entry.combatBoss;
                if (!candidate || candidate.World != instance.combatPlayer.World ||
                    !candidate.gameObject.scene.isLoaded || !candidate.CanEnableWeakpointSupport) continue;
                float distance = (candidate.AimPoint - (Vector2)instance.combatPlayer.transform.position).sqrMagnitude;
                if (distance > bestDistance || distance == bestDistance && entry.order >= bestOrder) continue;
                best = candidate; bestDistance = distance; bestOrder = entry.order;
            }
            return best;
        }
    }
}
