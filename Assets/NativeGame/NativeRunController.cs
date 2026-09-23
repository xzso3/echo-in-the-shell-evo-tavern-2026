using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echo.NativeGame
{
    // Level owns only this run's phase, clock display and terminal result. Other modules own their state.
    public sealed class NativeRunController : MonoBehaviour
    {
        public enum RunPhase { Starting, Playing, Ending, Completed, Dead }
        public NativePlayer player;
        public NativeCombat combat;
        public NativeCombatIntegration combatIntegration;
        public NativeQuest quest;
        public NativeMap map;
        public NativeDialogue dialogue;
        public NativeEcaRules rules;
        public NativeHud hud;
        public NativeNarrative narrative;
        public NativeEndingSequence endingSequence;
        public RunPhase Phase { get; private set; }
        public float Elapsed { get; private set; }
        public bool Running => Phase == RunPhase.Playing && player && player.Alive;
        public event Action Started;
        void Awake()
        {
            Time.timeScale = 1;
            if (!player || !combat || !combatIntegration || !quest || !map || !dialogue || !rules || !hud)
            { Debug.LogError("NativeDemo: missing module reference on Level.", this); enabled = false; }
        }
        void Start()
        {
            Phase = RunPhase.Playing;
            if (!combatIntegration.Initialize(this))
            {
                Phase = RunPhase.Starting;
                Debug.LogError("NativeDemo: shared combat integration failed; run remains stopped.", this);
                return;
            }
            Started?.Invoke();
        }
        void Update() { if (Running) Elapsed += Time.deltaTime; }
        public bool Complete()
        {
            if (!Running || !quest.Completed || !narrative || !narrative.EndingCommitted) return false;
            player.MoveInput = Vector2.zero; dialogue.Close(); hud.ClosePhone();
            if (narrative.Ending == NativeEnding.Birth)
            { Phase = RunPhase.Ending; endingSequence.Begin(); }
            else { Phase = RunPhase.Completed; hud.ShowResult(narrative.EndingTitle, narrative.EndingText); }
            return true;
        }
        public void FinishBirth()
        {
            if (Phase != RunPhase.Ending || narrative.BirthStage != NativeBirthStage.PhoneContinuation) return;
            Phase = RunPhase.Completed; hud.phone.ShowContinuation();
        }
        public void PlayerDied()
        {
            if (Phase != RunPhase.Playing) return;
            Phase = RunPhase.Dead; dialogue.Close();
            hud.ShowResult("躯壳失联", "你的信号中断了。\n\n保持移动，也可以尝试绕行。\n停火或打开手机，都不会暂停敌人的行动。");
        }
        public void Restart() { Time.timeScale = 1; SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
    }
}
