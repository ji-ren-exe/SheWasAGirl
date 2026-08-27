using UnityEngine;

namespace Myd.Platform
{
    /// <summary>
    /// 在编辑器中预览碰撞箱，不依赖 Play 模式
    /// </summary>
    public class ColliderPreviewGizmo : MonoBehaviour
    {
        [Header("碰撞箱预览（与 PlayerRenderer 中的值保持一致）")]
        public Rect normalHitbox = new Rect(0f, -0.25f, 0.27f, 1.1f);
        public Rect runHitbox = new Rect(0f, -0.25f, 0.27f, 1.1f);
        public Rect jumpHitbox = new Rect(0f, -0.25f, 0.27f, 1.1f);
        public Rect duckHitbox = new Rect(0f, -0.5f, 0.27f, 0.6f);

        private void OnDrawGizmos()
        {
            Vector2 pos = transform.position;

            // 站立碰撞箱（绿色）
            Vector2 normalCenter = pos + normalHitbox.position + normalHitbox.size / 2f;
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(normalCenter, new Vector3(normalHitbox.size.x, normalHitbox.size.y, 0));

            // 跑步碰撞箱（青色）
            Vector2 runCenter = pos + runHitbox.position + runHitbox.size / 2f;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(runCenter, new Vector3(runHitbox.size.x, runHitbox.size.y, 0));

            // 跳跃碰撞箱（蓝色）
            Vector2 jumpCenter = pos + jumpHitbox.position + jumpHitbox.size / 2f;
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(jumpCenter, new Vector3(jumpHitbox.size.x, jumpHitbox.size.y, 0));

            // 蹲伏碰撞箱（黄色）
            Vector2 duckCenter = pos + duckHitbox.position + duckHitbox.size / 2f;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(duckCenter, new Vector3(duckHitbox.size.x, duckHitbox.size.y, 0));

            // 原点标记（红色）
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(pos, 0.1f);
        }
    }
}
