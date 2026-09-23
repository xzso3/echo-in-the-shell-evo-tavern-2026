using UnityEngine;
namespace Echo.NativeGame
{
    // One local NPC's concrete ECA: validated interaction/choice -> existing facts -> Quest action -> Dialogue.
    public sealed class NativeNpcEcaRules : MonoBehaviour
    {
        public NativeRunController level;
        public NativeLocalNpc npc;
        public NativeQuest quest;
        public NativeNarrative narrative;
        public NativeDialogue dialogue;
        void OnEnable()
        {
            if (!level || !npc || !npc.interaction || !quest || !narrative || !dialogue)
            { if (Application.isPlaying) Debug.LogError("Native NPC ECA: missing scene reference", this); return; }
            npc.interaction.Confirmed += OnInteraction; dialogue.ChoiceSelected += OnChoice;
        }
        void OnDisable()
        {
            if (npc && npc.interaction) npc.interaction.Confirmed -= OnInteraction;
            if (dialogue) dialogue.ChoiceSelected -= OnChoice;
        }
        bool CanTalk => level && level.Running && npc && npc.CanInteract && npc.interaction.CanReach(level.player);
        void OnInteraction(NativeInteraction source)
        {
            if (source != npc.interaction || !CanTalk || !quest.Active) return;
            level.hud.ClosePhone();
            if (quest.SideBranch == NativeSideBranch.None)
            {
                dialogue.ShowChoices(npc, npc.SpeakerLabel + "\n我只接收一份委托，请任选其一：\nA：找回私人记忆，提交个人记录。\nB：实际走过检修通道，提交路线报告。\n选择后不能改选；这份支线不影响主线。",
                    "选择 A：个人记录" + (narrative.HasMemory(NativeMemoryKind.Private) ? "（已有记忆）" : ""),
                    "选择 B：路线报告" + (narrative.TookBypass ? "（已有路线记录）" : ""));
                return;
            }
            if (quest.SideProgress == NativeSideProgress.Completed)
            { dialogue.Show(npc.SpeakerLabel + "\n" + quest.SideResult + "\n这份委托已经结算过了，本局不再接收另一份。你可以继续主线。"); return; }
            bool evidence = quest.SideBranch == NativeSideBranch.PrivateRecord ? narrative.HasMemory(NativeMemoryKind.Private) : narrative.TookBypass;
            if (!evidence || !quest.TrySubmitSideBranch())
            { dialogue.Show(npc.SpeakerLabel + "\n" + quest.SideObjectiveText + "\n尚未交付；找到对应事实后，再回来按 E。"); return; }
            dialogue.Show(npc.SpeakerLabel + "\n" + quest.SideResult + "\n委托已完成，仅结算一次。另一方向已锁定。同步度、差异度和支援授权不因此改变。");
            Debug.Log("Native NPC ECA: interaction + Narrative evidence -> Quest one-time submission -> Dialogue", this);
        }
        void OnChoice(UnityEngine.Object context, NativeDialogueChoice choice)
        {
            if (context != npc || !level.Running) return;
            if (!CanTalk) { dialogue.Show("请先回到本地档案员身边，再选择委托。尚未接取支线。"); return; }
            var branch = choice == NativeDialogueChoice.PrivateRecord ? NativeSideBranch.PrivateRecord : NativeSideBranch.RouteReport;
            if (!quest.TryChooseSideBranch(branch)) { dialogue.Show(quest.SideObjectiveText + "\n已选方向不能更换。"); return; }
            bool alreadyReady = quest.HasSideEvidence;
            dialogue.Show(npc.SpeakerLabel + "\n已接取：" + (branch == NativeSideBranch.PrivateRecord ? "个人记录。" : "路线报告。") + "另一方向已锁定。\n" +
                (alreadyReady ? "你此前已经达成条件，无需重复寻找或重走路线。关闭对白后，在我身边再按 E 即可提交。" : quest.SideObjectiveText) + "\n手机的通讯页可查看支线进度。");
            Debug.Log("Native NPC ECA: dialogue choice -> Quest exclusive branch", this);
        }
    }
}
