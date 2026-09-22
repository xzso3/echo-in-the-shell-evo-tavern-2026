using UnityEngine;
namespace Echo.NativeGame
{
    // The actual rules of this scene: Unity events -> pure Quest conditions -> owning-module actions.
    // No business state, registry, service container, JSON interpreter or independent simulation clock.
    public sealed class NativeEcaRules : MonoBehaviour
    {
        [Header("Event sources")]
        public NativeRunController level;
        public NativeInteraction terminal, exit;
        [Header("Conditions and actions")]
        public NativeQuest quest;
        public NativeMap map;
        public NativeDialogue dialogue;
        [TextArea] public string terminalMessage = "COMMANDER / Link restored.\nThe eastern gate is open. Take the exit when you are ready.";
        void OnEnable()
        {
            if (!level || !terminal || !exit || !quest || !map || !dialogue)
            { if (Application.isPlaying) Debug.LogError("Native ECA: missing scene references", this); return; }
            level.Started += OnLevelStarted;
            terminal.Confirmed += OnTerminalConfirmed;
            exit.Confirmed += OnExitConfirmed;
        }
        void OnDisable()
        {
            if (level) level.Started -= OnLevelStarted;
            if (terminal) terminal.Confirmed -= OnTerminalConfirmed;
            if (exit) exit.Confirmed -= OnExitConfirmed;
        }
        void OnLevelStarted() { quest.Activate(); }
        void OnTerminalConfirmed(NativeInteraction source)
        {
            if (!level.Running || source.Used || !quest.CanRecordTerminal()) return;
            if (!quest.RecordTerminal()) return;
            source.Consume(); map.OpenExit(); dialogue.Show(terminalMessage);
            Debug.Log("Native ECA: interaction.confirmed -> quest.can_record -> quest.record -> map.open_exit -> dialogue.show", this);
        }
        void OnExitConfirmed(NativeInteraction source)
        {
            if (!level.Running || source.Used) return;
            if (!quest.CanLeaveSector())
            { dialogue.Show("COMMANDER / Exit locked.\nReconnect the cyan terminal before leaving this sector."); return; }
            if (level.Complete()) source.Consume();
            Debug.Log("Native ECA: exit.confirmed -> quest.can_leave -> level.complete", this);
        }
    }
}
