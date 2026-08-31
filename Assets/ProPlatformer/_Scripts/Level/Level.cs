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

        [Header("镜头取景")]
        [Tooltip("Bounds 左/右收缩量（世界单位）。正值收紧→角色在出生点偏左、切换点偏右")]
        public Vector2 cameraBoundsInset = Vector2.zero;
        [Tooltip("勾选后 Bounds 只按出生点+切换点计算，忽略 Ground 层平台（装饰性大地面不参与范围）")]
        public bool boundsFromSpawnAndTransitionsOnly = false;
        [Tooltip("勾选后关闭 Bounds 钳制：相机横向纵向均不限制，完全跟随角色")]
        public bool disableCameraBounds = false;
        [Tooltip("勾选后横向范围=出生点向左1/3屏宽 ~ 最右切换点向右1/3屏宽（出生点左侧/切换点右侧各留1/3屏可见），纵向不限制")]
        public bool spawnTransitionThirdScreenBounds = false;

        [Header("镜像场景相机（仅本场景生效）")]
        [Tooltip("勾选后：相机横向中心锁定在镜像轴上；角色通过动态缩放始终处于屏幕左1/3（镜像女儿自动处于右1/3），两人相向而行时镜头缓缓拉近")]
        public bool mirrorSceneCamera = false;
        [Tooltip("镜像轴（留空=自动取场景最右的 SceneTransition）")]
        public Transform mirrorAxis;
        [Tooltip("动态缩放下限：两人靠近时最近镜头")]
        public float mirrorMinOrtho = 3.5f;
        [Tooltip("动态缩放上限：两人最远时最广镜头（须≥出生点到轴距离×1.06，否则开局角色出画）")]
        public float mirrorMaxOrtho = 32f;

        [Header("固定镜头（仅本场景生效）")]
        [Tooltip("勾选后相机锁定在 lockTarget 中心，不跟随角色移动")]
        public bool lockCamera;
        [Tooltip("镜头锁定目标：取其 SpriteRenderer 世界包围盒中心")]
        public Transform lockTarget;

        [Header("角色缩放（仅本场景生效）")]
        [Tooltip("角色视觉缩放倍率，1=原始大小（碰撞盒不变，仅放大显示）")]
        public float playerScale = 1f;

        [Header("场景固定角色")]
        [Tooltip("本场景操作的角色：0=女儿, 1=母亲1(中年), 2=母亲2(年轻), 3=母亲3(老年), 4=母亲4(最老)")]
        public int playerCharacter = 0;

        /// <summary>
        /// 实时 Bounds：每次读取现算（出生点~所有场景切换点+半屏余量），关卡重排立即生效
        /// </summary>
        public Bounds RuntimeBounds => LevelBoundsUtility.Compute(this);

        /// <summary>
        /// 固定镜头中心：优先取 lockTarget 精灵包围盒中心，无渲染器取其位置，均缺省回落出生点
        /// </summary>
        public Vector2 GetCameraLockCenter()
        {
            if (lockTarget != null)
            {
                var renderer = lockTarget.GetComponentInChildren<SpriteRenderer>();
                if (renderer != null)
                    return renderer.bounds.center;
                return (Vector2)lockTarget.position;
            }
            return StartPosition;
        }

        private float cachedMirrorAxisX = float.NaN;

        /// <summary>
        /// 镜像轴 X：优先 mirrorAxis 指定对象；留空时取场景最右 SceneTransition（结果缓存）
        /// </summary>
        public float GetMirrorAxisX()
        {
            if (mirrorAxis != null) return mirrorAxis.position.x;
            if (float.IsNaN(cachedMirrorAxisX))
            {
                float maxX = float.MinValue;
                bool found = false;
                foreach (var t in FindObjectsOfType<SceneTransition>())
                {
                    if (!found || t.transform.position.x > maxX)
                    {
                        maxX = t.transform.position.x;
                        found = true;
                    }
                }
                cachedMirrorAxisX = found ? maxX : StartPosition.x;
            }
            return cachedMirrorAxisX;
        }

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
