using UnityEngine;

namespace Echo.NativeGame
{
    // A level-authored place for text entry. The phone still works elsewhere through local topics.
    public sealed class NativeCommanderSafeNode : MonoBehaviour
    {
        public NativeRunController level;
        public float radius = 2.2f;

        public bool CanCompose(out string reason)
        {
            if (!level || !level.Running || !level.player)
            { reason = "当前关卡状态不能发送自由通讯。"; return false; }
            Vector2 playerPosition = level.player.transform.position;
            if (level.hud && level.hud.IntegratedInput)
            {
                if (!level.hud.ActiveInputReady)
                { reason = "当前焦点关卡尚未就绪。"; return false; }
                if (level.hud.ActiveToolkitPlayer)
                    playerPosition = level.hud.ActiveToolkitPlayer.transform.position;
            }
            if (Vector2.Distance(playerPosition, transform.position) > radius)
            { reason = "请到安全通讯节点输入。"; return false; }
            reason = "安全节点内可输入。";
            return true;
        }

        void OnDrawGizmosSelected()
        { Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, radius); }
    }
}
