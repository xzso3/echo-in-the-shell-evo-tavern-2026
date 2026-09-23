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

        public CommanderTabletAction(string label, Action invoke, bool enabled = true)
        {
            Label = label ?? string.Empty;
            Invoke = invoke;
            Enabled = enabled;
        }
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
        public Func<string> SupportText;
        public Func<IReadOnlyList<CommanderTabletAction>> SupportActions;
        public Func<string> RecordsText;
        public Func<IReadOnlyList<CommanderTabletAction>> RecordsActions;
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
        RectTransform supportContent, recordsContent;
        Guid? openProposal;
        Action manualAccept, manualReject;
        string notice;
        bool refreshing, built, decisionMode;
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
            Image(Box("Status Bar", tablet, 159, 112, 770, 27), Ink);
            connectionLabel = Text(Box("AI Connection", tablet, 171, 115, 500, 22),
                "AI 通讯 / 待接线", 15, Jade);
            healthLabel = Text(Box("Health", tablet, 673, 115, 242, 22), "", 15, Pale,
                TextAlignmentOptions.MidlineRight);
            Image(Box("Identity Divider", tablet, 159, 188, 770, 2), Edge);
            avatarImage = Image(Box("Commander Avatar", tablet, 166, 139, 54, 49), Color.clear, false);
            Text(Box("Identity", tablet, 224, 141, 400, 24), "指挥官 / SHELL-LINK", 21, Pale);
            waitLabel = Text(Box("Wait State", tablet, 646, 143, 273, 21), "", 15, Amber,
                TextAlignmentOptions.MidlineRight);

            commsPanel = new GameObject("Communications", typeof(RectTransform));
            SetupBox(commsPanel.GetComponent<RectTransform>(), tablet, 159, 192, 770, 249);
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
            bindings = value;
            if (bindings?.Session != null) bindings.Session.Changed += Refresh;
            if (bindings?.Support != null) bindings.Support.ProposalChanged += OnProposalChanged;
            renderedMessageCount = -1;
            if (value?.Session == null && chatContent) Clear(chatContent);
            supportPresentation = recordsPresentation = null;
            if (built) Refresh();
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
            commsPanel.SetActive(page == CommanderTabletPage.Communications);
            supportPanel.SetActive(page == CommanderTabletPage.Support);
            recordsPanel.SetActive(page == CommanderTabletPage.Records);
            for (int i = 0; i < tabButtons.Length; i++)
            {
                tabButtons[i].interactable = !decisionMode || i == (int)CommanderTabletPage.Records;
                tabButtons[i].GetComponent<UnityEngine.UI.Image>().color = i == (int)page
                    ? new Color32(58, 102, 72, 255) : new Color32(35, 62, 48, 255);
            }
            if (page != CommanderTabletPage.Communications && input && input.isFocused)
                input.DeactivateInputField();
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
            contractOverlay.SetActive(true);
        }

        // Call when local Support/record state changes without a session event.
        public void RefreshExternal() => Refresh();

        void BuildComms()
        {
            var root = commsPanel.transform;
            Image(Box("Chat Background", root, 0, 0, 770, 127), Ink);
            chatScroll = Scroll(Box("Chat Scroll", root, 4, 4, 762, 119), out chatContent);
            for (int i = 0; i < localTopicButtons.Length; i++)
            {
                int slot = i;
                localTopicButtons[i] = Button(Box("Local Topic " + i, root,
                    (i % 2) * 389, 130 + (i / 2) * 27, 381, 24),
                    "预设主题", new Color32(35, 62, 48, 255), Pale, 14);
                localTopicButtons[i].onClick.AddListener(() => InvokeLocalTopic(slot));
            }
            locationLabel = Text(Box("Location Reason", root, 0, 184, 770, 18), "", 14, Muted);
            input = Input(Box("Composer", root, 0, 202, 558, 44), "输入消息；离线时仅有本地反馈…");
            input.onValueChanged.AddListener(value =>
            {
                if (!refreshing && bindings?.Session != null) bindings.Session.Draft = value;
                RefreshControls();
            });
            sendButton = Button(Box("Send", root, 564, 202, 98, 44), "发送", Jade, Ink, 17);
            cancelButton = Button(Box("Cancel", root, 668, 202, 102, 44), "取消", Edge, Pale, 17);
            sendButton.onClick.AddListener(Send);
            cancelButton.onClick.AddListener(() => bindings?.Session?.Cancel());
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
            SetupBox(page.GetComponent<RectTransform>(), tablet, 159, 192, 770, 249);
            Image(Box("Reading Background", page.transform, 0, 0, 770, 249), Ink);
            Scroll(Box("Reading Scroll", page.transform, 5, 5, 760, 239), out content);
            return page;
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
            var body = Scroll(Box("Contract Scroll", contractOverlay.transform, 38, 83, 710, 203),
                out var bodyContent);
            body.scrollSensitivity = 30;
            contractText = Text(StretchRow("Contract Text", bodyContent, 0), "", 19, Pale);
            var textLayout = contractText.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            textLayout.minHeight = 190;
            contractStatus = Text(Box("Contract Status", contractOverlay.transform, 40, 291, 690, 20),
                "", 14, Amber);
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
            if (bindings?.Session == null || bindings.CanCompose?.Invoke() != true ||
                bindings.Session.Busy) return;
            string message = input.text.Trim();
            if (message.Length == 0) return;
            notice = null;
            bindings.Session.Send(message);
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
            contractOverlay.SetActive(true);
        }

        void AcceptContract()
        {
            if (!ContractOpen) return;
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
                if (input && session != null && input.text != session.Draft)
                    input.SetTextWithoutNotify(session.Draft ?? string.Empty);
                connectionLabel.text = bindings?.ConnectionStatus?.Invoke() ?? "AI 通讯 / 未接线";
                healthLabel.text = bindings?.HealthStatus?.Invoke() ?? string.Empty;
                waitLabel.text = session?.Busy == true ? "正在等待在线服务" : string.Empty;
                if (session != null && (renderedMessageCount != session.Messages.Count ||
                    renderedSessionId != session.SessionId)) RenderMessages(session);
                RefreshProposalStates();
                RefreshControls();
                RefreshLocalTopics();
                if (SelectedPage == CommanderTabletPage.Support)
                    RenderReading(supportContent, bindings?.SupportText?.Invoke() ?? "手动支援尚未接线。",
                        bindings?.SupportActions?.Invoke(), ref supportPresentation);
                else if (SelectedPage == CommanderTabletPage.Records)
                    RenderReading(recordsContent, bindings?.RecordsText?.Invoke() ?? "本局记录尚未接线。",
                        bindings?.RecordsActions?.Invoke(), ref recordsPresentation);
            }
            finally { refreshing = false; }
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
            }
        }

        void RefreshControls()
        {
            if (!built || input == null) return;
            bool allowed = bindings?.CanCompose?.Invoke() == true;
            bool busy = bindings?.Session?.Busy == true;
            input.interactable = allowed && !busy;
            sendButton.interactable = allowed && !busy && !string.IsNullOrWhiteSpace(input.text);
            cancelButton.gameObject.SetActive(busy);
            locationLabel.color = string.IsNullOrWhiteSpace(notice) ? Muted : Error;
            locationLabel.text = !string.IsNullOrWhiteSpace(notice) ? notice :
                (bindings?.ComposeReason?.Invoke() ??
                    (allowed ? "安全节点内可发送。" : "请到安全通讯节点输入；支援与记录仍可使用。"));
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
                    Image(Box("Proposal Edge", row, 11, totalHeight, 739, 61), Amber);
                    Image(Box("Proposal Card", row, 13, totalHeight + 2, 735, 57), Ink);
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

        void RenderReading(RectTransform content, string body,
            IReadOnlyList<CommanderTabletAction> actions, ref string presentation)
        {
            if (!content) return;
            if (string.IsNullOrWhiteSpace(body)) body = "本局尚无可显示的记录。";
            string next = body;
            if (actions != null)
                foreach (var action in actions)
                    if (action != null) next += "\n" + action.Label + ":" + action.Enabled;
            if (presentation == next) return;
            presentation = next;
            var scroll = content.parent
                ? content.parent.GetComponentInParent<UnityEngine.UI.ScrollRect>() : null;
            float position = scroll ? scroll.verticalNormalizedPosition : 1f;
            Clear(content);
            var text = Text(StretchRow("Body", content, 0), body, 18, Pale);
            text.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight =
                Mathf.Max(60, text.GetPreferredValues(body, 720, 0).y + 18);
            if (actions != null)
                foreach (var action in actions)
                {
                    if (action == null) continue;
                    var button = Button(StretchRow("Action", content, 42), action.Label,
                        new Color32(44, 80, 59, 255), Pale, 17);
                    button.interactable = action.Enabled && action.Invoke != null;
                    var captured = action;
                    button.onClick.AddListener(() => { captured.Invoke?.Invoke(); RefreshExternal(); });
                }
            Canvas.ForceUpdateCanvases();
            if (scroll) scroll.verticalNormalizedPosition = position;
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
            label.overflowMode = TextOverflowModes.Overflow;
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
            viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            content = Fill("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1);
            var group = content.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            group.padding = new RectOffset(7, 7, 7, 7);
            group.spacing = 8;
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
            image.color = sprite ? Color.white : fallback ?? Color.clear;
            image.raycastTarget = image.name == "Physical Return";
        }

        static void Clear(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }
    }
}
