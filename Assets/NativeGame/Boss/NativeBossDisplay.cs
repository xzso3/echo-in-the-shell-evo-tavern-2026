using TMPro;
using UnityEngine;

namespace Echo.NativeGame
{
    // Boss-owned presentation only. Scene HUD/ECA owns input and progression.
    public sealed class NativeBossDisplay : MonoBehaviour
    {
        public NativeBossController boss;
        public Canvas canvas;
        public TMP_Text title, cue;
        public RectTransform armorFill;
        void LateUpdate()
        {
            if (!boss || !canvas) return;
            canvas.enabled = boss.Stage != NativeBossController.AttackStage.Dormant && boss.level && boss.level.Running;
            if (!canvas.enabled) return;
            if (armorFill) armorFill.anchorMax = new Vector2(boss.ArmorFraction, 1);
            if (title) title.text = boss.IsDefeated ? "战斗机体 / 已失效" : "战斗机体 / 外壳 " + Mathf.CeilToInt(boss.ArmorFraction * 100) + "%";
            if (cue) cue.text = boss.Cue + (boss.Stage == NativeBossController.AttackStage.CoreWindow ? "  " + boss.CoreSecondsRemaining.ToString("0.0") + "秒" : "");
        }
    }
}
