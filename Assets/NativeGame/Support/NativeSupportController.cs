using System;
using Echo.LevelToolkit.Combat;
using Echo.LevelToolkit.Foundation;
using UnityEngine;

namespace Echo.NativeGame
{
    // Local, scene-owned support. UI requests/confirms; ECA records successful authorization.
    public sealed class NativeSupportController : MonoBehaviour, INativeSupport
    {
        public NativeRunController level;
        public NativeBossController boss;
        public const float MedicalHealing = 40;
        public event Action<NativeSupportAuthorization> Authorized;
        public bool HasPending { get; private set; }
        public string StatusText { get; private set; } = "请选择支援、授权范围和一段已找回的记忆。确认并成功执行前，不会共享任何记忆。";
        bool medicalUsed, weakpointUsed;
        NativeSupportKind pendingKind;
        NativeSupportTier pendingTier;
        NativeMemoryKind pendingMemory;
        NativeRunController pendingLevel;
        NativePlayer pendingPlayer;
        NativeNarrative pendingNarrative;
        NativeBossController pendingBoss;
        CombatBoss pendingCombatBoss;
        CombatPlayer pendingCombatPlayer;
        RuntimeScope pendingScope;
        bool pendingIntegrated;
        RunId usageRunId;
        bool hasUsageRunId;

        public bool Request(NativeSupportKind kind, NativeSupportTier tier, NativeMemoryKind sharedMemory)
        {
            if (HasPending) return false; // Keep the visible contract intact until confirm/cancel.
            SyncUsageRun();
            var target = CurrentBoss();
            var combatTarget = CurrentCombatBoss();
            string reason = Unavailable(kind, tier, sharedMemory, target, combatTarget);
            if (reason != null) { StatusText = "暂不可用 / " + reason + " 未授予权限，也未增加同步度。"; return false; }
            pendingKind = kind; pendingTier = tier; pendingMemory = sharedMemory;
            pendingLevel = level; pendingPlayer = level.player; pendingNarrative = level.narrative;
            pendingBoss = kind == NativeSupportKind.Weakpoint ? target : null;
            pendingCombatBoss = kind == NativeSupportKind.Weakpoint ? combatTarget : null;
            pendingCombatPlayer = level.hud ? level.hud.ActiveToolkitPlayer : null;
            pendingIntegrated = level.hud && level.hud.IntegratedInput;
            pendingScope = pendingIntegrated ? level.hud.ActiveInputScope : default(RuntimeScope);
            HasPending = true;
            string memory = NativeMemoryNode.KindLabel(sharedMemory);
            foreach (var entry in level.narrative.Memories)
                if (entry.Kind == sharedMemory) { memory = entry.Source + " / " + entry.Title; break; }
            string benefit = kind == NativeSupportKind.Medical
                ? "最多恢复40点生命，不超过生命上限。本次可恢复" + Mathf.Min(MedicalHealing,
                    pendingCombatPlayer ? pendingCombatPlayer.maxHealth - pendingCombatPlayer.Health :
                    level.player.maxHealth - level.player.Health).ToString("0.#") + "点。"
                : "本次战斗中，之后每次核心暴露时间增加3秒；如果核心已暴露，当前窗口也会延长3秒。不会直接击破外壳或完成战斗。";
            StatusText = "待确认 / " + KindLabel(kind) + "\n" + benefit + "\n所选记忆：" + memory +
                (pendingBoss ? "\n支援目标：" + pendingBoss.name :
                    pendingCombatBoss ? "\n支援目标：" + pendingCombatBoss.name : "") +
                "\n授权范围：" + TierLabel(tier) + (tier == NativeSupportTier.Limited ? " / 允许读取所选记忆。" : " / 允许共同改写对所选记忆的理解。") +
                " 不涉及其他记忆。\n同步度：+" + Delta(tier) +
                "，仅在支援成功生效后增加。\n按 Enter 或“接受并执行”确认；拒绝不产生代价。";
            return true;
        }
        public bool Confirm()
        {
            if (!HasPending) return false;
            string reason = PendingTargetProblem();
            if (reason == null) reason = Unavailable(pendingKind, pendingTier, pendingMemory, pendingBoss, pendingCombatBoss);
            if (reason != null) { ClearPending(); StatusText = "执行失败 / " + reason + " 未授予权限，也未增加同步度。"; return false; }
            var authorization = new NativeSupportAuthorization(pendingKind, pendingTier, pendingMemory, Delta(pendingTier));
            float healthBefore = pendingCombatPlayer ? pendingCombatPlayer.Health : level.player.Health;
            bool applied = pendingKind == NativeSupportKind.Medical
                ? (pendingCombatPlayer ? pendingCombatPlayer.TryHeal(MedicalHealing) : level.player.TryHeal(MedicalHealing))
                : (pendingCombatBoss ? pendingCombatBoss.TryEnableWeakpointSupport() : pendingBoss.TryEnableWeakpointSupport());
            if (!applied) { ClearPending(); StatusText = "执行失败 / 当前无法应用此效果。未授予权限，也未增加同步度。"; return false; }
            float healedAmount = (pendingCombatPlayer ? pendingCombatPlayer.Health : level.player.Health) - healthBefore;
            if (pendingKind == NativeSupportKind.Medical) medicalUsed = true; else weakpointUsed = true;
            // Commit before notifying subscribers: repeated/reentrant confirmation cannot apply or charge twice.
            ClearPending();
            StatusText = "已执行 / " + KindLabel(authorization.Kind) + " / " + TierLabel(authorization.Tier) + "\n已共享记忆：" + NativeMemoryNode.KindLabel(authorization.SharedMemory) +
                "\n" + (authorization.Kind == NativeSupportKind.Medical ? "已恢复" + healedAmount.ToString("0.#") + "点生命。" : "本次战斗的核心暴露时间已延长3秒。") +
                "\n同步度：+" + authorization.SyncDelta + "。本局已使用此项支援。";
            Authorized?.Invoke(authorization);
            return true;
        }
        public void Cancel()
        {
            if (!HasPending) return;
            ClearPending(); StatusText = "已拒绝 / 未执行支援，未授权记忆，也未增加同步度。";
        }
        void Update()
        {
            if (!HasPending) return;
            string reason = PendingTargetProblem();
            if (reason == null) return;
            ClearPending();
            StatusText = "执行失败 / " + reason + " 未授予权限，也未增加同步度。";
        }
        void OnDisable() { Cancel(); }
        void ClearPending()
        {
            HasPending = false; pendingLevel = null; pendingPlayer = null; pendingNarrative = null;
            pendingBoss = null; pendingCombatBoss = null; pendingCombatPlayer = null;
            pendingScope = default(RuntimeScope); pendingIntegrated = false;
        }
        public static string KindLabel(NativeSupportKind value) => value == NativeSupportKind.Medical ? "医疗支援" : value == NativeSupportKind.Weakpoint ? "弱点解析" : "未知支援";
        public static string TierLabel(NativeSupportTier value) => value == NativeSupportTier.Limited ? "有限授权" : value == NativeSupportTier.Deep ? "深度授权" : "未知授权";
        static int Delta(NativeSupportTier tier) => tier == NativeSupportTier.Limited ? 20 : 45;
        NativeBossController CurrentBoss()
        {
            if (level && level.hud && level.hud.IntegratedInput) return level.hud.CurrentSupportBoss();
            return boss;
        }
        CombatBoss CurrentCombatBoss()
        {
            return level && level.hud && level.hud.IntegratedInput ? level.hud.CurrentCombatSupportBoss() : null;
        }
        void SyncUsageRun()
        {
            if (!level || !level.hud || !level.hud.IntegratedInput || !level.hud.HasActiveInputScope) return;
            var current = level.hud.ActiveInputScope.RunId;
            if (!hasUsageRunId || usageRunId != current)
            {
                medicalUsed = false; weakpointUsed = false;
                usageRunId = current; hasUsageRunId = true;
            }
        }
        string PendingTargetProblem()
        {
            if (!level || level != pendingLevel || !level.Running || level.player != pendingPlayer ||
                level.narrative != pendingNarrative) return "当前支援对象已变化，请重新查看合同。";
            bool integrated = level.hud && level.hud.IntegratedInput;
            if (integrated != pendingIntegrated) return "当前支援对象已变化，请重新查看合同。";
            if (integrated && (!level.hud.ActiveInputReady || level.hud.ActiveInputScope != pendingScope))
                return "当前关卡实例已变化，请重新查看合同。";
            if (integrated && level.hud.ActiveToolkitPlayer != pendingCombatPlayer)
                return "当前躯壳已变化，请重新查看合同。";
            if (pendingKind == NativeSupportKind.Weakpoint &&
                (pendingCombatBoss
                    ? CurrentCombatBoss() != pendingCombatBoss || !pendingCombatBoss.CanEnableWeakpointSupport
                    : !pendingBoss || CurrentBoss() != pendingBoss || !pendingBoss.CanEnableWeakpointSupport))
                return "战斗机体目标已变化或失效，请重新查看合同。";
            return null;
        }
        string Unavailable(NativeSupportKind kind, NativeSupportTier tier, NativeMemoryKind memory,
            NativeBossController target, CombatBoss combatTarget)
        {
            if (kind != NativeSupportKind.Medical && kind != NativeSupportKind.Weakpoint) return "无法识别此项支援。";
            if (tier != NativeSupportTier.Limited && tier != NativeSupportTier.Deep) return "无法识别此项授权。";
            if (!isActiveAndEnabled || !level || !level.isActiveAndEnabled || !level.gameObject.scene.isLoaded || gameObject.scene != level.gameObject.scene || !level.Running)
                return "当前无法使用支援。";
            if (level.hud && level.hud.IntegratedInput && !level.hud.ActiveInputReady)
                return "当前关卡实例不可用。";
            var player = level.player;
            var combatPlayer = level.hud ? level.hud.ActiveToolkitPlayer : null;
            if (combatPlayer)
            {
                if (!combatPlayer.isActiveAndEnabled || !combatPlayer.Alive || !combatPlayer.World ||
                    !combatPlayer.World.IsRunning) return "躯壳当前无法接受支援。";
            }
            else if (!player || !player.isActiveAndEnabled || !player.Alive || player.run != level ||
                player.gameObject.scene != level.gameObject.scene) return "躯壳当前无法接受支援。";
            if (!level.narrative || level.narrative.gameObject.scene != level.gameObject.scene || !level.narrative.HasMemory(memory))
                return "本局尚未找回所选记忆。";
            if (kind == NativeSupportKind.Medical)
            {
                if (medicalUsed) return "本局已使用医疗支援。";
                if (combatPlayer ? combatPlayer.Health >= combatPlayer.maxHealth : player.Health >= player.maxHealth)
                    return "生命已满，无需治疗。";
            }
            else
            {
                if (weakpointUsed) return "本局已使用弱点解析。";
                bool valid = combatPlayer
                    ? combatTarget && combatTarget.World == combatPlayer.World && combatTarget.gameObject.scene.isLoaded && combatTarget.CanEnableWeakpointSupport
                    : target && target.level == level && target.gameObject.scene.isLoaded && target.CanEnableWeakpointSupport;
                if (!valid)
                    return "请在战斗机体启动后、被击败前使用，且本局尚未使用弱点解析。";
            }
            return null;
        }
    }
}
