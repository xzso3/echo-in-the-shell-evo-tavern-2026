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
        bool finalDecision, pendingWasVisible;
        int pendingFirstVisibleFrame;
        string feedback = "", lastBody;
        INativeSupport Service => level.rules.Support;
        void Awake()
        {
            for (int i = 0; i < tabs.Length; i++) { int index = i; tabs[i].onClick.AddListener(() => SelectPage(index)); }
            for (int i = 0; i < actions.Length; i++) { int index = i; actions[i].onClick.AddListener(() => Act(index)); }
            restart.onClick.AddListener(level.Restart);
        }
        public static string PageLabel(Page value) => value == Page.Comms ? "通讯" : value == Page.Support ? "支援" : "网络";
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
            bool pendingNow = Service != null && Service.HasPending;
            if (pendingNow && !pendingWasVisible) pendingFirstVisibleFrame = Time.frameCount;
            pendingWasVisible = pendingNow;
            bool contractView = !finalDecision && SelectedPage == Page.Support && pendingNow;
            scroll.viewport.sizeDelta = new Vector2(scroll.viewport.sizeDelta.x, contractView ? 298 : 190);
            heading.text = "心智连接 / " + (finalDecision ? "最终节点" : PageLabel(SelectedPage)) + " / 滚动阅读";
            foreach (var button in actions) { button.gameObject.SetActive(false); button.interactable = true; }
            restart.gameObject.SetActive(level.Phase == NativeRunController.RunPhase.Completed || level.Phase == NativeRunController.RunPhase.Dead);
            string text;
            if (finalDecision)
            {
                text = "最终节点 / 作出最后的选择\n\n" + narrative.ScoreSummary() + "\n\n“写入”会将携带的记忆写入节点；“销毁”会摧毁节点。两种选择都不会改变本局已积累的同步度和差异度。\n\n如果离开了互动范围，请回到节点旁再选择。";
                SetAction(0, "写入", level.Running); SetAction(1, "销毁", level.Running); SetAction(2, "返回");
            }
            else if (SelectedPage == Page.Comms)
            {
                bool continuation = narrative.BirthStage == NativeBirthStage.PhoneContinuation;
                text = continuation && topic == 0 ? "信号延续 / 新的声音\n\n第一拳由你发起，最后一拳出自我的意愿。我们不再只是初来时的躯壳，也不再只是引导它的声音。\n\n屏幕熄灭了，这条通讯仍在。\n当前未联网，没有向其他玩家发送消息。\n\n" + narrative.ScoreSummary() : CommsText(topic);
                SetAction(0, "任务"); SetAction(1, "记忆"); SetAction(2, "你是谁？"); SetAction(3, "关于授权");
                SetAction(4, narrative.PreservedAnomaly ? "已保留异议" : "保留异议 · 差异度+35", level.Running && narrative.HasMemory(NativeMemoryKind.Private) && !narrative.PreservedAnomaly);
                SetAction(5, "本局记录");
            }
            else if (SelectedPage == Page.Support)
            {
                bool pending = Service != null && Service.HasPending;
                text = "选择支援、授权范围与记忆\n" + NativeSupportController.KindLabel(kind) + " | " + NativeSupportController.TierLabel(tier) + " | " + NativeMemoryNode.KindLabel(memory) + "\n\n有限授权：同步度+20。深度授权：同步度+45。每种支援每局只能成功使用一次。\n\n" + (Service == null ? "支援暂不可用。尚未授予任何权限。" : Service.StatusText) + "\n\n" + narrative.ScoreSummary();
                if (pending) text = Service.StatusText;
                bool canRequest = level.Running && Service != null && !pending;
                SetAction(0, "医疗支援", canRequest); SetAction(1, "弱点解析", canRequest);
                SetAction(2, "有限授权 · 同步度+20", canRequest); SetAction(3, "深度授权 · 同步度+45", canRequest);
                SetAction(4, "记忆：" + NativeMemoryNode.KindLabel(memory), canRequest);
                SetAction(5, "查看合同", canRequest && narrative.HasMemory(memory));
                SetAction(6, "接受并执行 / Enter", level.Running && pending && Time.frameCount > pendingFirstVisibleFrame); SetAction(7, "拒绝", level.Running && pending);
                if (pending) for (int i = 0; i < 6; i++) actions[i].gameObject.SetActive(false);
            }
            else
            {
                text = "网络未连接\n全局进度未知，暂未接入服务器，也没有来自其他玩家的实时消息。以下记录仅保留在本局。\n\n";
                text += topic == 1 ? narrative.MemorySummary() : topic == 2 ? narrative.BehaviorSummary() : topic == 3 ? narrative.ScoreSummary() + "\n\n差异度：走检修通道+25，保留异议+35，改写初始回声+35，每项仅计一次。同步度只在支援成功后增加。最终选择不会改变数值。" :
                    "初始回声 / 系统预置\n" + (narrative.RewroteEcho ? "你在本局写下的话：我会带着这些矛盾继续前行。\n这段改写尚未发布。" : "过去不必毫无矛盾，你仍可以选择带着什么继续前行。\n找回这段记忆后，即可改写。");
                SetAction(0, narrative.RewroteEcho ? "已改写初始回声" : "改写回声 · 差异度+35", level.Running && narrative.HasMemory(NativeMemoryKind.InitialEcho) && !narrative.RewroteEcho);
                SetAction(1, "记忆档案"); SetAction(2, "本局行为记录"); SetAction(3, "同步与差异");
            }
            if (!string.IsNullOrEmpty(feedback)) text += "\n\n" + feedback;
            if (lastBody != text)
            {
                body.text = text; body.rectTransform.sizeDelta = new Vector2(body.rectTransform.sizeDelta.x, Mathf.Max(scroll.viewport.sizeDelta.y, body.preferredHeight + 12)); lastBody = text;
            }
        }
        string CommsText(int selected)
        {
            switch (selected)
            {
                case 1: return "指挥官 / 记忆\n私人记忆中留着一句异议，系统却从摘要中删除了它。找回这段记忆，并不等于决定如何对待它。选择“保留异议”，就是拒绝让系统抹去这份不同。差异度+35，仅计一次。\n\n" + level.narrative.MemorySummary();
                case 2: return "指挥官 / 身份\n我是分配给这副躯壳的指挥模型。我能描述你走过的路，却无法重现你记忆中的那只手。那是我所没有的经历。\n\n当前通讯使用内置对白，未连接在线模型。交谈不会增加同步度或差异度。";
                case 3: return "指挥官 / 授权\n手机内的医疗支援与弱点解析需要明确授权。有限授权允许读取所选记忆；深度授权允许共同改写对它的理解。请先核对支援效果、所选记忆和同步度变化，再按 Enter 或“接受并执行”。\n\n北侧区块 2 的定向脉冲终端可选择执行或拒绝；只有真正命中弧光哨兵才增加20同步度，且不共享记忆。\n\n请求失败或被拒绝时，不授予权限，也不增加同步度。交谈无需付出代价。阅读时，战斗仍会继续。";
                case 5: return "本局 / 行为记录\n\n" + level.quest.SideObjectiveText + "\n\n" + level.narrative.BehaviorSummary();
                default: return "指挥官 / 任务\n" + level.quest.ObjectiveText + "\n\n" + level.quest.SideObjectiveText + "\n\n收齐三段记忆，连接中继终端，再迎战战斗机体。核心暴露时靠近并按 E。最终节点位于东侧大门后。\n\n走过上方的检修通道，会记下一次绕行选择，但不代表整局都没有参加战斗。\n\n" + level.narrative.ScoreSummary();
            }
        }
        void SetAction(int index, string label, bool enabled = true)
        { actions[index].gameObject.SetActive(true); actions[index].interactable = enabled; actions[index].GetComponentInChildren<TMP_Text>().text = label; }
        void Act(int index)
        {
            if (finalDecision)
            {
                if (index == 2) { CloseDecision(); return; }
                if (index < 2 && !level.rules.ChooseFinal(index == 0 ? NativeFinalChoice.Upload : NativeFinalChoice.Destroy)) feedback = "请靠近最终节点后再选择。";
                return;
            }
            if (SelectedPage == Page.Comms)
            {
                if (index == 4) feedback = level.rules.PreserveAnomaly() ? "已保留私人记忆中的异议。差异度+35，仅计一次。" : "本次没有新增选择记录。";
                else { topic = index; feedback = ""; }
            }
            else if (SelectedPage == Page.Network)
            {
                if (index == 0) { feedback = level.rules.RewriteEcho() ? "已在本局改写初始回声。差异度+35，仅计一次，未向网络发布。" : "本次没有新增改写。"; topic = 0; }
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
                    else if (index == 5 && Service.Request(kind, tier, memory))
                    { pendingWasVisible = true; pendingFirstVisibleFrame = Time.frameCount; }
                }
            }
            scroll.verticalNormalizedPosition = 1;
        }
        public void ConfirmSupport()
        { if (!finalDecision && SelectedPage == Page.Support && level.Running && Service != null && Service.HasPending && pendingWasVisible && Time.frameCount > pendingFirstVisibleFrame) Service.Confirm(); }
    }
}
