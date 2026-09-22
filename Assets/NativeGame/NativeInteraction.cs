using UnityEngine;
namespace Echo.NativeGame
{
    public sealed class NativeInteraction : MonoBehaviour
    {
        public NativeRunController run;
        public bool isExit;
        public float radius = 1.65f;
        public SpriteRenderer indicator;
        public bool Used { get; private set; }
        public string Prompt => isExit ? run.TerminalActivated ? "E  /  LEAVE THE SECTOR" : "EXIT LOCKED  /  RECONNECT THE CYAN TERMINAL" : "E  /  RECONNECT TERMINAL";
        public bool CanReach(NativePlayer player) => player && Vector2.Distance(player.transform.position, transform.position) <= radius && !NativeObstacle.Blocked(player.transform.position, transform.position);
        public void Use(NativePlayer player)
        {
            if (Used || !run || !run.Running || !CanReach(player)) return;
            if (isExit && !run.TerminalActivated) return;
            Used = true;
            if (indicator) indicator.color = new Color(.35f, 1, .75f);
            if (isExit) run.ReachExit(); else run.ActivateRelay();
        }
        void OnDrawGizmosSelected() { Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, radius); }
    }
}
