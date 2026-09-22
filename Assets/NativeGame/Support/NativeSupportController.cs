using System;
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

        public bool Request(NativeSupportKind kind, NativeSupportTier tier, NativeMemoryKind sharedMemory)
        {
            if (HasPending) return false; // Keep the visible contract intact until confirm/cancel.
            string reason = Unavailable(kind, tier, sharedMemory);
            if (reason != null) { StatusText = "暂不可用 / " + reason + " 未授予权限，也未增加同步度。"; return false; }
            pendingKind = kind; pendingTier = tier; pendingMemory = sharedMemory;
            pendingLevel = level; pendingPlayer = level.player; pendingNarrative = level.narrative; pendingBoss = boss;
            HasPending = true;
            string memory = NativeMemoryNode.KindLabel(sharedMemory);
            foreach (var entry in level.narrative.Memories)
                if (entry.Kind == sharedMemory) { memory = entry.Source + " / " + entry.Title; break; }
            string benefit = kind == NativeSupportKind.Medical
                ? "最多恢复40点生命，不超过生命上限。本次可恢复" + Mathf.Min(MedicalHealing, level.player.maxHealth - level.player.Health).ToString("0.#") + "点。"
                : "本次战斗中，之后每次核心暴露时间增加3秒；如果核心已暴露，当前窗口也会延长3秒。不会直接击破外壳或完成战斗。";
            StatusText = "待确认 / " + KindLabel(kind) + "\n" + benefit + "\n所选记忆：" + memory +
                "\n授权范围：" + TierLabel(tier) + (tier == NativeSupportTier.Limited ? " / 允许读取所选记忆。" : " / 允许共同改写对所选记忆的理解。") +
                " 不涉及其他记忆。\n同步度：+" + Delta(tier) +
                "，仅在支援成功生效后增加。\n按 Enter 或“接受并执行”确认；拒绝不产生代价。";
            return true;
        }
        public bool Confirm()
        {
            if (!HasPending) return false;
            string reason = level != pendingLevel || !level || level.player != pendingPlayer || level.narrative != pendingNarrative ||
                (pendingKind == NativeSupportKind.Weakpoint && boss != pendingBoss)
                ? "当前支援对象已变化，请重新查看合同。" : Unavailable(pendingKind, pendingTier, pendingMemory);
            if (reason != null) { ClearPending(); StatusText = "执行失败 / " + reason + " 未授予权限，也未增加同步度。"; return false; }
            var authorization = new NativeSupportAuthorization(pendingKind, pendingTier, pendingMemory, Delta(pendingTier));
            float healthBefore = level.player.Health;
            bool applied = pendingKind == NativeSupportKind.Medical ? level.player.TryHeal(MedicalHealing) : boss.TryEnableWeakpointSupport();
            if (!applied) { ClearPending(); StatusText = "执行失败 / 当前无法应用此效果。未授予权限，也未增加同步度。"; return false; }
            if (pendingKind == NativeSupportKind.Medical) medicalUsed = true; else weakpointUsed = true;
            // Commit before notifying subscribers: repeated/reentrant confirmation cannot apply or charge twice.
            ClearPending();
            StatusText = "已执行 / " + KindLabel(authorization.Kind) + " / " + TierLabel(authorization.Tier) + "\n已共享记忆：" + NativeMemoryNode.KindLabel(authorization.SharedMemory) +
                "\n" + (authorization.Kind == NativeSupportKind.Medical ? "已恢复" + (level.player.Health - healthBefore).ToString("0.#") + "点生命。" : "本次战斗的核心暴露时间已延长3秒。") +
                "\n同步度：+" + authorization.SyncDelta + "。本局已使用此项支援。";
            Authorized?.Invoke(authorization);
            return true;
        }
        public void Cancel()
        {
            if (!HasPending) return;
            ClearPending(); StatusText = "已拒绝 / 未执行支援，未授权记忆，也未增加同步度。";
        }
        void OnDisable() { Cancel(); }
        void ClearPending()
        {
            HasPending = false; pendingLevel = null; pendingPlayer = null; pendingNarrative = null; pendingBoss = null;
        }
        public static string KindLabel(NativeSupportKind value) => value == NativeSupportKind.Medical ? "医疗支援" : value == NativeSupportKind.Weakpoint ? "弱点解析" : "未知支援";
        public static string TierLabel(NativeSupportTier value) => value == NativeSupportTier.Limited ? "有限授权" : value == NativeSupportTier.Deep ? "深度授权" : "未知授权";
        static int Delta(NativeSupportTier tier) => tier == NativeSupportTier.Limited ? 20 : 45;
        string Unavailable(NativeSupportKind kind, NativeSupportTier tier, NativeMemoryKind memory)
        {
            if (kind != NativeSupportKind.Medical && kind != NativeSupportKind.Weakpoint) return "无法识别此项支援。";
            if (tier != NativeSupportTier.Limited && tier != NativeSupportTier.Deep) return "无法识别此项授权。";
            if (!isActiveAndEnabled || !level || !level.isActiveAndEnabled || !level.gameObject.scene.isLoaded || gameObject.scene != level.gameObject.scene || !level.Running)
                return "当前无法使用支援。";
            var player = level.player;
            if (!player || !player.isActiveAndEnabled || !player.Alive || player.run != level || player.gameObject.scene != level.gameObject.scene)
                return "躯壳当前无法接受支援。";
            if (!level.narrative || level.narrative.gameObject.scene != level.gameObject.scene || !level.narrative.HasMemory(memory))
                return "本局尚未找回所选记忆。";
            if (kind == NativeSupportKind.Medical)
            {
                if (medicalUsed) return "本局已使用医疗支援。";
                if (player.Health >= player.maxHealth) return "生命已满，无需治疗。";
            }
            else
            {
                if (weakpointUsed) return "本局已使用弱点解析。";
                if (!boss || boss.level != level || boss.gameObject.scene != level.gameObject.scene || !boss.CanEnableWeakpointSupport)
                    return "请在战斗机体启动后、被击败前使用，且本局尚未使用弱点解析。";
            }
            return null;
        }
    }
}
