using System;
using UnityEngine;
namespace Echo.NativeGame
{
    public sealed class NativeInteraction : MonoBehaviour
    {
        public NativeRunController run;
        public NativeMap map;
        public bool isExit;
        public float radius = 1.65f;
        public string promptOverride;
        public SpriteRenderer indicator;
        public bool Used { get; private set; }
        public event Action<NativeInteraction> Confirmed;
        public string Prompt => !string.IsNullOrEmpty(promptOverride) ? promptOverride : isExit ? map.ExitOpen ? "E  /  离开区域" : "出口已锁定 / 请先连接青色终端" : "E  /  连接终端";
        public bool CanReach(NativePlayer player) => player && Vector2.Distance(player.transform.position, transform.position) <= radius && !NativeObstacle.Blocked(player.transform.position, transform.position);
        public void Use(NativePlayer player)
        {
            if (Used || !run || !run.Running || !CanReach(player)) return;
            Confirmed?.Invoke(this);
        }
        public void Consume()
        { if (Used) return; Used = true; if (indicator) indicator.color = new Color(.35f, 1, .75f); }
        public static NativeInteraction FindNearest(NativeInteraction[] targets, NativePlayer player)
        {
            NativeInteraction best = null; float distance = float.MaxValue;
            foreach (var target in targets)
            {
                if (!target || target.Used || !target.CanReach(player)) continue;
                float d = Vector2.Distance(target.transform.position, player.transform.position);
                if (d < distance) { distance = d; best = target; }
            }
            return best;
        }
        void OnDrawGizmosSelected() { Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, radius); }
    }
}
