using UnityEngine;

namespace Echo.NativeGame
{
    // North terminal: confirmed interaction -> live conditions -> Combat effect -> Narrative cost.
    public sealed class NativeNorthPulseEcaRules : MonoBehaviour
    {
        public NativeRunController level;
        public NativeInteraction terminal;
        public NativeNorthPulseSupport support;
        public NativeNarrative narrative;
        public NativeDialogue dialogue;

        void OnEnable()
        {
            if (!level || !terminal || !support || !narrative || !dialogue)
            { if (Application.isPlaying) Debug.LogError("North pulse: missing scene references.", this); return; }
            terminal.Confirmed += OnConfirmed;
            dialogue.ChoiceSelected += OnChoice;
        }

        void OnDisable()
        {
            if (terminal) terminal.Confirmed -= OnConfirmed;
            if (dialogue) dialogue.ChoiceSelected -= OnChoice;
        }

        bool CanChoose => level && level.Running && terminal && !terminal.Used &&
            terminal.CanReach(level.player) && support && support.CanExecute &&
            narrative && narrative.CanRecordNorthPulse;

        void OnConfirmed(NativeInteraction source)
        {
            if (source != terminal || !level || !level.Running) return;
            level.hud.ClosePhone();
            if (!CanChoose)
            { dialogue.Show("北侧定向脉冲 / 当前无法执行。\n目标必须仍在作战，且你需要站在终端旁。未扣除同步度。"); return; }
            dialogue.ShowChoices(this,
                "北侧定向脉冲 / 目标：弧光哨兵\n执行后将对目标造成最多" + support.damage.ToString("0.#") + "点真实伤害；仅在命中生效时，同步度+" + NativeNarrative.NorthPulseSyncCost + "，并在本局记录一次。\n你可以拒绝；目标已被击败或离开终端时执行会失败且不扣费。",
                "执行脉冲 · 同步度+" + NativeNarrative.NorthPulseSyncCost, "拒绝 · 无代价");
        }

        void OnChoice(UnityEngine.Object context, NativeDialogueChoice choice)
        {
            if (context != this) return;
            if (choice == NativeDialogueChoice.RouteReport)
            { dialogue.Show("北侧定向脉冲 / 已拒绝。\n未执行攻击，也未增加同步度。"); return; }
            if (!CanChoose || !support.TryExecute())
            { dialogue.Show("北侧定向脉冲 / 执行失败。\n目标或终端条件已变化；未增加同步度。"); return; }
            // TryExecute returns true only after Combat really lowers the target's health.
            if (!narrative.RecordNorthPulse())
            { Debug.LogError("North pulse hit, but Narrative could not record its cost.", this); dialogue.Show("北侧定向脉冲 / 记录异常。请查看日志。"); return; }
            terminal.Consume();
            dialogue.Show("北侧定向脉冲 / 已命中弧光哨兵。\n同步度+" + NativeNarrative.NorthPulseSyncCost + "，本局仅计一次。打开手机的通讯 / 本局记录可查看结果。");
        }
    }
}
