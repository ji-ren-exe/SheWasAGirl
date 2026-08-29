using UnityEngine;

namespace Myd.Platform
{
    /// <summary>
    /// 复活点：玩家经过后成为当前激活复活点；死亡后回到这里
    /// 按 Y 最高的已激活复活点优先
    /// </summary>
    public class Checkpoint : MonoBehaviour
    {
        [Header("触发设置")]
        [Tooltip("触发范围（以本物体为中心的矩形，世界单位）")]
        [SerializeField] private Vector2 triggerSize = new Vector2(2.5f, 4f);
        [Tooltip("复活位置偏移（相对本物体，默认物体位置）")]
        [SerializeField] private Vector2 respawnOffset = Vector2.zero;

        [Header("表现（可选）")]
        [Tooltip("激活后的精灵（无则不变）")]
        [SerializeField] private SpriteRenderer activeVisual;

        private bool activated;

        private void Update()
        {
            if (activated) return;
            var player = Player.Current;
            if (player == null) return;

            Vector3 pos = transform.position;
            bool inRange = Mathf.Abs(player.Position.x - pos.x) <= triggerSize.x * 0.5f
                && Mathf.Abs(player.Position.y - pos.y) <= triggerSize.y * 0.5f;

            if (inRange)
            {
                activated = true;
                CheckpointManager.Instance?.ActivateCheckpoint(this);
                if (activeVisual != null) activeVisual.enabled = true;
            }
        }

        public Vector2 GetRespawnPosition()
        {
            return (Vector2)transform.position + respawnOffset;
        }

        private void OnDrawGizmosSelected()
        {
            // 触发范围可视化（青色）
            Gizmos.color = new Color(0.2f, 0.9f, 0.9f, 0.3f);
            Gizmos.DrawCube(transform.position, triggerSize);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, triggerSize);

            // 复活点标记
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere((Vector2)transform.position + respawnOffset, 0.25f);
        }
    }
}
