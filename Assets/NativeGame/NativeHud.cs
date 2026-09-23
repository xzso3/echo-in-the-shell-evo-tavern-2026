using System;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using Echo.LevelToolkit.Combat;
using Echo.LevelToolkit.Foundation;
using Echo.NativeGame.Commander;
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
        ICommanderSession commanderSession;
        public bool IntegratedInput => inputTargets.IntegratedMode;
        public bool HasActiveInputScope => inputTargets.HasActiveScope;
        public bool ActiveInputReady => inputTargets.IsActiveRunReady(level, level ? level.player : null);
        public RuntimeScope ActiveInputScope => inputTargets.ActiveScope;
        public CombatPlayer ActiveToolkitPlayer => inputTargets.ActiveCombatPlayer;
        public CombatWeapon ActiveToolkitWeapon => inputTargets.ActiveCombatWeapon;
        // LevelHost registers only this instance's endpoints and switches the active
        // scope as the player crosses a level boundary. Legacy serialized input stays
        // available in NativeDemo until the first explicit instance registration.
        public bool RegisterInputInstance(LevelInstanceContext context) => inputTargets.RegisterInstance(context, level);
        public bool RegisterToolkitInputInstance(LevelInstanceContext context, CombatPlayer player,
            CombatWeapon weapon, Transform placementRoot) =>
            inputTargets.RegisterToolkitInstance(context, level, player, weapon, placementRoot);
        public bool RegisterInteraction(RuntimeScope scope, NativeInteraction target) => inputTargets.RegisterInteraction(scope, target);
        public bool RegisterInteraction(RuntimeScope scope, NativeScopedInteraction target) => inputTargets.RegisterInteraction(scope, target);
        public bool RegisterBoss(RuntimeScope scope, NativeBossController target) => inputTargets.RegisterBoss(scope, target);
        public bool RegisterBoss(RuntimeScope scope, CombatBoss target) => inputTargets.RegisterBoss(scope, target);
        public void UnregisterInteraction(RuntimeScope scope, NativeInteraction target) => inputTargets.UnregisterInteraction(scope, target);
        public void UnregisterInteraction(RuntimeScope scope, NativeScopedInteraction target) => inputTargets.UnregisterInteraction(scope, target);
        public void UnregisterBoss(RuntimeScope scope, NativeBossController target) => inputTargets.UnregisterBoss(scope, target);
        public void UnregisterBoss(RuntimeScope scope, CombatBoss target) => inputTargets.UnregisterBoss(scope, target);
        public void UnregisterInputInstance(RuntimeScope scope)
        {
            if (ActiveInputScope == scope && ActiveToolkitPlayer) ActiveToolkitPlayer.MoveInput = Vector2.zero;
            inputTargets.UnregisterInstance(scope);
        }
        public bool SetActiveInputInstance(RuntimeScope scope)
        {
            if (ActiveToolkitPlayer) ActiveToolkitPlayer.MoveInput = Vector2.zero;
            return inputTargets.SetActiveInstance(scope);
        }
        public void ClearActiveInputInstance()
        {
            if (ActiveToolkitPlayer) ActiveToolkitPlayer.MoveInput = Vector2.zero;
            inputTargets.ClearActiveInstance();
        }
        public NativeBossController CurrentSupportBoss() => inputTargets.FindSupportBoss(level, level ? level.player : null);
        public CombatBoss CurrentCombatSupportBoss() => inputTargets.FindCombatSupportBoss(level, level ? level.player : null);
        void Awake()
        {
            phonePanel.SetActive(false); resultPanel.SetActive(false);
            restartButton.onClick.AddListener(level.Restart); phoneCloseButton.onClick.AddListener(ClosePhone);
            if (phone && phone.tabletView)
            {
                var oldPhone = phonePanel.GetComponent<CanvasGroup>();
                if (!oldPhone) oldPhone = phonePanel.AddComponent<CanvasGroup>();
                oldPhone.alpha = 0f;
                oldPhone.interactable = false;
                oldPhone.blocksRaycasts = false;
                phone.tabletView.gameObject.SetActive(false);
            }
        }
        public void BindCommanderSession(ICommanderSession session) { commanderSession = session; }
        void OnDestroy()
        {
            if (commanderSession is IDisposable disposable) disposable.Dispose();
            commanderSession = null;
        }
        void Update()
        {
            var player = level.player; var combat = level.combat; var dialogue = level.dialogue;
            var toolkitPlayer = ActiveToolkitPlayer;
            var toolkitWeapon = ActiveToolkitWeapon;
            bool typing = EventSystem.current && EventSystem.current.currentSelectedGameObject &&
                (EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>() || EventSystem.current.currentSelectedGameObject.GetComponent<UnityEngine.UI.InputField>());
            player.MoveInput = Vector2.zero;
            if (toolkitPlayer) toolkitPlayer.MoveInput = Vector2.zero;
            NativeInteraction nearby = null;
            NativeScopedInteraction nearbyScoped = null;
            NativeBossController nearbyBoss = null;
            CombatBoss nearbyCombatBoss = null;
            bool legacyCore = false;
            bool phoneOpen = phonePanel.activeSelf;
            if (phoneOpen)
            {
                if (Input.GetKeyDown(KeyCode.Escape) || !typing && Input.GetKeyDown(KeyCode.Tab))
                {
                    if (phone && phone.tabletView && phone.tabletView.gameObject.activeSelf)
                        phone.tabletView.HandleBack();
                    else ClosePhone();
                }
                else if (!typing && Input.GetKeyDown(KeyCode.Return) && phone &&
                    (!phone.tabletView || !phone.tabletView.gameObject.activeSelf)) phone.ConfirmSupport();
            }
            else if (!typing)
            {
                if (level.Phase == NativeRunController.RunPhase.Ending && Input.GetKeyDown(KeyCode.E)) level.endingSequence.FirstPunch();
                if (level.Phase == NativeRunController.RunPhase.Completed && Input.GetKeyDown(KeyCode.Tab)) OpenPhone();
                if (level.Running)
                {
                    if (Input.GetKeyDown(KeyCode.Tab)) OpenPhone();
                    if (!phonePanel.activeSelf)
                    {
                        Vector2 movement = Vector2.ClampMagnitude(new Vector2((Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0),
                            (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0)), 1);
                        if (toolkitPlayer && ActiveInputReady) toolkitPlayer.MoveInput = movement;
                        else if (!IntegratedInput || ActiveInputReady) player.MoveInput = movement;
                        if (Input.GetKeyDown(KeyCode.Space))
                        { if (toolkitWeapon && ActiveInputReady) toolkitWeapon.ToggleFire(); else if (!IntegratedInput || ActiveInputReady) combat.ToggleFire(); }
                        if (Input.GetKeyDown(KeyCode.Escape)) dialogue.Close();
                        if (IntegratedInput)
                        {
                            var selection = inputTargets.FindTarget(level, player);
                            nearby = selection.Interaction; nearbyScoped = selection.ScopedInteraction;
                            nearbyBoss = selection.Boss; nearbyCombatBoss = selection.CombatBoss;
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
                            else if (nearbyCombatBoss) nearbyCombatBoss.InteractCore(toolkitPlayer);
                            else if (nearbyBoss) nearbyBoss.InteractCore(player);
                            else if (legacyCore) level.rules.InteractBossCore(player);
                            else if (nearbyScoped) nearbyScoped.Use(toolkitPlayer);
                            else if (nearby) nearby.Use(player);
                        }
                    }
                }
            }
            healthLabel.text = "生命  " + Mathf.CeilToInt(toolkitPlayer ? toolkitPlayer.Health : player.Health) + " / " + (toolkitPlayer ? toolkitPlayer.maxHealth : player.maxHealth);
            bool autoFire = toolkitWeapon ? toolkitWeapon.AutoFire : combat.AutoFire;
            fireLabel.text = autoFire ? "自动开火  /  Space 停火" : "停火  /  Space 自动开火";
            fireLabel.color = autoFire ? new Color(.3f, 1, .85f) : new Color(1, .78f, .35f);
            objectiveLabel.text = level.quest.ObjectiveText;
            if (!phone && phoneArchive && phonePanel.activeSelf && level.narrative) phoneArchive.text = level.narrative.MemorySummary();
            counterLabel.text = string.Format("{0:00}:{1:00}   /   击败敌人 {2}", (int)level.Elapsed / 60, (int)level.Elapsed % 60, toolkitWeapon ? toolkitWeapon.Kills : combat.Kills) + "   |   同步度 " + level.narrative.Sync + "   /   差异度 " + level.narrative.Difference;
            promptLabel.text = phonePanel.activeSelf ? "" : dialogue.HasChoices ? "点击选择 / E 暂不选择" : dialogue.IsOpen ? "E  /  收到" : nearbyBoss || nearbyCombatBoss || legacyCore ? "E  /  解除核心封锁" : nearbyScoped ? nearbyScoped.Prompt : nearby ? nearby.Prompt : level.Running ? "WASD 移动    Space 自动开火/停火    E 互动    Tab 手机" : "";
        }
        public void ShowResult(string title, string body)
        { ClosePhone(); resultTitle.text = title; resultBody.text = body; resultPanel.SetActive(true); }
        public void TogglePhone() { if (phonePanel.activeSelf) ClosePhone(); else OpenPhone(); }
        public void OpenPhone()
        {
            level.PhonePause.OpenPhone();
            phonePanel.SetActive(true);
            if (phone && phone.tabletView)
            {
                phone.tabletView.gameObject.SetActive(true);
                phone.tabletView.RefreshExternal();
            }
            if (level.player) level.player.MoveInput = Vector2.zero;
            if (ActiveToolkitPlayer) ActiveToolkitPlayer.MoveInput = Vector2.zero;
            ClearSelection();
        }
        public void ClosePhone()
        {
            if (phonePanel.activeSelf || level.PhonePause.IsPhoneOpen)
            {
                if (phone && phone.tabletView) phone.tabletView.gameObject.SetActive(false);
                commanderSession?.Cancel();
                if (phone) phone.CancelComposition();
            }
            phonePanel.SetActive(false);
            level.PhonePause.ClosePhone();
            if (phone) phone.CloseDecision();
            ClearSelection();
        }
        public void PrepareForRestart()
        {
            commanderSession?.Reset();
            var supportBridge = level.GetComponent<CommanderSupportBridge>();
            if (supportBridge) supportBridge.Reset();
            ClosePhone();
        }
        static void ClearSelection() { if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null); }
    }
}
