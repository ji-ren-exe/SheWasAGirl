using UnityEngine;

namespace Myd.Platform.Dialogue
{
    /// <summary>
    /// 说话者标记：挂在场景中的角色对象上（NPC、母亲等）
    /// 编号与 DialogueBubble.speakerId 对应，气泡会出现在该角色旁边
    /// speakerId=0 固定代表玩家，场景角色从 1 开始编号
    /// </summary>
    public class DialogueSpeaker : MonoBehaviour
    {
        [Tooltip("说话者编号（1/2/3...，0 保留给玩家）")]
        public int speakerId = 1;

        [Tooltip("气泡相对角色的世界坐标偏移（Y轴，默认头顶）")]
        public float bubbleWorldOffsetY = 1.5f;

        /// <summary>
        /// 气泡锚定的世界位置
        /// </summary>
        public Vector2 GetBubbleAnchor()
        {
            return (Vector2)transform.position + Vector2.up * bubbleWorldOffsetY;
        }

        private void OnDrawGizmosSelected()
        {
            // 说话者编号可视化
            Gizmos.color = new Color(0.5f, 0.8f, 1f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * bubbleWorldOffsetY, 0.2f);
        }
    }
}
