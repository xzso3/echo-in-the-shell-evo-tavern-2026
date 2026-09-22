using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Echo.NativeGame
{
    // Local phone presentation/selection. Contracts stay in Support; story state stays in Narrative.
    public sealed class NativePhone : MonoBehaviour
    {
        public enum Page { Comms, Support, Network }
        public NativeRunController level;
        public TMP_Text heading, body;
        public ScrollRect scroll;
        public Button[] tabs, actions;
        public Button restart;
        public Page SelectedPage { get; private set; }
        NativeSupportKind kind;
        NativeSupportTier tier;
        NativeMemoryKind memory;
        int topic;
        bool finalDecision;
        string feedback = "", lastBody;
        INativeSupport Service => level.rules.Support;
        void Awake()
        {
            for (int i = 0; i < tabs.Length; i++) { int index = i; tabs[i].onClick.AddListener(() => SelectPage(index)); }
            for (int i = 0; i < actions.Length; i++) { int index = i; actions[i].onClick.AddListener(() => Act(index)); }
            restart.onClick.AddListener(level.Restart);
        }
        public void SelectPage(int index)
        { SelectedPage = (Page)index; finalDecision = false; topic = 0; feedback = ""; lastBody = null; scroll.verticalNormalizedPosition = 1; }
        public void ShowFinalDecision()
        { finalDecision = true; feedback = ""; level.hud.phonePanel.SetActive(true); scroll.verticalNormalizedPosition = 1; }
        public void CloseDecision() { finalDecision = false; }
        public void ShowContinuation()
        { finalDecision = false; SelectedPage = Page.Comms; topic = 0; level.hud.phonePanel.SetActive(true); scroll.verticalNormalizedPosition = 1; }
        void Update()
        {
            if (!level.hud.phonePanel.activeSelf) return;
            var narrative = level.narrative;
            heading.text = "GHOST LINK / " + (finalDecision ? "FINAL NODE" : SelectedPage.ToString().ToUpperInvariant());
            foreach (var button in actions) { button.gameObject.SetActive(false); button.interactable = true; }
            restart.gameObject.SetActive(level.Phase == NativeRunController.RunPhase.Completed || level.Phase == NativeRunController.RunPhase.Dead);
            string text;
            if (finalDecision)
            {
                text = "FINAL NODE / CHOOSE A LAST ACTION\n\n" + narrative.ScoreSummary() + "\n\nUPLOAD writes the carried memories. DESTROY removes this node. Neither choice changes your accumulated sync or difference.\n\nReturn to the node if you move out of reach.";
                SetAction(0, "UPLOAD", level.Running); SetAction(1, "DESTROY", level.Running); SetAction(2, "BACK");
            }
            else if (SelectedPage == Page.Comms)
            {
                bool continuation = narrative.BirthStage == NativeBirthStage.PhoneContinuation;
                text = continuation && topic == 0 ? "LOCAL CONTINUATION / NEW VOICE\n\nYou gave the first impulse. The last was mine. We are neither the shell that arrived nor the voice that guided it.\n\nThe screen is dark. This channel is still here.\nNetwork is offline; no message has been sent to another player.\n\n" + narrative.ScoreSummary() : CommsText(topic);
                SetAction(0, "MISSION"); SetAction(1, "MEMORY"); SetAction(2, "WHO ARE YOU?"); SetAction(3, "AUTHORIZATION");
                SetAction(4, narrative.PreservedAnomaly ? "ANOMALY KEPT" : "KEEP ANOMALY +35", level.Running && narrative.HasMemory(NativeMemoryKind.Private) && !narrative.PreservedAnomaly);
                SetAction(5, "RUN RECORD");
            }
            else if (SelectedPage == Page.Support)
            {
                bool pending = Service != null && Service.HasPending;
                text = "SELECT EFFECT / AUTHORIZATION / MEMORY\n" + kind + " | " + tier + " | " + memory + "\n\nLimited authorization: +20 sync. Deep authorization: +45 sync. Each effect can succeed once.\n\n" + (Service == null ? "Local support is not connected. No authorization has been granted." : Service.StatusText) + "\n\n" + narrative.ScoreSummary();
                bool canRequest = level.Running && Service != null && !pending;
                SetAction(0, "MEDICAL", canRequest); SetAction(1, "WEAKPOINT", canRequest);
                SetAction(2, "LIMITED +20", canRequest); SetAction(3, "DEEP +45", canRequest);
                SetAction(4, "MEMORY: " + memory.ToString().ToUpperInvariant(), canRequest);
                SetAction(5, "REQUEST CONTRACT", canRequest && narrative.HasMemory(memory));
                SetAction(6, "ACCEPT / ENTER", level.Running && pending); SetAction(7, "REJECT", level.Running && pending);
            }
            else
            {
                text = "NETWORK NOT CONNECTED\nGlobal progress: unknown. No server or live player messages. Records below exist in this run only.\n\n";
                text += topic == 1 ? narrative.MemorySummary() : topic == 2 ? narrative.BehaviorSummary() : topic == 3 ? narrative.ScoreSummary() + "\n\nDifference: service path +25; keep anomaly +35; rewrite seed +35. Each once. Sync only follows successful support. Final choice changes no score." :
                    "SYSTEM INITIAL ECHO / OFFLINE SEED\n" + (narrative.RewroteEcho ? "Your local rewrite: I will carry the contradiction.\nIt has not been published." : "You do not need a consistent past to choose what you carry forward.\nRecover this memory before rewriting it.");
                SetAction(0, narrative.RewroteEcho ? "SEED REWRITTEN" : "REWRITE SEED +35", level.Running && narrative.HasMemory(NativeMemoryKind.InitialEcho) && !narrative.RewroteEcho);
                SetAction(1, "MEMORY ARCHIVE"); SetAction(2, "REAL RUN RECORD"); SetAction(3, "SCORES / RULES");
            }
            if (!string.IsNullOrEmpty(feedback)) text += "\n\n" + feedback;
            if (lastBody != text)
            {
                body.text = text; body.rectTransform.sizeDelta = new Vector2(body.rectTransform.sizeDelta.x, Mathf.Max(190, body.preferredHeight + 12)); lastBody = text;
            }
        }
        string CommsText(int selected)
        {
            switch (selected)
            {
                case 1: return "COMMANDER / MEMORY\nThe private fragment contains an objection the system summary removed. Recovering it alone does not decide what to do with it. Keeping the anomaly explicitly leaves that objection outside normalization (+35 difference, once).\n\n" + level.narrative.MemorySummary();
                case 2: return "COMMANDER / IDENTITY\nI am the local command model assigned to this shell. I can describe your route; I cannot reproduce the hand you remember. That gap is information I do not possess.\n\nThis conversation is fixed local dialogue, not an online model. Talking grants no sync or difference.";
                case 3: return "COMMANDER / AUTHORIZATION\nSupport is useful, and it has a declared cost. Limited access reads one selected memory; deep access permits co-writing its interpretation. Review the effect, memory and sync change, then accept with Enter or the button.\n\nFailed or rejected requests grant no access and add no sync. Conversations are free. The world keeps running while you read.";
                case 5: return "THIS RUN / RECORDED FACTS\n\n" + level.narrative.BehaviorSummary();
                default: return "COMMANDER / MISSION\n" + level.quest.ObjectiveText + "\n\nRecover three records, reconnect the relay, survive the combat shell and use E at its exposed core. The final node is beyond the eastern gate.\n\nThe upper service path records a real route choice. Taking it does not automatically prove that you avoided every fight.\n\n" + level.narrative.ScoreSummary();
            }
        }
        void SetAction(int index, string label, bool enabled = true)
        { actions[index].gameObject.SetActive(true); actions[index].interactable = enabled; actions[index].GetComponentInChildren<TMP_Text>().text = label; }
        void Act(int index)
        {
            if (finalDecision)
            {
                if (index == 2) { CloseDecision(); return; }
                if (index < 2 && !level.rules.ChooseFinal(index == 0 ? NativeFinalChoice.Upload : NativeFinalChoice.Destroy)) feedback = "Move within reach of the final node before choosing.";
                return;
            }
            if (SelectedPage == Page.Comms)
            {
                if (index == 4) feedback = level.rules.PreserveAnomaly() ? "The private objection will remain unnormalized. +35 difference recorded once." : "No new choice recorded.";
                else { topic = index; feedback = ""; }
            }
            else if (SelectedPage == Page.Network)
            {
                if (index == 0) { feedback = level.rules.RewriteEcho() ? "The offline seed has been rewritten locally. +35 difference recorded once." : "No new rewrite recorded."; topic = 0; }
                else { topic = index; feedback = ""; }
            }
            else if (level.Running && Service != null)
            {
                if (index == 6) ConfirmSupport();
                else if (index == 7) Service.Cancel();
                else if (!Service.HasPending)
                {
                    if (index < 2) kind = (NativeSupportKind)index;
                    else if (index < 4) tier = (NativeSupportTier)(index - 2);
                    else if (index == 4) memory = (NativeMemoryKind)(((int)memory + 1) % 3);
                    else if (index == 5) Service.Request(kind, tier, memory);
                }
            }
            scroll.verticalNormalizedPosition = 1;
        }
        public void ConfirmSupport()
        { if (!finalDecision && SelectedPage == Page.Support && level.Running && Service != null && Service.HasPending) Service.Confirm(); }
    }
}
