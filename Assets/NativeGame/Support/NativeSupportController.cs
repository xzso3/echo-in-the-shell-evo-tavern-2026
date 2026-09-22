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
        public string StatusText { get; private set; } = "Select support, an authorization tier and an owned memory. Nothing is shared until confirmation succeeds.";
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
            if (reason != null) { StatusText = "UNAVAILABLE / " + reason + " No authorization or synchronization cost."; return false; }
            pendingKind = kind; pendingTier = tier; pendingMemory = sharedMemory;
            pendingLevel = level; pendingPlayer = level.player; pendingNarrative = level.narrative; pendingBoss = boss;
            HasPending = true;
            string memory = sharedMemory.ToString();
            foreach (var entry in level.narrative.Memories)
                if (entry.Kind == sharedMemory) { memory = entry.Source + " / " + entry.Title; break; }
            string benefit = kind == NativeSupportKind.Medical
                ? "Restore up to 40 health, capped at maximum (currently " + Mathf.Min(MedicalHealing, level.player.maxHealth - level.player.Health).ToString("0.#") + ")."
                : "Add 3 seconds to the current core window if open, and every future core window in this encounter. Does not break the shell or finish the Boss.";
            StatusText = "PENDING / " + kind + "\n" + benefit + "\nSelected memory: " + memory +
                "\nAuthorization: " + tier + (tier == NativeSupportTier.Limited ? " / limited access to the selected memory." : " / deep access to the selected memory.") +
                " No other memories are selected.\nSynchronization: +" + Delta(tier) +
                " only after a successful effect.\nConfirm with Enter or the confirmation button; cancel incurs no cost.";
            return true;
        }
        public bool Confirm()
        {
            if (!HasPending) return false;
            string reason = level != pendingLevel || !level || level.player != pendingPlayer || level.narrative != pendingNarrative ||
                (pendingKind == NativeSupportKind.Weakpoint && boss != pendingBoss)
                ? "The scene bindings changed. Request a new contract." : Unavailable(pendingKind, pendingTier, pendingMemory);
            if (reason != null) { ClearPending(); StatusText = "FAILED / " + reason + " No authorization or synchronization cost."; return false; }
            var authorization = new NativeSupportAuthorization(pendingKind, pendingTier, pendingMemory, Delta(pendingTier));
            float healthBefore = level.player.Health;
            bool applied = pendingKind == NativeSupportKind.Medical ? level.player.TryHeal(MedicalHealing) : boss.TryEnableWeakpointSupport();
            if (!applied) { ClearPending(); StatusText = "FAILED / The effect is no longer available. No authorization or synchronization cost."; return false; }
            if (pendingKind == NativeSupportKind.Medical) medicalUsed = true; else weakpointUsed = true;
            // Commit before notifying subscribers: repeated/reentrant confirmation cannot apply or charge twice.
            ClearPending();
            StatusText = "AUTHORIZED / " + authorization.Kind + " / " + authorization.Tier + "\nShared memory: " + authorization.SharedMemory +
                "\n" + (authorization.Kind == NativeSupportKind.Medical ? "Restored " + (level.player.Health - healthBefore).ToString("0.#") + " health." : "Core windows extended by 3 seconds for this encounter.") +
                "\nSynchronization: +" + authorization.SyncDelta + ". This support is now used for this run.";
            Authorized?.Invoke(authorization);
            return true;
        }
        public void Cancel()
        {
            if (!HasPending) return;
            ClearPending(); StatusText = "CANCELLED / No effect, memory authorization or synchronization cost.";
        }
        void OnDisable() { Cancel(); }
        void ClearPending()
        {
            HasPending = false; pendingLevel = null; pendingPlayer = null; pendingNarrative = null; pendingBoss = null;
        }
        static int Delta(NativeSupportTier tier) => tier == NativeSupportTier.Limited ? 20 : 45;
        string Unavailable(NativeSupportKind kind, NativeSupportTier tier, NativeMemoryKind memory)
        {
            if (kind != NativeSupportKind.Medical && kind != NativeSupportKind.Weakpoint) return "Unknown support kind.";
            if (tier != NativeSupportTier.Limited && tier != NativeSupportTier.Deep) return "Unknown authorization tier.";
            if (!isActiveAndEnabled || !level || !level.isActiveAndEnabled || !level.gameObject.scene.isLoaded || gameObject.scene != level.gameObject.scene || !level.Running)
                return "The run is not active.";
            var player = level.player;
            if (!player || !player.isActiveAndEnabled || !player.Alive || player.run != level || player.gameObject.scene != level.gameObject.scene)
                return "The current player is unavailable.";
            if (!level.narrative || level.narrative.gameObject.scene != level.gameObject.scene || !level.narrative.HasMemory(memory))
                return "The selected memory has not been recovered in this run.";
            if (kind == NativeSupportKind.Medical)
            {
                if (medicalUsed) return "Medical support was already used this run.";
                if (player.Health >= player.maxHealth) return "Health is already full.";
            }
            else
            {
                if (weakpointUsed) return "Weakpoint support was already used this run.";
                if (!boss || boss.level != level || boss.gameObject.scene != level.gameObject.scene || !boss.CanEnableWeakpointSupport)
                    return "The Boss must be active, undefeated, and not already enhanced by weakpoint support.";
            }
            return null;
        }
    }
}
