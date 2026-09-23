using System.Collections.Generic;
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
            public NativeBossController Boss;
            public bool HasTarget => Interaction || Boss;
        }

        sealed class Entry
        {
            internal NativeInteraction interaction;
            internal NativeBossController boss;
            internal int order;
        }

        sealed class Instance
        {
            internal LevelInstanceContext context;
            internal NativeRunController level;
            internal readonly List<Entry> entries = new List<Entry>();
            internal int nextOrder;
        }

        readonly Dictionary<RuntimeScope, Instance> instances = new Dictionary<RuntimeScope, Instance>();
        RuntimeScope activeScope;
        bool hasActiveScope;
        public bool IntegratedMode { get; private set; }
        public bool HasActiveScope => hasActiveScope && instances.ContainsKey(activeScope);
        public RuntimeScope ActiveScope => HasActiveScope ? activeScope : default(RuntimeScope);

        public bool RegisterInstance(LevelInstanceContext context, NativeRunController level)
        {
            if (context == null || !level || !level.player || level.player.run != level ||
                context.Run.Player != level.player.transform || instances.ContainsKey(context.Scope)) return false;
            instances.Add(context.Scope, new Instance { context = context, level = level });
            IntegratedMode = true;
            return true;
        }

        public bool RegisterInteraction(RuntimeScope scope, NativeInteraction target)
        {
            Instance instance;
            if (!instances.TryGetValue(scope, out instance) || !target || target.run != instance.level ||
                Contains(instance, target, null)) return false;
            instance.entries.Add(new Entry { interaction = target, order = instance.nextOrder++ });
            return true;
        }

        public bool RegisterBoss(RuntimeScope scope, NativeBossController target)
        {
            Instance instance;
            if (!instances.TryGetValue(scope, out instance) || !target || target.level != instance.level ||
                Contains(instance, null, target)) return false;
            instance.entries.Add(new Entry { boss = target, order = instance.nextOrder++ });
            return true;
        }

        static bool Contains(Instance instance, NativeInteraction interaction, NativeBossController boss)
        {
            foreach (var entry in instance.entries)
                if (interaction && entry.interaction == interaction || boss && entry.boss == boss) return true;
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

        public void UnregisterInstance(RuntimeScope scope)
        {
            instances.Remove(scope);
            if (hasActiveScope && activeScope == scope) hasActiveScope = false;
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
            return HasActiveScope && instances.TryGetValue(activeScope, out instance) &&
                instance.level == level && level && level.Running && player && level.player == player &&
                player.run == level && instance.context.Run.IsRunning &&
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
            float bestDistance = float.PositiveInfinity;
            int bestPriority = int.MaxValue, bestOrder = int.MaxValue;
            foreach (var entry in instance.entries)
            {
                var interaction = entry.interaction;
                var boss = entry.boss;
                if (interaction)
                {
                    if (interaction.run != level || interaction.Used || !interaction.CanReach(player)) continue;
                }
                else if (boss)
                {
                    if (boss.level != level || !boss.gameObject.scene.isLoaded || !boss.CanInteractCore(player)) continue;
                }
                else continue;
                Vector2 point = interaction ? (Vector2)interaction.transform.position : boss.AimPoint;
                float distance = (point - (Vector2)player.transform.position).sqrMagnitude;
                int priority = boss ? 0 : 1; // Core wins an exact distance tie.
                if (distance > bestDistance || distance == bestDistance &&
                    (priority > bestPriority || priority == bestPriority && entry.order >= bestOrder)) continue;
                bestDistance = distance; bestPriority = priority; bestOrder = entry.order;
                result = new Selection { Interaction = interaction, Boss = boss };
            }
            return result;
        }

        public NativeBossController FindSupportBoss(NativeRunController level, NativePlayer player)
        {
            Instance instance;
            if (!TryGetActive(level, player, out instance)) return null;
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
    }
}
