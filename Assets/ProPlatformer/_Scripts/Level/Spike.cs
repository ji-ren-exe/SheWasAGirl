using UnityEngine;

namespace Myd.Platform
{
    /// <summary>
    /// 尖刺：碰到玩家造成死亡，回到最近激活的复活点
    /// 触发判定用 X/Y 距离（不用 Bounds.Contains，规避 Z 轴陷阱）
    /// </summary>
    public class Spike : MonoBehaviour
    {
        [Header("判定设置")]
        [Tooltip("判定范围（以本物体为中心的矩形，世界单位）。高度建议≥1.2：玩家跳跃掠过尖刺上空时脚底也需命中")]
        [SerializeField] private Vector2 hitSize = new Vector2(1.0f, 1.2f);
        [Tooltip("判定中心相对物体的偏移（默认物体中心；贴地尖刺建议 y+0.3 让判定覆盖地上空间）")]
        [SerializeField] private Vector2 hitOffset = new Vector2(0f, 0.3f);

        private void Update()
        {
            var player = Player.Current;
            if (player == null) return;

            Vector2 center = (Vector2)transform.position + hitOffset;
            bool hit = Mathf.Abs(player.Position.x - center.x) <= hitSize.x * 0.5f
                && Mathf.Abs(player.Position.y - center.y) <= hitSize.y * 0.5f;

            if (hit)
            {
                // 母亲4免疫尖刺死亡：只触发受击反馈（镜头震动+手柄震动），不回复活点
                if (CharacterAbilities.SpikeImmune)
                {
                    CharacterAbilities.PlayHurtFeedback();
                    return;
                }
                // 死亡路径同样触发手柄震动（所有场景通用；Xbox/桥接手柄有效）
                RumbleDriver.Play(0.9f, 0.5f);
                CheckpointManager.Instance?.RespawnPlayer();
            }
        }

        private void OnDrawGizmosSelected()
        {
            // 判定范围可视化（红色）
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.35f);
            Gizmos.DrawCube(transform.position + (Vector3)hitOffset, hitSize);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position + (Vector3)hitOffset, hitSize);
        }
    }
}
