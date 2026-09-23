using TMPro;
using UnityEngine;

namespace Echo.NativeGame
{
    // Presentation only. Inventory and effect state remain in NativeEquipment.
    public sealed class NativeEquipmentHud : MonoBehaviour
    {
        public NativeEquipment equipment;
        public TMP_Text gearLabel;
        public TMP_Text effectLabel;

        void OnEnable()
        {
            if (equipment) equipment.Changed += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            if (equipment) equipment.Changed -= Refresh;
        }

        void Update()
        {
            if (equipment && equipment.OverclockActive) Refresh();
        }

        public void Refresh()
        {
            if (!gearLabel || !effectLabel) return;
            if (!equipment || !equipment.HasCoil)
            {
                gearLabel.text = "装备 / 未获取";
                effectLabel.text = "寻找脉冲线圈";
            }
            else if (!equipment.Equipped)
            {
                gearLabel.text = "脉冲线圈 / 已拾取";
                effectLabel.text = "F 装备";
            }
            else
            {
                gearLabel.text = "脉冲线圈 / 伤害 +" + equipment.damageBonus.ToString("0.#") + "   F 卸下";
                effectLabel.text = equipment.OverclockActive
                    ? "超频生效 / " + equipment.OverclockSecondsLeft.ToString("0.0") + " 秒"
                    : "Q 超频 / " + equipment.overclockDuration.ToString("0.#") + " 秒";
            }
            effectLabel.color = equipment && equipment.OverclockActive
                ? new Color(1f, .78f, .34f)
                : new Color(.46f, .96f, .86f);
        }
    }
}
