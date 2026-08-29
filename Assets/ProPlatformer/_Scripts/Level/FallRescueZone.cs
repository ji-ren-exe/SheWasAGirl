using System.Collections.Generic;
using UnityEngine;

namespace Myd.Platform
{
    /// <summary>
    /// 坠落救援：角色掉到所有平台包络之下时，
    /// 传送回当前 X 处最高平台的上方（不掉关卡进度，也省去找复活点的需求）。
    /// 挂在场景任意对象（如 Game）。仅扫描 Ground 层的平台。
    /// </summary>
    public class FallRescueZone : MonoBehaviour
    {
        [Header("救援设置")]
        [Tooltip("低于最低平台多少触发传送")]
        [SerializeField] private float triggerBelow = 2.5f;
        [Tooltip("传送落点在平台上方的高度")]
        [SerializeField] private float respawnAbove = 1.5f;
        [Tooltip("扫描的层（默认 Ground）")]
        [SerializeField] private LayerMask platformMask = -1;
        [Tooltip("是否重置速度")]
        [SerializeField] private bool resetVelocity = true;

        // 缓存平台数据（场景静态，每 2 秒刷新一次即可）
        private List<(float x, float y, float w)> platforms = new List<(float, float, float)>();
        private float envelopeMinY;
        private float refreshTimer;
        private bool maskInitialized;

        private void Start()
        {
            if (platformMask.value == -1 || platformMask.value == 0)
                platformMask = LayerMask.GetMask("Ground");
            maskInitialized = true;
            RefreshPlatforms();
        }

        private void RefreshPlatforms()
        {
            platforms.Clear();
            envelopeMinY = float.MaxValue;
            if (platformMask.value == 0) platformMask = LayerMask.GetMask("Ground");

            foreach (var col in Object.FindObjectsOfType<Collider2D>())
            {
                if (((1 << col.gameObject.layer) & platformMask.value) == 0) continue;
                if (col.isTrigger) continue;
                // 只算"平台"型：横向为主的碰撞体
                var b = col.bounds;
                if (b.size.x < 0.5f) continue;
                platforms.Add((b.center.x, b.max.y, b.size.x)); // y 用碰撞体顶面（可站立面）
                if (b.min.y < envelopeMinY) envelopeMinY = b.min.y;
            }
        }

        private void Update()
        {
            var player = Player.Current;
            if (player == null) return;

            // 定期刷新平台缓存（关卡静态时低频即可）
            refreshTimer -= Time.deltaTime;
            if (refreshTimer <= 0)
            {
                refreshTimer = 2f;
                RefreshPlatforms();
            }
            if (platforms.Count == 0) return;

            // 低于包络下界 + 触发余量 → 救援
            if (player.Position.y > envelopeMinY - triggerBelow) return;

            // 找当前 X 处最高平台
            float px = player.Position.x;
            float topY = float.MinValue;
            bool found = false;
            foreach (var p in platforms)
            {
                if (px >= p.x - p.w / 2f && px <= p.x + p.w / 2f && p.y > topY)
                {
                    topY = p.y;
                    found = true;
                }
            }

            if (!found)
            {
                // 当前 X 无平台（缝隙）：找最近平台的 X
                float bestDist = float.MaxValue;
                foreach (var p in platforms)
                {
                    float d = Mathf.Abs(p.x - px);
                    if (d < bestDist) { bestDist = d; topY = p.y; found = true; }
                }
            }
            if (!found) return;

            // 传送：重置状态并放到平台上方
            Vector2 rescuePos = new Vector2(px, topY + respawnAbove);
            var ctrlField = typeof(Player).GetField("playerController",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var ctrl = ctrlField?.GetValue(player) as PlayerController;
            if (ctrl == null) return;

            ctrl.Respawn(rescuePos);
            if (resetVelocity) ctrl.Speed = Vector2.zero;
            Debug.Log($"[FallRescue] 角色坠落救援 → ({rescuePos.x:F1}, {rescuePos.y:F1})（该列最高平台上方）");
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.4f, 0.3f);
            // 画包络下界线（运行时值，编辑器示意）
            Gizmos.DrawWireCube(transform.position, Vector3.one);
        }
    }
}
