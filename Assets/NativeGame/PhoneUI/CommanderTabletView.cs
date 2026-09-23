using System;
using System.Collections.Generic;
using Echo.NativeGame.Commander;
using TMPro;
using UnityEngine;

namespace Echo.NativeGame.PhoneUI
{
    public enum CommanderTabletPage { Communications, Support, Records }

    // The run adapter maps existing local Support/Phone actions into page buttons.
    // The view never calculates a support cost or changes game state itself.
    public sealed class CommanderTabletAction
    {
        public string Label { get; }
        public bool Enabled { get; }
        public Action Invoke { get; }
        public bool Selected { get; }

        public CommanderTabletAction(string label, Action invoke, bool enabled = true, bool selected = false)
        {
            Label = label ?? string.Empty;
            Invoke = invoke;
            Enabled = enabled;
            Selected = selected;
        }
    }

    // Presentation-only data: no UI code interprets prose to decide game rules.
    public sealed class CommanderTabletEntry
    {
        public string Title, Status, Preview;
        public Action Open;
    }

    public sealed class CommanderTabletGroup
    {
        public string Label;
        public IReadOnlyList<CommanderTabletAction> Options;
    }

    public sealed class CommanderTabletReading
    {
        public string Key, Title, Source, Body, Footer;
        public Action Back;
        public IReadOnlyList<CommanderTabletEntry> Entries;
        public IReadOnlyList<CommanderTabletGroup> Groups;
        public IReadOnlyList<CommanderTabletAction> Actions;
    }

    public sealed class CommanderTabletBindings
    {
        public ICommanderSession Session;
        public ICommanderSupportBridge Support;
        public Func<bool> CanCompose;
        public Func<string> ComposeReason;
        public Func<string> ConnectionStatus;
        public Func<string> HealthStatus;
        public Func<Guid, NativeSupportKind?> ProposalKind;
        public Func<Guid, string> ProposalTitle;
        public Func<IReadOnlyList<CommanderTabletAction>> CommunicationsActions;
        public Func<bool> OnlineAvailable;
        public Func<CommanderTabletReading> LocalPage, SupportPage, RecordsPage;
        public Func<bool> TryBack;
        public Action ResetPresentation;
        public Action CloseRequested;
    }

    // Build under a 1280 x 720 CanvasScaler canvas with a GraphicRaycaster.
    // Screen text and hit targets are native uGUI/TMP; sprites only decorate them.
    [RequireComponent(typeof(RectTransform))]
    public sealed class CommanderTabletView : MonoBehaviour
    {
        static readonly Color Ink = new Color32(9, 25, 24, 255);
        static readonly Color Screen = new Color32(13, 41, 31, 255);
        static readonly Color Pale = new Color32(213, 232, 216, 255);
        static readonly Color Muted = new Color32(136, 164, 152, 255);
        static readonly Color Jade = new Color32(156, 222, 179, 255);
        static readonly Color Amber = new Color32(223, 177, 109, 255);
        static readonly Color Error = new Color32(207, 138, 120, 255);
        static readonly Color Edge = new Color32(66, 102, 80, 255);

        [Header("Optional UI-02 decoration; assign sprites after Build, then ApplySprites")]
        public Sprite tabletFrameSprite;
        public Sprite physicalReturnKeySprite;
        public Sprite commanderSignalAvatarSprite;
        public Sprite communicationsTabSprite;
        public Sprite supportTabSprite;
        public Sprite recordsTabSprite;
        public Sprite authorizationMedicalSprite;

        // Let INT route Esc through HandleBack once from NativeHud. Enabling this
        // directly is useful only when no other component consumes Escape.
        public bool handleEscapeLocally;

        public CommanderTabletPage SelectedPage { get; private set; }
        public bool ContractOpen => contractOverlay && contractOverlay.activeSelf;

        CommanderTabletBindings bindings;
        TMP_FontAsset font;
        RectTransform tablet;
        UnityEngine.UI.Image frameImage, returnImage, avatarImage;
        readonly UnityEngine.UI.Image[] tabIcons = new UnityEngine.UI.Image[3];
        readonly UnityEngine.UI.Button[] tabButtons = new UnityEngine.UI.Button[3];
        readonly UnityEngine.UI.Button[] localTopicButtons = new UnityEngine.UI.Button[4];
        UnityEngine.UI.ScrollRect chatScroll;
        RectTransform chatContent;
        TMP_InputField input;
        UnityEngine.UI.Button sendButton, cancelButton, acceptButton;
        TMP_Text connectionLabel, healthLabel, locationLabel, waitLabel;
        TMP_Text contractText, contractStatus;
        GameObject commsPanel, supportPanel, recordsPanel, contractOverlay;
        RectTransform supportContent, recordsContent, localContent;
        UnityEngine.UI.ScrollRect localScroll;
        GameObject localPanel, onlinePanel, modeBar;
        UnityEngine.UI.Button localModeButton, onlineModeButton;
        bool onlineMode;
        string localPresentation;
        sealed class ReadingState
        {
            public UnityEngine.UI.ScrollRect Scroll;
            public RectTransform Body, Footer, Back;
            public bool HasBack, HasActions;
            public string Key;
            public readonly Dictionary<string, float> Positions = new Dictionary<string, float>();
        }
        readonly Dictionary<RectTransform, ReadingState> readingStates = new Dictionary<RectTransform, ReadingState>();
        Guid? openProposal;
        Action manualAccept, manualReject;
        string notice;
        bool refreshing, built, decisionMode;
        int contractOpenedFrame;
        float nextPassiveRefresh;
        string supportPresentation, recordsPresentation;
        int renderedMessageCount = -1;
        Guid renderedSessionId;
        readonly Dictionary<Guid, UnityEngine.UI.Button> proposalButtons =
            new Dictionary<Guid, UnityEngine.UI.Button>();
        readonly Dictionary<Guid, TMP_Text> proposalStates = new Dictionary<Guid, TMP_Text>();

        public void Build(TMP_FontAsset chineseFont)
        {
            if (built) return;
            built = true;
            font = chineseFont ? chineseFont : TMP_Settings.defaultFontAsset;
            var root = GetComponent<RectTransform>();
            if (!root) root = gameObject.AddComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;

            var dim = Image(Fill("World Dim", transform), new Color(0f, .035f, .03f, .72f), true);
            dim.transform.SetAsFirstSibling();
            tablet = new GameObject("Tactical Tablet", typeof(RectTransform)).GetComponent<RectTransform>();
            tablet.SetParent(transform, false);
            tablet.anchorMin = tablet.anchorMax = new Vector2(.5f, .5f);
            tablet.pivot = new Vector2(.5f, .5f);
            tablet.sizeDelta = new Vector2(1088, 612);
            Image(Box("Tablet Base", tablet, 0, 0, 1088, 612), new Color32(29, 39, 40, 255));
            Image(Box("Screen", tablet, 151, 104, 786, 388), Screen);
            Image(Box("Status Bar", tablet, 159, 112, 770, 30), Ink);
            avatarImage = Image(Box("Commander Signal", tablet, 168, 116, 22, 22), Color.clear, false);
            connectionLabel = Text(Box("AI Connection", tablet, 198, 115, 510, 24),
                "SHELL-LINK / 本地模式", 18, Pale);
            healthLabel = Text(Box("Health", tablet, 710, 115, 207, 24), "", 16, Pale,
                TextAlignmentOptions.MidlineRight);
            Image(Box("Header Divider", tablet, 159, 145, 770, 1), Edge);

            commsPanel = new GameObject("Communications", typeof(RectTransform));
            SetupBox(commsPanel.GetComponent<RectTransform>(), tablet, 159, 148, 770, 292);
            BuildComms();
            supportPanel = BuildReadingPage("Support", out supportContent);
            recordsPanel = BuildReadingPage("Records", out recordsContent);

            for (int i = 0; i < 3; i++)
            {
                int page = i;
                float x = 159 + i * 258;
                float width = i == 2 ? 254 : 250;
                tabButtons[i] = Button(Box("Tab " + i, tablet, x, 446, width, 42),
                    "", new Color32(35, 62, 48, 255), Pale, 18);
                tabButtons[i].onClick.AddListener(() => SelectPage((CommanderTabletPage)page));
                tabIcons[i] = Image(Box("Icon", tabButtons[i].transform, 19, 4, 34, 34), Color.clear, false);
                Text(Box("Label", tabButtons[i].transform, 59, 8, 165, 27),
                    i == 0 ? "通讯" : i == 1 ? "支援" : "记录", 18, Pale);
            }

            frameImage = Image(Box("Tablet Frame Art", tablet, 0, 0, 1088, 612), Color.clear, false);
            var back = Button(Box("Physical Return", tablet, 813, 527, 128, 48),
                "", new Color32(36, 48, 46, 255), Pale, 16);
            back.onClick.AddListener(HandleBack);
            returnImage = back.GetComponent<UnityEngine.UI.Image>();
            Text(Box("Return Label", back.transform, 9, 10, 110, 27), "返回", 17, Pale,
                TextAlignmentOptions.Center);

            BuildContract();
            ApplySprites();
            SelectPage(CommanderTabletPage.Communications);
            Refresh();
        }

        public void Bind(CommanderTabletBindings value)
        {
            if (ContractOpen) CloseContract();
            if (bindings?.Session != null) bindings.Session.Changed -= Refresh;
            if (bindings?.Support != null) bindings.Support.ProposalChanged -= OnProposalChanged;
            if (input && input.isFocused) input.DeactivateInputField();
            onlineMode = false;
            bindings = value;
            if (bindings?.Session != null) bindings.Session.Changed += Refresh;
            if (bindings?.Support != null) bindings.Support.ProposalChanged += OnProposalChanged;
            renderedMessageCount = -1;
            if (value?.Session == null && chatContent) Clear(chatContent);
            ResetReadingState();
            bindings?.ResetPresentation?.Invoke();
            decisionMode = false;
            renderedSessionId = Guid.Empty;
            if (built) SelectPage(CommanderTabletPage.Communications);
        }

        public void ApplySprites()
        {
            SetArt(frameImage, tabletFrameSprite);
            SetArt(returnImage, physicalReturnKeySprite, new Color32(36, 48, 46, 255));
            SetArt(avatarImage, commanderSignalAvatarSprite);
            SetArt(tabIcons[0], communicationsTabSprite);
            SetArt(tabIcons[1], supportTabSprite);
            SetArt(tabIcons[2], recordsTabSprite);
        }

        public void SelectPage(CommanderTabletPage page)
        {
            if (decisionMode && page != CommanderTabletPage.Records) return;
            SelectedPage = page;
            notice = null;
            if (!built) return;
            if (page != CommanderTabletPage.Communications && input && input.isFocused)
                input.DeactivateInputField();
            commsPanel.SetActive(page == CommanderTabletPage.Communications);
            supportPanel.SetActive(page == CommanderTabletPage.Support);
            recordsPanel.SetActive(page == CommanderTabletPage.Records);
            for (int i = 0; i < tabButtons.Length; i++)
            {
                tabButtons[i].interactable = !decisionMode || i == (int)CommanderTabletPage.Records;
                tabButtons[i].GetComponent<UnityEngine.UI.Image>().color = i == (int)page
                    ? new Color32(58, 102, 72, 255) : new Color32(35, 62, 48, 255);
            }
            // Reading another page deliberately leaves the session and HTTP request alive.
            Refresh();
        }

        public void SetDecisionMode(bool value)
        {
            decisionMode = value;
            if (value) SelectPage(CommanderTabletPage.Records);
            else SelectPage(SelectedPage);
        }

        public void HandleBack()
        {
            if (ContractOpen) { CloseContract(); return; }
            if (!decisionMode && !(SelectedPage == CommanderTabletPage.Communications && onlineMode) &&
                bindings?.TryBack?.Invoke() == true) { Refresh(); return; }
            if (bindings?.CloseRequested != null) bindings.CloseRequested();
            else gameObject.SetActive(false);
        }

        // The adapter uses this for a manual Support.Request contract. Its delegates must
        // call the existing Support Confirm/Cancel and refresh this view afterwards.
        public void ShowManualContract(string unityContractText, Action accept, Action reject)
        {
            if (!built || ContractOpen || accept == null || reject == null) return;
            openProposal = null;
            manualAccept = accept;
            manualReject = reject;
            contractText.text = unityContractText ?? "合同内容暂不可用。";
            SizeContractText();
            contractStatus.text = "请核对效果、记忆、授权范围与同步度变化。";
            contractStatus.color = Amber;
            acceptButton.interactable = true;
            contractOpenedFrame = Time.frameCount;
            contractOverlay.SetActive(true);
        }

        // Call when local Support/record state changes without a session event.
        public void RefreshExternal() => Refresh();

        void BuildComms()
        {
            var root = commsPanel.transform;
            modeBar = new GameObject("Communication Mode", typeof(RectTransform));
            SetupBox(modeBar.GetComponent<RectTransform>(), root, 0, 0, 770, 32);
            localModeButton = Button(Box("Local Briefing", modeBar.transform, 0, 0, 381, 32),
                "本地简报", Edge, Pale, 17);
            onlineModeButton = Button(Box("Online Conversation", modeBar.transform, 389, 0, 381, 32),
                "在线通讯", Edge, Pale, 17);
            localModeButton.onClick.AddListener(() => { onlineMode = false; Refresh(); });
            onlineModeButton.onClick.AddListener(() => { onlineMode = true; Refresh(); });
            localPanel = new GameObject("Local Briefing", typeof(RectTransform));
            SetupBox(localPanel.GetComponent<RectTransform>(), root, 0, 0, 770, 292);
            for (int i = 0; i < localTopicButtons.Length; i++)
            {
                int slot = i;
                localTopicButtons[i] = Button(Box("Local Topic " + i, localPanel.transform,
                    i * 194, 0, 188, 32), "预设主题", Edge, Pale, 18);
                localTopicButtons[i].onClick.AddListener(() => InvokeLocalTopic(slot));
            }
            localScroll = Scroll(Box("Local Reading", localPanel.transform, 0, 40, 770, 252), out localContent);
            RegisterReading(localContent, localScroll, localPanel.transform);

            onlinePanel = new GameObject("Online Conversation", typeof(RectTransform));
            SetupBox(onlinePanel.GetComponent<RectTransform>(), root, 0, 40, 770, 252);
            chatScroll = Scroll(Box("Chat Scroll", onlinePanel.transform, 0, 0, 770, 154), out chatContent);
            waitLabel = Text(Box("Request State", onlinePanel.transform, 0, 158, 770, 22), "", 16, Amber);
            locationLabel = Text(Box("Location Reason", onlinePanel.transform, 0, 183, 770, 22), "", 16, Muted);
            input = Input(Box("Composer", onlinePanel.transform, 0, 210, 558, 42), "输入在线消息…");
            input.onValueChanged.AddListener(value =>
            {
                if (!refreshing && bindings?.Session != null) bindings.Session.Draft = value;
                RefreshControls();
            });
            sendButton = Button(Box("Send", onlinePanel.transform, 564, 210, 98, 42), "发送", Jade, Ink, 17);
            cancelButton = Button(Box("Cancel", onlinePanel.transform, 668, 210, 102, 42), "取消", Edge, Pale, 17);
            sendButton.onClick.AddListener(Send);
            cancelButton.onClick.AddListener(() => bindings?.Session?.Cancel());
            onlinePanel.SetActive(false);
        }

        void InvokeLocalTopic(int slot)
        {
            var actions = bindings?.CommunicationsActions?.Invoke();
            if (actions == null || slot >= actions.Count || actions[slot] == null ||
                !actions[slot].Enabled) return;
            actions[slot].Invoke?.Invoke();
            Refresh();
        }

        GameObject BuildReadingPage(string name, out RectTransform content)
        {
            var page = new GameObject(name, typeof(RectTransform));
            SetupBox(page.GetComponent<RectTransform>(), tablet, 159, 148, 770, 292);
            var scroll = Scroll(Box("Reading Scroll", page.transform, 0, 0, 770, 292), out content);
            RegisterReading(content, scroll, page.transform);
            return page;
        }

        void RegisterReading(RectTransform content, UnityEngine.UI.ScrollRect scroll, Transform parent)
        {
            var footer = Box("Fixed Actions", parent, 0, 244, 770, 48);
            Image(footer, Ink);
            footer.gameObject.SetActive(false);
            readingStates[content] = new ReadingState { Scroll = scroll,
                Body = scroll.GetComponent<RectTransform>(), Footer = footer,
                Back = Box("Fixed Back", parent, 0, 0, 770, 32) };
            readingStates[content].Back.gameObject.SetActive(false);
        }

        void BuildContract()
        {
            contractOverlay = new GameObject("Contract Overlay", typeof(RectTransform));
            SetupBox(contractOverlay.GetComponent<RectTransform>(), tablet, 151, 104, 786, 388);
            Image(Fill("Modal Blocker", contractOverlay.transform), new Color(0.015f, .045f, .035f, .94f));
            Image(Box("Contract Border", contractOverlay.transform, 14, 19, 758, 350), Amber);
            Image(Box("Contract Face", contractOverlay.transform, 16, 21, 754, 346), Ink);
            Text(Box("Contract Title", contractOverlay.transform, 39, 35, 605, 32),
                "支援授权合同", 24, Amber);
            var close = Button(Box("Close Contract", contractOverlay.transform, 683, 31, 65, 40),
                "关闭", Edge, Pale, 15);
            close.onClick.AddListener(CloseContract);
            var body = Scroll(Box("Contract Scroll", contractOverlay.transform, 38, 83, 710, 176),
                out var bodyContent);
            body.scrollSensitivity = 30;
            contractText = Text(StretchRow("Contract Text", bodyContent, 0), "", 19, Pale);
            var textLayout = contractText.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            textLayout.minHeight = 190;
            contractStatus = Text(Box("Contract Status", contractOverlay.transform, 40, 264, 690, 46),
                "", 16, Amber);
            acceptButton = Button(Box("Accept", contractOverlay.transform, 40, 316, 286, 44),
                "接受并执行", Amber, Ink, 19);
            acceptButton.onClick.AddListener(AcceptContract);
            var reject = Button(Box("Reject", contractOverlay.transform, 345, 316, 180, 44),
                "拒绝", Edge, Pale, 18);
            reject.onClick.AddListener(RejectContract);
            contractOverlay.SetActive(false);
        }

        void Send()
        {
            if (!onlineMode || bindings?.OnlineAvailable?.Invoke() != true ||
                bindings.Session == null || bindings.CanCompose?.Invoke() != true ||
                bindings.Session.Busy) return;
            string message = input.text.Trim();
            if (message.Length == 0) return;
            notice = null;
            bool accepted = bindings.Session.Send(message);
            (bindings.Session as CommanderSession)?.DiagnosticTrace?.Log("ui.send.return", "accepted=" + accepted + " busy=" + bindings.Session.Busy);
            Refresh();
        }

        void OpenProposal(Guid id)
        {
            if (ContractOpen || bindings?.Support == null) return;
            if (!bindings.Support.OpenContract(id, out string text, out string reason))
            {
                notice = string.IsNullOrWhiteSpace(reason) ? "支援提案已失效，请重新询问。" : reason;
                RefreshControls();
                RefreshProposalStates();
                return;
            }
            openProposal = id;
            manualAccept = manualReject = null;
            contractText.text = text ?? string.Empty;
            SizeContractText();
            contractStatus.text = "合同由当前游戏状态生成；接受前将再次核对。";
            contractStatus.color = Amber;
            acceptButton.interactable = true;
            contractOpenedFrame = Time.frameCount;
            contractOverlay.SetActive(true);
        }

        void AcceptContract()
        {
            if (!ContractOpen || !acceptButton.interactable || Time.frameCount <= contractOpenedFrame) return;
            acceptButton.interactable = false; // Block reentrant and repeated confirmation before invoking game rules.
            if (openProposal.HasValue)
            {
                string result = null;
                bool accepted = bindings?.Support != null &&
                    bindings.Support.Accept(openProposal.Value, out result);
                if (!accepted)
                {
                    contractStatus.text = string.IsNullOrWhiteSpace(result) ? "条件已变化，支援未执行。" : result;
                    contractStatus.color = Error;
                    acceptButton.interactable = false;
                    RefreshProposalStates();
                    return;
                }
            }
            else manualAccept?.Invoke();
            HideContract();
            Refresh();
        }

        void RejectContract()
        {
            if (openProposal.HasValue) bindings?.Support?.Reject(openProposal.Value);
            else manualReject?.Invoke();
            HideContract();
            Refresh();
        }

        void CloseContract()
        {
            if (!ContractOpen) return;
            if (openProposal.HasValue) bindings?.Support?.Cancel(openProposal.Value);
            else manualReject?.Invoke();
            HideContract();
            Refresh();
        }

        void HideContract()
        {
            openProposal = null;
            manualAccept = manualReject = null;
            contractOverlay.SetActive(false);
        }

        void SizeContractText()
        {
            if (!contractText) return;
            var layout = contractText.GetComponent<UnityEngine.UI.LayoutElement>();
            layout.preferredHeight = Mathf.Max(190,
                contractText.GetPreferredValues(contractText.text, 690, 0).y + 18);
        }

        void Refresh()
        {
            // The session can reset during scene exit while this tablet is hidden.
            // OpenPhone calls RefreshExternal after enabling it again.
            if (!built || refreshing || !gameObject.activeInHierarchy) return;
            refreshing = true;
            try
            {
                var session = bindings?.Session;
                if (session != null && renderedSessionId != Guid.Empty && renderedSessionId != session.SessionId)
                {
                    if (ContractOpen) CloseContract();
                    bindings.ResetPresentation?.Invoke();
                    ResetReadingState();
                    decisionMode = false;
                    SelectPage(CommanderTabletPage.Communications);
                }
                if (input && session != null && input.text != session.Draft)
                    input.SetTextWithoutNotify(session.Draft ?? string.Empty);
                connectionLabel.text = SelectedPage == CommanderTabletPage.Support ? "SHELL-LINK / 支援" :
                    SelectedPage == CommanderTabletPage.Records ? "SHELL-LINK / 记录" :
                    bindings?.ConnectionStatus?.Invoke() ?? "SHELL-LINK / 本地模式";
                healthLabel.text = bindings?.HealthStatus?.Invoke() ?? string.Empty;
                waitLabel.text = session?.Busy == true ? "正在等待在线服务" : string.Empty;
                if (session != null && (renderedMessageCount != session.Messages.Count ||
                    renderedSessionId != session.SessionId)) RenderMessages(session);
                RefreshProposalStates();
                RefreshControls();
                RefreshLocalTopics();
                if (SelectedPage == CommanderTabletPage.Communications && !onlineMode)
                    RenderReading(localContent, bindings?.LocalPage?.Invoke(), ref localPresentation);
                else if (SelectedPage == CommanderTabletPage.Support)
                    RenderReading(supportContent, bindings?.SupportPage?.Invoke(), ref supportPresentation);
                else if (SelectedPage == CommanderTabletPage.Records)
                    RenderReading(recordsContent, bindings?.RecordsPage?.Invoke(), ref recordsPresentation);
            }
            finally { refreshing = false; }
        }

        void ResetReadingState()
        {
            if (input && input.isFocused) input.DeactivateInputField();
            onlineMode = false;
            supportPresentation = recordsPresentation = localPresentation = null;
            foreach (var state in readingStates.Values) { state.Key = null; state.Positions.Clear(); }
        }

        void RefreshLocalTopics()
        {
            var actions = bindings?.CommunicationsActions?.Invoke();
            for (int i = 0; i < localTopicButtons.Length; i++)
            {
                var button = localTopicButtons[i];
                if (!button) continue;
                var action = actions != null && i < actions.Count ? actions[i] : null;
                button.gameObject.SetActive(action != null);
                if (action == null) continue;
                button.interactable = action.Enabled && action.Invoke != null;
                var label = button.GetComponentInChildren<TMP_Text>();
                if (label) label.text = action.Label;
                button.GetComponent<UnityEngine.UI.Image>().color = action.Selected ? Edge : Ink;
            }
        }

        void RefreshControls()
        {
            if (!built || input == null) return;
            bool available = bindings?.OnlineAvailable?.Invoke() == true;
            if (!available) onlineMode = false;
            if ((!onlineMode || !available) && input.isFocused) input.DeactivateInputField();
            modeBar.SetActive(available);
            onlinePanel.SetActive(onlineMode && available);
            localPanel.SetActive(!onlineMode);
            localModeButton.GetComponent<UnityEngine.UI.Image>().color = !onlineMode ? Edge : Ink;
            onlineModeButton.GetComponent<UnityEngine.UI.Image>().color = onlineMode ? Edge : Ink;
            var localRect = localPanel.GetComponent<RectTransform>();
            localRect.anchoredPosition = new Vector2(0, available ? -40 : 0);
            localRect.sizeDelta = new Vector2(770, available ? 252 : 292);
            LayoutReading(localContent, readingStates[localContent]);
            bool allowed = onlineMode && available && bindings?.CanCompose?.Invoke() == true;
            bool busy = bindings?.Session?.Busy == true;
            // Never expose a local free-form/fallback path. Keep the draft in Session.
            if (!allowed && input.isFocused) input.DeactivateInputField();
            input.gameObject.SetActive(allowed);
            sendButton.gameObject.SetActive(allowed);
            input.interactable = allowed && !busy;
            sendButton.interactable = allowed && !busy && !string.IsNullOrWhiteSpace(input.text);
            cancelButton.gameObject.SetActive(onlineMode && busy);
            locationLabel.color = string.IsNullOrWhiteSpace(notice) ? Muted : Error;
            locationLabel.text = !string.IsNullOrWhiteSpace(notice) ? notice :
                bindings?.ComposeReason?.Invoke() ?? "请到安全通讯节点输入。";
            waitLabel.text = bindings?.ConnectionStatus?.Invoke() ?? string.Empty;
        }

        void RenderMessages(ICommanderSession session)
        {
            bool stickToBottom = chatScroll.verticalNormalizedPosition < .05f;
            float oldPosition = chatScroll.verticalNormalizedPosition;
            Clear(chatContent);
            proposalButtons.Clear();
            proposalStates.Clear();
            foreach (var item in session.Messages)
            {
                var row = StretchRow("Message", chatContent, 0);
                var background = Image(row, item.Source == CommanderChatSource.Error ?
                    new Color32(67, 42, 39, 255) : item.Source == CommanderChatSource.LocalFact ?
                    new Color32(39, 57, 47, 255) : new Color32(24, 49, 39, 255));
                background.raycastTarget = false;
                string source = item.Source == CommanderChatSource.Player ? "你" :
                    item.Source == CommanderChatSource.OnlineAssistant ? "指挥官 / 在线" :
                    item.Source == CommanderChatSource.LocalFact ? "本地通讯" :
                    item.Source == CommanderChatSource.LocalTopic ? "预设主题" : "通讯提示";
                Text(Box("Source", row, 14, 8, 690, 21), source, 14,
                    item.Source == CommanderChatSource.Error ? Error : Muted);
                var body = Text(Box("Body", row, 14, 31, 716, 30), item.Text, 18, Pale);
                float bodyHeight = Mathf.Max(30, body.GetPreferredValues(item.Text, 716, 0).y + 4);
                body.GetComponent<RectTransform>().sizeDelta = new Vector2(716, bodyHeight);
                float totalHeight = 43 + bodyHeight;
                if (item.ProposalId.HasValue)
                {
                    Guid id = item.ProposalId.Value;
                    Image(Box("Proposal Edge", row, 11, totalHeight, 728, 61), Amber);
                    Image(Box("Proposal Card", row, 13, totalHeight + 2, 724, 57), Ink);
                    NativeSupportKind? kind = bindings?.ProposalKind?.Invoke(id);
                    if (kind == NativeSupportKind.Medical)
                    {
                        var icon = Image(Box("Authorization Icon", row, 20, totalHeight + 13, 32, 32),
                            Color.clear, false);
                        SetArt(icon, authorizationMedicalSprite);
                    }
                    string title = bindings?.ProposalTitle?.Invoke(id) ??
                        (kind == NativeSupportKind.Medical ? "医疗支援 / 待授权" :
                            kind == NativeSupportKind.Weakpoint ? "弱点解析 / 待授权" :
                            "支援提案 / 待授权");
                    Text(Box("Proposal Title", row, 58, totalHeight + 5, 430, 25),
                        title, 17, Amber);
                    proposalStates[id] = Text(Box("Proposal State", row, 58, totalHeight + 31, 420, 20),
                        "尚未执行", 14, Muted);
                    var button = Button(Box("View Contract", row, 550, totalHeight + 10, 178, 42),
                        "查看合同", new Color32(91, 70, 44, 255), Amber, 17);
                    button.onClick.AddListener(() => OpenProposal(id));
                    proposalButtons[id] = button;
                    totalHeight += 68;
                }
                row.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = totalHeight + 6;
            }
            var trace = (session as CommanderSession)?.DiagnosticTrace;
            trace?.Log("ui.render", "messageCount=" + session.Messages.Count + " busy=" + session.Busy);
            int firstNew = renderedSessionId == session.SessionId ? renderedMessageCount : 0;
            for (int i = firstNew; i < session.Messages.Count; i++)
                trace?.Log("ui.message", "index=" + i + " source=" + session.Messages[i].Source + " text=" + session.Messages[i].Text);
            renderedMessageCount = session.Messages.Count;
            renderedSessionId = session.SessionId;
            Canvas.ForceUpdateCanvases();
            chatScroll.verticalNormalizedPosition = stickToBottom || renderedMessageCount <= 1 ? 0f : oldPosition;
        }

        void RefreshProposalStates()
        {
            foreach (var pair in proposalButtons)
            {
                CommanderProposalState state = bindings?.Support?.GetState(pair.Key) ?? CommanderProposalState.Expired;
                pair.Value.interactable = state == CommanderProposalState.Available;
                if (proposalStates.TryGetValue(pair.Key, out TMP_Text label))
                    label.text = state == CommanderProposalState.Available ? "尚未执行 · 需要授权" :
                        state == CommanderProposalState.Viewing ? "合同待确认" :
                        state == CommanderProposalState.Executed ? "已执行" :
                        state == CommanderProposalState.Rejected ? "已拒绝" :
                        state == CommanderProposalState.Cancelled ? "已取消" :
                        state == CommanderProposalState.Failed ? "执行失败" : "已失效 · 请重新询问";
            }
        }

        void RenderReading(RectTransform content, CommanderTabletReading page, ref string presentation)
        {
            if (!content || page == null) return;
            var state = readingStates[content];
            string next = page.Key + "|" + page.Title + "|" + page.Source + "|" + page.Body + "|" + page.Footer;
            if (page.Entries != null)
                foreach (var entry in page.Entries) next += "|" + entry.Title + entry.Status + entry.Preview;
            if (page.Groups != null)
                foreach (var group in page.Groups)
                {
                    next += "|" + group.Label;
                    foreach (var action in group.Options) next += "|" + action.Label + action.Enabled + action.Selected;
                }
            if (page.Actions != null)
                foreach (var action in page.Actions) next += "|" + action.Label + action.Enabled;
            if (presentation == next) return;
            presentation = next;
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            string selectedPath = null;
            if (state.Key == page.Key && eventSystem && eventSystem.currentSelectedGameObject)
                selectedPath = RelativePath(eventSystem.currentSelectedGameObject.transform, content.parent.parent.parent);
            if (state.Key != null) state.Positions[state.Key] = state.Scroll.verticalNormalizedPosition;
            state.Key = page.Key;
            float position = state.Positions.TryGetValue(page.Key, out float saved) ? saved : 1f;
            Clear(content);
            Clear(state.Footer);
            Clear(state.Back);
            bool footer = page.Actions != null && page.Actions.Count > 0;
            state.HasBack = page.Back != null;
            state.HasActions = footer;
            state.Footer.gameObject.SetActive(footer);
            state.Back.gameObject.SetActive(state.HasBack);
            LayoutReading(content, state);
            if (page.Back != null)
            {
                var back = Button(Fill("Back to List", state.Back),
                    content == localContent ? "‹ 返回任务列表" : "‹ 返回记录目录", Ink, Jade, 17);
                var backLabel = back.GetComponentInChildren<TMP_Text>();
                backLabel.alignment = TextAlignmentOptions.MidlineLeft;
                backLabel.rectTransform.offsetMin = new Vector2(12, 0);
                back.onClick.AddListener(() => { page.Back(); RefreshExternal(); });
            }
            if (!string.IsNullOrEmpty(page.Title)) ReadingText(content, "Heading", page.Title, 22, Pale);
            if (!string.IsNullOrEmpty(page.Source)) ReadingText(content, "Source", page.Source, 16, Muted);
            if (page.Entries != null)
                foreach (var entry in page.Entries)
                {
                    bool preview = !string.IsNullOrEmpty(entry.Preview);
                    var row = StretchRow("Entry " + entry.Title, content, 40);
                    var button = Button(row, "", new Color32(24, 49, 39, 255), Pale, 20);
                    float top = preview ? 6 : 4;
                    var title = Text(Box("Title", row, 16, top, preview ? 335 : 690, 1), entry.Title, 20, Pale);
                    title.enableWordWrapping = false;
                    title.overflowMode = TextOverflowModes.Ellipsis;
                    float titleHeight = EntryTextHeight(title, 1);
                    title.rectTransform.sizeDelta = new Vector2(preview ? 335 : 690, titleHeight);
                    float height = Mathf.Max(40, top + titleHeight + 4);
                    if (preview)
                    {
                        var status = Text(Box("Status", row, 358, top, 335, 1), entry.Status, 16, Jade);
                        status.enableWordWrapping = false;
                        status.overflowMode = TextOverflowModes.Ellipsis;
                        float statusHeight = EntryTextHeight(status, 1);
                        status.rectTransform.sizeDelta = new Vector2(335, statusHeight);
                        float headerHeight = Mathf.Max(titleHeight, statusHeight);
                        float summaryTop = top + headerHeight + 4;
                        var summary = Text(Box("Preview", row, 16, summaryTop, 680, 1), entry.Preview, 18, Pale);
                        summary.overflowMode = TextOverflowModes.Ellipsis;
                        float summaryHeight = EntryTextHeight(summary, 2);
                        summary.rectTransform.sizeDelta = new Vector2(680, summaryHeight);
                        height = summaryTop + summaryHeight + 8;
                    }
                    // The layout group, accent and chevron all follow the measured text envelope.
                    RowHeight(row.gameObject, height);
                    Image(Box("Accent", row, 0, 0, 3, height), Edge, false);
                    Text(Box("Chevron", row, 707, 4, 30, height - 8), "›", 24, Jade, TextAlignmentOptions.Center);
                    button.onClick.AddListener(() => { entry.Open?.Invoke(); RefreshExternal(); });
                }
            if (page.Groups != null)
                foreach (var group in page.Groups)
                {
                    var row = StretchRow("Selection " + group.Label, content, 46);
                    RowHeight(row.gameObject, 46);
                    Text(Box("Group Label", row, 4, 4, 145, 38), group.Label, 20, Pale);
                    int count = group.Options.Count;
                    float width = (588f - (count - 1) * 8) / Mathf.Max(1, count);
                    for (int i = 0; i < count; i++)
                    {
                        var action = group.Options[i];
                        var button = Button(Box("Option " + i, row, 153 + i * (width + 8), 2, width, 42),
                            (action.Selected ? "✓ " : "") + action.Label, action.Selected ? Edge : Ink, Pale, 18);
                        WireAction(button, action);
                    }
                }
            if (!string.IsNullOrWhiteSpace(page.Body)) ReadingText(content, "Body", page.Body, 18, Pale);
            if (footer)
            {
                Image(Box("Divider", state.Footer, 0, 0, 770, 1), Edge, false);
                float start = string.IsNullOrEmpty(page.Footer) ? 0 : 400;
                if (start > 0) Text(Box("Action Note", state.Footer, 7, 6, 385, 38), page.Footer, 16, Amber);
                float width = (770 - start - (page.Actions.Count - 1) * 8) / page.Actions.Count;
                for (int i = 0; i < page.Actions.Count; i++)
                {
                    var action = page.Actions[i];
                    var button = Button(Box("Fixed Action " + i, state.Footer, start + i * (width + 8), 6, width, 42),
                        action.Label, Edge, Pale, 18);
                    WireAction(button, action);
                }
            }
            Canvas.ForceUpdateCanvases();
            state.Scroll.StopMovement();
            state.Scroll.verticalNormalizedPosition = position;
            if (selectedPath != null && eventSystem)
            {
                var selected = content.parent.parent.parent.Find(selectedPath);
                var selectable = selected ? selected.GetComponent<UnityEngine.UI.Selectable>() : null;
                eventSystem.SetSelectedGameObject(selectable && selectable.IsInteractable() ? selected.gameObject : null);
            }
        }

        static string RelativePath(Transform selected, Transform root)
        {
            if (!selected.IsChildOf(root)) return null;
            string path = selected.name;
            while (selected.parent != root)
            {
                selected = selected.parent;
                if (!selected) return null;
                path = selected.name + "/" + path;
            }
            return path;
        }

        void LayoutReading(RectTransform content, ReadingState state)
        {
            float top = content == localContent ? 40 : 0;
            float height = content == localContent ? (bindings?.OnlineAvailable?.Invoke() == true ? 212 : 252) : 292;
            state.Back.anchoredPosition = new Vector2(0, -top);
            state.Body.anchoredPosition = new Vector2(0, -top - (state.HasBack ? 36 : 0));
            state.Body.sizeDelta = new Vector2(770, height - (state.HasBack ? 36 : 0) - (state.HasActions ? 48 : 0));
        }

        // Limited to Entry slots: TMP Ellipsis can clear the whole line when its first
        // glyph exceeds the vertical box, so width ellipsis still needs safe line height.
        static float EntryTextHeight(TMP_Text text, int lines)
        {
            var face = text.font.faceInfo;
            float scale = text.fontSize / Mathf.Max(1f, face.pointSize) * face.scale;
            float fontHeight = (face.ascentLine - face.descentLine + (lines - 1) * face.lineHeight) * scale;
            string sample = lines == 1 ? text.text : "A\nA";
            float preferred = text.GetPreferredValues(sample, Mathf.Infinity, Mathf.Infinity).y;
            return Mathf.Ceil(Mathf.Max(fontHeight, preferred)) + 6;
        }

        void ReadingText(Transform parent, string name, string value, int size, Color color)
        {
            var text = Text(StretchRow(name, parent, 0), value, size, color);
            text.alignment = TextAlignmentOptions.TopLeft;
            text.lineSpacing = 4;
            RowHeight(text.gameObject, Mathf.Max(size + 6, text.GetPreferredValues(value, 736, 0).y + 4));
        }

        static void RowHeight(GameObject target, float height)
        {
            var layout = target.AddComponent<UnityEngine.UI.LayoutElement>();
            layout.minHeight = layout.preferredHeight = height;
            layout.flexibleHeight = 0;
        }

        void WireAction(UnityEngine.UI.Button button, CommanderTabletAction action)
        {
            button.interactable = action.Enabled && action.Invoke != null;
            if (!button.interactable)
            {
                button.GetComponent<UnityEngine.UI.Image>().color = Ink;
                button.GetComponentInChildren<TMP_Text>().color = Muted;
            }
            button.onClick.AddListener(() => { if (action.Enabled) action.Invoke?.Invoke(); RefreshExternal(); });
        }

        void OnProposalChanged(Guid id, CommanderProposalState state, string reason)
        {
            if (openProposal == id && state != CommanderProposalState.Viewing)
            {
                acceptButton.interactable = false;
                contractStatus.text = string.IsNullOrWhiteSpace(reason) ? "合同条件已变化。" : reason;
                contractStatus.color = Error;
            }
            RefreshProposalStates();
        }

        void Update()
        {
            if (handleEscapeLocally && UnityEngine.Input.GetKeyDown(KeyCode.Escape)) HandleBack();
            if (!tablet) return;
            var root = GetComponent<RectTransform>();
            float scale = Mathf.Min(1f, root.rect.width / 1088f, root.rect.height / 612f);
            tablet.localScale = Vector3.one * Mathf.Max(.1f, scale);
            if (Time.unscaledTime >= nextPassiveRefresh)
            {
                nextPassiveRefresh = Time.unscaledTime + .3f;
                Refresh();
            }
        }

        void OnDisable()
        {
            if (ContractOpen) CloseContract();
            bindings?.Session?.Cancel();
        }

        void OnDestroy()
        {
            if (bindings?.Session != null) bindings.Session.Changed -= Refresh;
            if (bindings?.Support != null) bindings.Support.ProposalChanged -= OnProposalChanged;
        }

        static RectTransform Fill(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        static RectTransform Box(string name, Transform parent, float x, float y, float width, float height)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            SetupBox(rect, parent, x, y, width, height);
            return rect;
        }

        static void SetupBox(RectTransform rect, Transform parent, float x, float y, float width, float height)
        {
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        static RectTransform StretchRow(string name, Transform parent, float height)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(0, height);
            return rect;
        }

        static UnityEngine.UI.Image Image(RectTransform rect, Color color, bool raycast = true)
        {
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        TMP_Text Text(RectTransform rect, string value, int size, Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
        {
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.fontSize = size;
            label.color = color;
            label.text = value ?? string.Empty;
            label.alignment = alignment;
            label.enableWordWrapping = true;
            label.overflowMode = TextOverflowModes.Truncate;
            label.richText = false;
            label.raycastTarget = false;
            return label;
        }

        UnityEngine.UI.Button Button(RectTransform rect, string label, Color background,
            Color foreground, int size)
        {
            var image = Image(rect, background);
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.disabledColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.selectedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
            button.colors = colors;
            Text(Fill("Label", rect), label, size, foreground, TextAlignmentOptions.Center);
            return button;
        }

        TMP_InputField Input(RectTransform rect, string placeholder)
        {
            Image(rect, Ink);
            var field = rect.gameObject.AddComponent<TMP_InputField>();
            var viewport = Box("Text Area", rect, 12, 4, 534, 36);
            viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            var hint = Text(Fill("Placeholder", viewport), placeholder, 17, Muted);
            var value = Text(Fill("Input Text", viewport), "", 18, Pale);
            field.textViewport = viewport;
            field.textComponent = (TextMeshProUGUI)value;
            field.placeholder = hint;
            field.lineType = TMP_InputField.LineType.SingleLine;
            field.characterLimit = 1200;
            field.customCaretColor = true;
            field.caretColor = Jade;
            return field;
        }

        static UnityEngine.UI.ScrollRect Scroll(RectTransform root, out RectTransform content)
        {
            var scroll = root.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
            var viewport = Fill("Viewport", root);
            Image(viewport, Ink);
            viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            content = Fill("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1);
            var group = content.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            group.padding = new RectOffset(8, 8, 8, 8);
            group.spacing = 4;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit =
                UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28;
            return scroll;
        }

        static void SetArt(UnityEngine.UI.Image image, Sprite sprite, Color? fallback = null)
        {
            if (!image) return;
            image.sprite = sprite;
            image.preserveAspect = true;
            image.color = sprite ? Color.white : fallback ?? Color.clear;
            image.raycastTarget = image.name == "Physical Return";
        }

        static void Clear(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                child.gameObject.SetActive(false);
                // Destroy is deferred; detach now so focus-path lookup cannot select a retired node.
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }
        }
    }
}
