using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using Echo.LevelToolkit.Foundation;
namespace Echo.NativeGame
{
    // Concrete input/presentation component. Reads module state; never owns quest/combat/door state.
    public sealed class NativeHud : MonoBehaviour
    {
        public NativeRunController level;
        public NativeInteraction[] interactables;
        public TMP_Text healthLabel, fireLabel, objectiveLabel, promptLabel, counterLabel, resultTitle, resultBody;
        public GameObject phonePanel, resultPanel;
        public TMP_Text phoneArchive;
        public NativePhone phone;
        public UnityEngine.UI.Button restartButton, phoneCloseButton;
        readonly NativeInputTargetRegistry inputTargets = new NativeInputTargetRegistry();
        public bool IntegratedInput => inputTargets.IntegratedMode;
        public bool HasActiveInputScope => inputTargets.HasActiveScope;
        public bool ActiveInputReady => inputTargets.IsActiveRunReady(level, level ? level.player : null);
        public RuntimeScope ActiveInputScope => inputTargets.ActiveScope;
        // LevelHost registers only this instance's endpoints and switches the active
        // scope as the player crosses a level boundary. Legacy serialized input stays
        // available in NativeDemo until the first explicit instance registration.
        public bool RegisterInputInstance(LevelInstanceContext context) => inputTargets.RegisterInstance(context, level);
        public bool RegisterInteraction(RuntimeScope scope, NativeInteraction target) => inputTargets.RegisterInteraction(scope, target);
        public bool RegisterBoss(RuntimeScope scope, NativeBossController target) => inputTargets.RegisterBoss(scope, target);
        public void UnregisterInteraction(RuntimeScope scope, NativeInteraction target) => inputTargets.UnregisterInteraction(scope, target);
        public void UnregisterBoss(RuntimeScope scope, NativeBossController target) => inputTargets.UnregisterBoss(scope, target);
        public void UnregisterInputInstance(RuntimeScope scope) => inputTargets.UnregisterInstance(scope);
        public bool SetActiveInputInstance(RuntimeScope scope) => inputTargets.SetActiveInstance(scope);
        public void ClearActiveInputInstance() => inputTargets.ClearActiveInstance();
        public NativeBossController CurrentSupportBoss() => inputTargets.FindSupportBoss(level, level ? level.player : null);
        void Awake()
        {
            phonePanel.SetActive(false); resultPanel.SetActive(false);
            restartButton.onClick.AddListener(level.Restart); phoneCloseButton.onClick.AddListener(ClosePhone);
        }
        void Update()
        {
            var player = level.player; var combat = level.combat; var dialogue = level.dialogue;
            bool typing = EventSystem.current && EventSystem.current.currentSelectedGameObject &&
                (EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>() || EventSystem.current.currentSelectedGameObject.GetComponent<UnityEngine.UI.InputField>());
            player.MoveInput = Vector2.zero;
            NativeInteraction nearby = null;
            NativeBossController nearbyBoss = null;
            bool legacyCore = false;
            if (!typing && level.Phase == NativeRunController.RunPhase.Ending && Input.GetKeyDown(KeyCode.E)) level.endingSequence.FirstPunch();
            if (!typing && level.Phase == NativeRunController.RunPhase.Completed && Input.GetKeyDown(KeyCode.Tab)) TogglePhone();
            if (level.Running && !typing)
            {
                player.MoveInput = Vector2.ClampMagnitude(new Vector2((Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0),
                    (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0)), 1);
                if (Input.GetKeyDown(KeyCode.Space)) combat.ToggleFire();
                if (Input.GetKeyDown(KeyCode.Tab)) TogglePhone();
                if (Input.GetKeyDown(KeyCode.Return) && phonePanel.activeSelf && phone) phone.ConfirmSupport();
                if (Input.GetKeyDown(KeyCode.Escape)) { ClosePhone(); dialogue.Close(); }
                if (IntegratedInput)
                {
                    var selection = inputTargets.FindTarget(level, player);
                    nearby = selection.Interaction; nearbyBoss = selection.Boss;
                }
                else
                {
                    nearby = NativeInteraction.FindNearest(interactables, player);
                    legacyCore = level.rules.CanInteractBossCore(player);
                }
                // An E that opens a dialogue cannot also close that newly created session.
                if (Input.GetKeyDown(KeyCode.E))
                {
                    if (dialogue.IsOpen) dialogue.Close();
                    else if (nearbyBoss) nearbyBoss.InteractCore(player);
                    else if (legacyCore) level.rules.InteractBossCore(player);
                    else if (nearby) nearby.Use(player);
                }
            }
            healthLabel.text = "生命  " + Mathf.CeilToInt(player.Health) + " / " + player.maxHealth;
            fireLabel.text = combat.AutoFire ? "自动开火  /  Space 停火" : "停火  /  Space 自动开火";
            fireLabel.color = combat.AutoFire ? new Color(.3f, 1, .85f) : new Color(1, .78f, .35f);
            objectiveLabel.text = level.quest.ObjectiveText;
            if (!phone && phoneArchive && phonePanel.activeSelf && level.narrative) phoneArchive.text = level.narrative.MemorySummary();
            counterLabel.text = string.Format("{0:00}:{1:00}   /   击败敌人 {2}", (int)level.Elapsed / 60, (int)level.Elapsed % 60, combat.Kills) + "   |   同步度 " + level.narrative.Sync + "   /   差异度 " + level.narrative.Difference;
            promptLabel.text = dialogue.HasChoices ? "点击选择 / E 暂不选择" : dialogue.IsOpen ? "E  /  收到" : nearbyBoss || legacyCore ? "E  /  解除核心封锁" : nearby ? nearby.Prompt : level.Running ? "WASD 移动    Space 自动开火/停火    E 互动    Tab 手机" : "";
        }
        public void ShowResult(string title, string body)
        { if (phone) phone.CancelComposition(); phonePanel.SetActive(false); resultTitle.text = title; resultBody.text = body; resultPanel.SetActive(true); ClearSelection(); }
        public void TogglePhone() { if (phonePanel.activeSelf && phone) phone.CancelComposition(); phonePanel.SetActive(!phonePanel.activeSelf); ClearSelection(); }
        public void ClosePhone() { if (phone) phone.CancelComposition(); phonePanel.SetActive(false); if (phone) phone.CloseDecision(); ClearSelection(); }
        static void ClearSelection() { if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null); }
    }
}
