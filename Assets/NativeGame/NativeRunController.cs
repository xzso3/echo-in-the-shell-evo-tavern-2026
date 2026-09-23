using System;
using Echo.NativeGame.Commander;
using Echo.NativeGame.GameFlow;
using Echo.NativeGame.GameFlow.Ending;
using Echo.NativeGame.GameFlow.Results;
using Echo.NativeGame.ToolkitIntegration.LevelHost;
using UnityEngine;

namespace Echo.NativeGame
{
    // Level owns only this run's phase, clock display and terminal result. Other modules own their state.
    [DefaultExecutionOrder(-100)]
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
        public bool IsCombatAdvancing => PhonePause.IsCombatAdvancing;
        public NativeRunResultFlow ResultFlow
        {
            get
            {
                if (!resultFlow) resultFlow = GetComponent<NativeRunResultFlow>();
                if (!resultFlow) resultFlow = gameObject.AddComponent<NativeRunResultFlow>();
                return resultFlow;
            }
        }
        public IPhonePauseController PhonePause
        {
            get
            {
                if (!phonePause) phonePause = GetComponent<NativePhonePauseController>();
                if (!phonePause) phonePause = gameObject.AddComponent<NativePhonePauseController>();
                return phonePause;
            }
        }
        NativePhonePauseController phonePause;
        NativeRunResultFlow resultFlow;
        bool prepared;
        bool actionStarted;
        public event Action Started;
        void Awake()
        {
            _ = PhonePause;
            if (!GetComponent<FlowNavigation>()) gameObject.AddComponent<FlowNavigation>();
            if (!player || !combat || !combatIntegration || !quest || !map || !dialogue || !rules ||
                !hud || !narrative || !endingSequence)
            { Debug.LogError("NativeDemo: missing module reference on Level.", this); enabled = false; return; }
            if (!GetComponent<CommanderRuntimeHost>()) gameObject.AddComponent<CommanderRuntimeHost>();
        }
        void Start()
        {
            Phase = RunPhase.Starting;
            Elapsed = 0f;
            var navigation = GetComponent<FlowNavigation>();
            if (!navigation) navigation = gameObject.AddComponent<FlowNavigation>();
            var lifecycle = GetComponent<FlowRunLifecycle>();
            if (!lifecycle) lifecycle = gameObject.AddComponent<FlowRunLifecycle>();
            var pause = GetComponent<FlowPauseController>();
            var pauseMenu = GetComponent<FlowPauseMenuController>();
            if (!pauseMenu) pauseMenu = gameObject.AddComponent<FlowPauseMenuController>();
            pauseMenu.TryReturnToMenu = () => navigation.TryLoad(FlowDestination.Menu);
            pauseMenu.TerminalPhoneBackRequested += OnTerminalPhoneBackRequested;
            _ = ResultFlow;
            var killLedger = GetComponent<RunKillLedger>();
            var levelHost = GetComponent<NativeLevelHost>();
            if (!levelHost)
            {
                var runtime = GetComponent<CommanderRuntimeHost>();
                if (runtime) levelHost = runtime.levelHost;
            }
            if (killLedger && levelHost) killLedger.SetHost(levelHost);
            prepared = true;
            if (!pause || !lifecycle.Initialize(BeginAction,
                paused => pause.SetPaused(FlowPauseReason.Intro, paused)))
                Debug.LogError("NativeDemo: intro gate could not be initialized.", this);
        }
        // The intro UI is the only caller. Initialization that requires Running and
        // the existing Started subscribers all run in this one synchronous action.
        public bool BeginAction()
        {
            if (!prepared || actionStarted || Phase != RunPhase.Starting) return false;
            Phase = RunPhase.Playing;
            if (!combatIntegration.Initialize(this))
            {
                Phase = RunPhase.Starting;
                Debug.LogError("NativeDemo: shared combat integration failed; run remains stopped.", this);
                return false;
            }
            actionStarted = true;
            Started?.Invoke();
            return true;
        }
        void Update() { if (IsCombatAdvancing) Elapsed += Time.deltaTime; }
        public bool Complete()
        {
            if (!Running || !quest.Completed || !narrative || !narrative.EndingCommitted) return false;
            if (!ResultFlow.FreezeSuccess(this, narrative.BehaviorRecords)) return false;
            if (narrative.Ending == NativeEnding.Birth)
            {
                Phase = RunPhase.Ending;
                GetComponent<FlowPauseMenuController>()?.CloseForRunEnd();
                NativeTerminalCleanup.StopInteractions(this);
                endingSequence.Begin();
            }
            else
            {
                Phase = RunPhase.Completed;
                GetComponent<FlowPauseMenuController>()?.CloseForRunEnd();
                NativeTerminalCleanup.StopInteractions(this);
            }
            return true;
        }
        public void FinishBirth()
        {
            if (Phase != RunPhase.Ending || narrative.BirthStage != NativeBirthStage.PhoneContinuation) return;
            if (!ResultFlow.ReachBirthContinuation(narrative)) return;
            Phase = RunPhase.Completed;
            hud.phone.ShowContinuation();
        }
        public void PlayerDied()
        {
            if (Phase != RunPhase.Playing) return;
            if (!ResultFlow.FreezeDeath(this, narrative.BehaviorRecords)) return;
            Phase = RunPhase.Dead;
            GetComponent<FlowPauseMenuController>()?.CloseForRunEnd();
            NativeTerminalCleanup.StopInteractions(this);
        }
        public void Restart()
        {
            var navigation = GetComponent<FlowNavigation>();
            if (navigation) navigation.RetryRun();
        }
        void OnTerminalPhoneBackRequested()
        {
            if (hud && hud.TerminalUi && hud.TerminalUi.TryHandleTabletBack()) return;
            ResultFlow.ViewResults();
        }
        void OnDestroy()
        {
            var pauseMenu = GetComponent<FlowPauseMenuController>();
            if (pauseMenu) pauseMenu.TerminalPhoneBackRequested -= OnTerminalPhoneBackRequested;
        }
    }
}
