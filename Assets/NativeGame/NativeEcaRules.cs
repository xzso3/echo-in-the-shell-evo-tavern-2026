using UnityEngine;
namespace Echo.NativeGame
{
    // Concrete rules of this level: events -> read-only conditions -> actions on owning Unity components.
    public sealed class NativeEcaRules : MonoBehaviour
    {
        [Header("Event sources")]
        public NativeRunController level;
        public NativeInteraction terminal, exit;
        public NativeMemoryNode[] memoryNodes = new NativeMemoryNode[0];
        public NativeRegion[] regions = new NativeRegion[0];
        [Tooltip("Assign the real NativeBossController implementing INativeBossEncounter. No placeholder victory.")]
        public MonoBehaviour bossComponent;
        public MonoBehaviour supportComponent;
        INativeSupport support;
        public INativeSupport Support => support;
        [Header("Conditions and actions")]
        public NativeQuest quest;
        public NativeMap map;
        public NativeDialogue dialogue;
        public NativeNarrative narrative;
        [TextArea] public string terminalMessage = "COMMANDER / Three sources, none erased.\nThe eastern passage is open. The combat shell ahead is a development placeholder, not a named story character.";
        INativeBossEncounter boss;
        public bool BossConnected => bossComponent && boss != null;
        void OnEnable()
        {
            if (!level || !terminal || !exit || !quest || !map || !dialogue)
            { if (Application.isPlaying) Debug.LogError("Native ECA: missing scene references", this); return; }
            level.Started += OnLevelStarted; terminal.Confirmed += OnTerminalConfirmed; exit.Confirmed += OnExitConfirmed;
            foreach (var memory in memoryNodes) if (memory && memory.interaction) memory.interaction.Confirmed += OnMemoryConfirmed;
            foreach (var region in regions) if (region) region.Entered += OnRegionEntered;
            boss = bossComponent as INativeBossEncounter;
            if (boss != null) boss.Defeated += OnBossDefeated;
            support = supportComponent as INativeSupport;
            if (support != null) support.Authorized += OnSupportAuthorized;
        }
        void OnDisable()
        {
            if (level) level.Started -= OnLevelStarted;
            if (terminal) terminal.Confirmed -= OnTerminalConfirmed;
            if (exit) exit.Confirmed -= OnExitConfirmed;
            foreach (var memory in memoryNodes) if (memory && memory.interaction) memory.interaction.Confirmed -= OnMemoryConfirmed;
            foreach (var region in regions) if (region) region.Entered -= OnRegionEntered;
            if (boss != null) boss.Defeated -= OnBossDefeated; boss = null;
            if (support != null) support.Authorized -= OnSupportAuthorized; support = null;
        }
        void OnLevelStarted()
        {
            quest.Activate(); if (narrative) narrative.Begin();
            dialogue.Show("COMMANDER / Your signal is fragmented.\nRecover a private memory, a system record and an initial echo. You can fight through the checkpoint or take the service path. Holding fire will not stop hostile units.");
        }
        void OnMemoryConfirmed(NativeInteraction source)
        {
            if (!level.Running || source.Used || !narrative) return;
            foreach (var node in memoryNodes)
            {
                if (!node || node.interaction != source || !quest.CanRecover(node.kind)) continue;
                if (narrative.Recover(node)) { source.Consume(); dialogue.Show(node.Transmission); }
                return;
            }
        }
        void OnTerminalConfirmed(NativeInteraction source)
        {
            if (!level.Running || source.Used) return;
            if (!quest.CanRecordTerminal())
            { dialogue.Show("COMMANDER / The link needs all three memory sources.\nRecovered " + (narrative ? narrative.MemoryCount : 0) + " of 3. Check the marked archive nodes before reconnecting."); return; }
            if (!quest.RecordTerminal()) return;
            source.Consume(); map.OpenExit(); narrative.RecordRelay(); dialogue.Show(terminalMessage);
            Debug.Log("Native ECA: terminal -> Quest relay objective -> Map passage -> Dialogue", this);
        }
        void OnRegionEntered(NativeRegion region, NativePlayer actor)
        {
            if (!level.Running || actor != level.player) return;
            if (region.purpose == NativeRegion.Purpose.ServiceBypass)
            { if (narrative) narrative.RecordBypass(); return; }
            if (!quest.CanStartBoss()) return;
            if (!BossConnected)
            { dialogue.Show("DEVELOPMENT CHECKPOINT\nThe real Boss component is not connected in this main-line source branch yet. No defeat or ending has been credited. Boss integration is required to continue."); return; }
            if (boss.IsDefeated || !quest.RecordBossStart()) return;
            map.LockArena(); boss.ActivateEncounter();
            dialogue.Show("DEVELOPMENT ENCOUNTER / Combat shell\nWatch its warning, move out of danger, then approach the exposed core and press E. Missed windows will return. This is placeholder combat, not a finalized Boss identity.");
        }
        public bool CanInteractBossCore(NativePlayer actor) => level.Running && quest.BossStarted && !quest.BossCleared && BossConnected && boss.CanInteractCore(actor);
        public bool InteractBossCore(NativePlayer actor) => CanInteractBossCore(actor) && boss.InteractCore(actor);
        void OnBossDefeated()
        {
            if (!level.Running || !BossConnected || !boss.IsDefeated || !quest.RecordBossDefeat()) return;
            narrative.RecordBossDefeated(); map.ReleaseArena();
            dialogue.Show("COMMANDER / The lock has broken.\nThe final archive node is through the eastern gate. You can keep the conflicting records instead of letting the system erase their differences.");
            Debug.Log("Native ECA: real Boss.Defeated -> Quest -> Narrative -> Map final gate -> Dialogue", this);
        }
        void OnSupportAuthorized(NativeSupportAuthorization authorization)
        {
            if (level.Running) narrative.RecordSupport(authorization);
        }
        public bool PreserveAnomaly() => level.Running && narrative.PreserveAnomaly();
        public bool RewriteEcho() => level.Running && narrative.RewriteEcho();
        void OnExitConfirmed(NativeInteraction source)
        {
            if (!level.Running || source.Used) return;
            if (!quest.CanLeaveSector()) { dialogue.Show("FINAL NODE / Access denied.\nRecover the three records and complete the real core encounter first."); return; }
            level.hud.phone.ShowFinalDecision();
        }
        public bool ChooseFinal(NativeFinalChoice choice)
        {
            if (!level.Running || !quest.CanLeaveSector() || !exit.CanReach(level.player)) return false;
            if (!narrative.CommitEnding(level.combat.Kills, level.Elapsed, choice) || !quest.RecordFinalObjective()) return false;
            if (level.Complete()) exit.Consume();
            Debug.Log("Native ECA: final choice -> Quest condition -> Narrative quadrant -> Level ending", this);
            return true;
        }
    }
}
