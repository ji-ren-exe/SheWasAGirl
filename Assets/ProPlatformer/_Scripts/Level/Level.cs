using System;
using System.Collections.Generic;
using UnityEngine;

namespace Myd.Platform
{
    public class Level : MonoBehaviour
    {
        public int levelId;

        [Obsolete("改用 RuntimeBounds：实时按出生点+切换点计算，此序列化字段仅作编辑器预览参考")]
        public Bounds Bounds;

        public Vector2 StartPosition;

        /// <summary>
        /// 实时 Bounds：每次读取现算（出生点~所有场景切换点+半屏余量），关卡重排立即生效
        /// </summary>
        public Bounds RuntimeBounds => LevelBoundsUtility.Compute(this);

        public void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(RuntimeBounds.center, RuntimeBounds.size);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(StartPosition, 0.5f);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // 出生点常显指示：绿色向下箭头 + 站位横线（不需选中也可见）
            Vector3 p = StartPosition;
            Vector3 groundLine = p + Vector3.down * 1.4f;
            Gizmos.color = new Color(0.2f, 1f, 0.3f, 0.9f);
            Gizmos.DrawLine(p, groundLine);
            Vector3 right = Quaternion.Euler(0, 0, 45) * Vector3.up;
            Vector3 left = Quaternion.Euler(0, 0, -45) * Vector3.up;
            Gizmos.DrawLine(groundLine, groundLine + right * 0.4f);
            Gizmos.DrawLine(groundLine, groundLine + left * 0.4f);
            Gizmos.DrawLine(groundLine + Vector3.left * 0.6f, groundLine + Vector3.right * 0.6f);
            // 角色头顶高度参考线
            Gizmos.color = new Color(0.2f, 1f, 0.3f, 0.25f);
            Gizmos.DrawLine(p + Vector3.up * 2.8f + Vector3.left * 0.6f, p + Vector3.up * 2.8f + Vector3.right * 0.6f);
        }
#endif
    }
}
