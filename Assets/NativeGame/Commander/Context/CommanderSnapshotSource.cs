using System;
using System.Collections.Generic;
using Echo.LevelToolkit.Combat;
using Echo.LevelToolkit.Foundation;
using Echo.NativeGame.ToolkitIntegration.LevelHost;
using UnityEngine;

namespace Echo.NativeGame.Commander
{
    // Reads the live focused run. Nothing here grants support or changes game state.
    public sealed class CommanderSnapshotSource : ICommanderSnapshotSource
    {
        internal readonly struct FocusIdentity : IEquatable<FocusIdentity>
        {
            internal readonly NativeRunController Run;
            internal readonly NativeLevelHost Host;
            internal readonly RunId RunId;
            internal readonly RuntimeScope Scope;
            internal readonly bool Integrated;
            internal readonly CombatPlayer ToolkitPlayer;
            internal readonly NativePlayer NativePlayer;
            internal readonly NativeBossController NativeBoss;
            internal readonly CombatBoss ToolkitBoss;

            internal FocusIdentity(NativeRunController run, NativeLevelHost host,
                RuntimeScope scope, bool integrated, CombatPlayer toolkitPlayer,
                NativePlayer nativePlayer, NativeBossController nativeBoss, CombatBoss toolkitBoss)
            {
                Run = run;
                Host = host;
                RunId = host ? host.RunId : default;
                Scope = scope;
                Integrated = integrated;
                ToolkitPlayer = toolkitPlayer;
                NativePlayer = nativePlayer;
                NativeBoss = nativeBoss;
                ToolkitBoss = toolkitBoss;
            }

            public bool Equals(FocusIdentity other) => Run == other.Run &&
                Host == other.Host &&
                RunId.Equals(other.RunId) && Scope.Equals(other.Scope) &&
                Integrated == other.Integrated && ToolkitPlayer == other.ToolkitPlayer &&
                NativePlayer == other.NativePlayer && NativeBoss == other.NativeBoss &&
                ToolkitBoss == other.ToolkitBoss;
        }

        readonly NativeRunController run;
        readonly NativeCommanderSafeNode safeNode;
        readonly ICommanderSupportBridge support;
        readonly NativeLevelHost host;

        public CommanderSnapshotSource(NativeRunController run, NativeCommanderSafeNode safeNode,
            ICommanderSupportBridge support, NativeLevelHost host = null)
        {
            this.run = run;
            this.safeNode = safeNode;
            this.support = support;
            this.host = host ? host : run ? run.GetComponent<NativeLevelHost>() : null;
        }

        public bool TryCapture(Guid sessionId, out CommanderSnapshot snapshot, out string reason)
        {
            snapshot = null;
            reason = null;
            if (sessionId == Guid.Empty || !TryGetFocus(out var focus, out reason)) return false;
            if (!safeNode || safeNode.level != run || !safeNode.CanCompose(out reason)) return false;
            if (!run.quest || !run.narrative || !run.map || support == null)
            { reason = "当前游戏状态尚未准备好。"; return false; }

            var snapshotId = Guid.NewGuid();
            IReadOnlyList<CommanderSupportOption> available = support.GetAvailableOptions(sessionId, snapshotId);
            var options = new List<CommanderSupportOption>();
            var optionIds = new HashSet<string>(StringComparer.Ordinal);
            if (available != null)
                foreach (var option in available)
                    if (option.SnapshotId == snapshotId && !string.IsNullOrWhiteSpace(option.OptionId) &&
                        optionIds.Add(option.OptionId) && run.narrative.HasMemory(option.Memory) &&
                        (option.Kind == NativeSupportKind.Medical || option.Kind == NativeSupportKind.Weakpoint) &&
                        (option.Tier == NativeSupportTier.Limited || option.Tier == NativeSupportTier.Deep))
                        options.Add(option);

            if (!IsCurrent(focus)) { reason = "当前游戏焦点已变化，请重新发送。"; return false; }
            float health = focus.ToolkitPlayer ? focus.ToolkitPlayer.Health : focus.NativePlayer.Health;
            float maxHealth = focus.ToolkitPlayer ? focus.ToolkitPlayer.maxHealth : focus.NativePlayer.maxHealth;
            var memories = new List<string>();
            foreach (var memory in run.narrative.Memories)
                memories.Add(Limit(memory.Source, 80) + " / " + Limit(memory.Title, 120) +
                    (run.narrative.IsShared(memory.Kind) ? "（已授权共享）" : "（本局持有）"));
            var routes = KnownRoutes(focus);
            var facts = RecentFacts(run.narrative);
            string bossStage = focus.ToolkitBoss ? focus.ToolkitBoss.Stage.ToString() :
                focus.NativeBoss ? focus.NativeBoss.Stage.ToString() :
                run.quest.BossCleared ? "当前封锁已解除" :
                run.quest.BossStarted ? "当前战斗机体阶段未知" : null;
            snapshot = new CommanderSnapshot(sessionId, snapshotId, health, maxHealth,
                run.quest.ObjectiveText, memories.AsReadOnly(), routes.AsReadOnly(), bossStage,
                options.AsReadOnly(), facts.AsReadOnly());
            reason = null;
            return true;
        }

        internal bool TryCaptureIdentity(out FocusIdentity identity)
        {
            if (!TryGetFocus(out identity, out _)) return false;
            return safeNode && safeNode.level == run && safeNode.CanCompose(out _);
        }

        internal bool IsCurrent(FocusIdentity identity) =>
            TryCaptureIdentity(out var current) && current.Equals(identity);

        bool TryGetFocus(out FocusIdentity identity, out string reason)
        {
            identity = default;
            if (!run || !run.isActiveAndEnabled || !run.gameObject.scene.isLoaded ||
                !run.Running || !run.player || !run.player.Alive || !run.hud)
            { reason = "当前关卡状态不能发送自由通讯。"; return false; }
            var hud = run.hud;
            bool integrated = hud.IntegratedInput;
            if (integrated && !hud.ActiveInputReady)
            { reason = "当前关卡实例不可用。"; return false; }
            if (host && (!host.IsRunning || integrated && host.RunId != hud.ActiveInputScope.RunId))
            { reason = "当前关卡身份已变化。"; return false; }
            var toolkitPlayer = integrated ? hud.ActiveToolkitPlayer : null;
            var nativePlayer = toolkitPlayer ? null : run.player;
            if (toolkitPlayer && (!toolkitPlayer.isActiveAndEnabled || !toolkitPlayer.Alive ||
                !toolkitPlayer.World || !toolkitPlayer.World.IsRunning))
            { reason = "当前躯壳不可用。"; return false; }
            var nativeBoss = hud.CurrentSupportBoss();
            var toolkitBoss = integrated ? hud.CurrentCombatSupportBoss() : null;
            if (!integrated && !nativeBoss && run.rules)
            {
                var candidate = run.rules.bossComponent as NativeBossController;
                if (candidate && candidate.level == run && !candidate.IsDefeated &&
                    run.quest && run.quest.BossStarted) nativeBoss = candidate;
            }
            if (integrated && !toolkitBoss && host &&
                host.TryGet(hud.ActiveInputScope, out var instance) && instance.World &&
                instance.Placement && toolkitPlayer)
            {
                float bestDistance = float.PositiveInfinity;
                foreach (var candidate in instance.Placement.GetComponentsInChildren<CombatBoss>())
                {
                    if (!candidate || candidate.World != instance.World ||
                        !candidate.IsEncounterActive || candidate.IsDefeated) continue;
                    float distance = ((Vector2)candidate.transform.position -
                        (Vector2)toolkitPlayer.transform.position).sqrMagnitude;
                    if (distance >= bestDistance) continue;
                    bestDistance = distance;
                    toolkitBoss = candidate;
                }
            }
            identity = new FocusIdentity(run, host, integrated ? hud.ActiveInputScope : default,
                integrated, toolkitPlayer, nativePlayer, nativeBoss, toolkitBoss);
            reason = null;
            return true;
        }

        List<string> KnownRoutes(FocusIdentity focus)
        {
            var routes = new List<string>();
            var map = run.map;
            var quest = run.quest;
            if (map.northRouteEntry && map.northRouteDoor && map.northRouteChunks != null &&
                map.northRouteChunks.Length > 0)
            {
                Vector2 position = focus.ToolkitPlayer ? (Vector2)focus.ToolkitPlayer.transform.position :
                    (Vector2)focus.NativePlayer.transform.position;
                bool discovered = map.NorthRouteOpen || run.narrative.TookBypass ||
                    Vector2.Distance(position, map.northRouteEntry.position) <= 8f;
                if (discovered) routes.Add("北侧检修通道：" +
                    (map.NorthRouteOpen || !map.northRouteDoor.activeInHierarchy ? "可通行" : "门锁未解除"));
            }
            if (map.arenaEntryGate && (quest.RelayRestored || quest.BossStarted || quest.BossCleared))
                routes.Add("东侧通路：" + (map.ArenaLocked ? "战斗封锁中" : "可通行"));
            if (map.finalGate && quest.BossCleared)
                routes.Add("最终节点通路：" + (map.FinalOpen ? "可通行" : "尚未开放"));
            return routes;
        }

        static List<string> RecentFacts(NativeNarrative narrative)
        {
            var facts = new List<string>();
            string summary = narrative.BehaviorSummary();
            if (string.IsNullOrEmpty(summary)) return facts;
            var lines = summary.Split('\n');
            for (int i = Math.Max(0, lines.Length - 6); i < lines.Length; i++)
            {
                string fact = lines[i].Trim().TrimStart('-', ' ');
                if (fact.Length > 240) fact = fact.Substring(0, 240);
                if (fact.Length > 0) facts.Add(fact);
            }
            return facts;
        }

        static string Limit(string text, int max) =>
            string.IsNullOrEmpty(text) ? string.Empty : text.Length <= max ? text : text.Substring(0, max);
    }
}
