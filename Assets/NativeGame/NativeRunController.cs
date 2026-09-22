using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echo.NativeGame
{
    // Level owns only this run's phase, clock display and terminal result. Other modules own their state.
    public sealed class NativeRunController : MonoBehaviour
    {
        public enum RunPhase { Starting, Playing, Completed, Dead }
        public NativePlayer player;
        public NativeCombat combat;
        public NativeQuest quest;
        public NativeMap map;
        public NativeDialogue dialogue;
        public NativeEcaRules rules;
        public NativeHud hud;
        public NativeNarrative narrative;
        public RunPhase Phase { get; private set; }
        public float Elapsed { get; private set; }
        public bool Running => Phase == RunPhase.Playing && player && player.Alive;
        public event Action Started;
        void Awake()
        {
            Time.timeScale = 1;
            if (!player || !combat || !quest || !map || !dialogue || !rules || !hud)
            { Debug.LogError("NativeDemo: missing module reference on Level.", this); enabled = false; }
        }
        void Start() { Phase = RunPhase.Playing; Started?.Invoke(); }
        void Update() { if (Running) Elapsed += Time.deltaTime; }
        public bool Complete()
        {
            if (!Running || !quest.Completed || !narrative || !narrative.EndingCommitted) return false;
            Phase = RunPhase.Completed; player.MoveInput = Vector2.zero; dialogue.Close();
            hud.ShowResult(narrative.EndingTitle, narrative.EndingText);
            return true;
        }
        public void PlayerDied()
        {
            if (Phase != RunPhase.Playing) return;
            Phase = RunPhase.Dead; dialogue.Close();
            hud.ShowResult("SHELL OFFLINE", "Your signal was lost.\n\nKeep moving or use the side paths.\nHolding fire and opening the phone do not pause enemies.");
        }
        public void Restart() { Time.timeScale = 1; SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
    }
}
