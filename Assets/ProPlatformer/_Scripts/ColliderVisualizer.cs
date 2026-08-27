using UnityEngine;

namespace Myd.Platform
{
    /// <summary>
    /// 可开关的全局碰撞箱可视化，显示场景中所有 Collider2D 和玩家自定义碰撞箱
    /// </summary>
    public class ColliderVisualizer : MonoBehaviour
    {
        [Header("碰撞箱显示开关")]
        public bool showColliders = true;
        public bool showPlayerHitbox = true;

        [Header("颜色设置")]
        public Color solidColliderColor = new Color(0f, 1f, 0f, 0.8f);    // 实体碰撞（绿）
        public Color triggerColliderColor = new Color(1f, 0.8f, 0f, 0.8f); // 触发器（黄）
        public Color playerHitboxColor = new Color(0f, 0.5f, 1f, 0.8f);   // 玩家碰撞箱（蓝）

        private void OnDrawGizmos()
        {
            if (!showColliders) return;

            // 绘制场景中所有 Collider2D
            var colliders = FindObjectsOfType<Collider2D>();
            foreach (var col in colliders)
            {
                if (col is BoxCollider2D box)
                {
                    Gizmos.color = box.isTrigger ? triggerColliderColor : solidColliderColor;
                    Gizmos.DrawWireCube(box.bounds.center, box.bounds.size);
                }
                else if (col is PolygonCollider2D poly)
                {
                    Gizmos.color = poly.isTrigger ? triggerColliderColor : solidColliderColor;
                    Gizmos.DrawWireCube(poly.bounds.center, poly.bounds.size);
                }
                else
                {
                    Gizmos.color = col.isTrigger ? triggerColliderColor : solidColliderColor;
                    Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
                }
            }

            // 绘制玩家自定义碰撞箱
            if (showPlayerHitbox && Player.Current != null)
            {
                var player = Player.Current;
                var ctrlField = typeof(Player).GetField("playerController",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (ctrlField == null) return;
                var ctrl = ctrlField.GetValue(player) as PlayerController;
                if (ctrl == null) return;

                var colliderField = typeof(PlayerController).GetField("collider",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (colliderField == null) return;
                var collider = (Rect)colliderField.GetValue(ctrl);

                Vector2 center = player.Position + collider.position + collider.size / 2f;
                Gizmos.color = playerHitboxColor;
                Gizmos.DrawWireCube(center, new Vector3(collider.size.x, collider.size.y, 0));

                // 角色位置点
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(player.Position, 0.08f);
            }
        }
    }
}
