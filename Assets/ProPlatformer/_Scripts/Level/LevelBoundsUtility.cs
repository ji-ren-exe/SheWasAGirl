using UnityEngine;

namespace Myd.Platform
{
    /// <summary>
    /// 实时 Bounds 工具：以出生点、所有场景切换点、所有 Ground 层平台为参考自动计算关卡范围。
    /// Level.RuntimeBounds 每次读取时现算（不缓存），保证关卡重排/新增切换点后立即生效。
    /// 半屏按相机当前正交尺寸动态取值，风之力拉远后自动加宽。
    /// </summary>
    public static class LevelBoundsUtility
    {
        /// <summary>
        /// 计算实时 Bounds（出生点 + 所有 SceneTransition + 所有 Ground 平台，四周扩半屏+余量）
        /// </summary>
        public static Bounds Compute(Level level)
        {
            if (level == null) return new Bounds(Vector3.zero, new Vector3(64f, 36f, 0f));

            Vector2 spawn = level.StartPosition;

            // 基准点：出生点
            float minX = spawn.x, maxX = spawn.x;
            float minY = spawn.y, maxY = spawn.y;

            // 纳入所有场景切换点
            foreach (var t in Object.FindObjectsOfType<SceneTransition>())
            {
                Vector3 p = t.transform.position;
                if (p.x < minX) minX = p.x;
                if (p.x > maxX) maxX = p.x;
                if (p.y < minY) minY = p.y;
                if (p.y > maxY) maxY = p.y;
            }

            // 特殊取景模式：横向=出生点向左1/3屏宽 ~ 最右切换点向右1/3屏宽，纵向不限制（Y 钳制永不生效）
            if (level.spawnTransitionThirdScreenBounds)
            {
                float hw, hh;
                GetHalfView(out hw, out hh);
                float third = hw * 2f / 3f;
                float bMinX = spawn.x - third;
                float bMaxX = maxX + third;
                return new Bounds(
                    new Vector3((bMinX + bMaxX) / 2f, spawn.y, 0f),
                    new Vector3(Mathf.Max(bMaxX - bMinX, 1f), 1000f, 0f));
            }

            // 纳入所有 Ground 层实体平台（覆盖纵向关卡的爬高区域）
            // boundsFromSpawnAndTransitionsOnly=true 时跳过（装饰性大地面不参与范围）
            if (!level.boundsFromSpawnAndTransitionsOnly)
            {
                int groundMask = LayerMask.GetMask("Ground");
                foreach (var col in Object.FindObjectsOfType<Collider2D>())
                {
                    if (col.isTrigger) continue;
                    if (((1 << col.gameObject.layer) & groundMask) == 0) continue;
                    Bounds b = col.bounds;
                    if (b.min.x < minX) minX = b.min.x;
                    if (b.max.x > maxX) maxX = b.max.x;
                    if (b.min.y < minY) minY = b.min.y;
                    if (b.max.y > maxY) maxY = b.max.y;
                }
            }

            // 半屏（按当前相机正交尺寸，宽高比 16:9 换算）+ 余量
            float halfW, halfH;
            GetHalfView(out halfW, out halfH);
            float pad = 2f;

            float boundsW = (maxX - minX) + (halfW + pad) * 2f;
            float boundsCX = (minX + maxX) / 2f;

            // 每场景收缩（cameraBoundsInset.x=左收缩, .y=右收缩）
            float insetL = level.cameraBoundsInset.x;
            float insetR = level.cameraBoundsInset.y;
            if (insetL > 0 || insetR > 0)
            {
                boundsW -= (insetL + insetR);
                boundsCX += (insetL - insetR) * 0.5f;
            }

            return new Bounds(
                new Vector3(boundsCX, (minY + maxY) / 2f, 0f),
                new Vector3(
                    boundsW,
                    Mathf.Max((maxY - minY), 4f) + (halfH + pad) * 2f,
                    0f));
        }

        /// <summary>
        /// 当前相机半屏尺寸（正交）。拉远后自动变大。
        /// </summary>
        public static void GetHalfView(out float halfW, out float halfH)
        {
            var cam = Camera.main;
            halfH = cam != null ? cam.orthographicSize : 9f;
            halfW = halfH * 16f / 9f; // 宽高比换算
        }
    }
}
