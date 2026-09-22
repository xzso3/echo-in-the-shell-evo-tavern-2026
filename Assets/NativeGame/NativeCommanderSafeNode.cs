using UnityEngine;

namespace Echo.NativeGame
{
    // A level-authored place for text entry. The phone still works elsewhere through local topics.
    public sealed class NativeCommanderSafeNode : MonoBehaviour
    {
        public NativeRunController level;
        public float radius = 2.2f;
        public float enemyExclusionRadius = 5f;

        public bool CanCompose(out string reason)
        {
            if (!level || !level.Running || !level.player)
            { reason = "当前关卡状态不能发送自由通讯。"; return false; }
            if (Vector2.Distance(level.player.transform.position, transform.position) > radius)
            { reason = "请到出生点北侧的安全通讯节点输入。"; return false; }
            foreach (var enemy in NativeEnemy.Active)
            {
                if (enemy && enemy.Alive && Vector2.Distance(enemy.transform.position, level.player.transform.position) < enemyExclusionRadius)
                { reason = "敌人已接近；草稿已保留，请先脱离战斗。"; return false; }
            }
            reason = "安全节点内可输入；通讯不会暂停战斗。";
            return true;
        }

        void OnDrawGizmosSelected()
        { Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, radius); }
    }
}
