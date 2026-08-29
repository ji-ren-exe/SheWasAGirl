

using Myd.Platform.Core;
using UnityEngine;

namespace Myd.Platform
{

    /// <summary>
    /// Controller关于表现相关
    /// </summary>
    public partial class PlayerController
    {
        private Vector2 cameraPosition;

        protected void UpdateCamera(float deltaTime)
        {
            var from = cameraPosition;
            var target = CameraTarget;
            var multiplier = 1f;

            cameraPosition = from + (target - from) * (1f - (float)Mathf.Pow(0.01f / multiplier, deltaTime));
        }

        public Vector2 GetCameraPosition()
        {
            return cameraPosition;
        }

        protected Vector2 CameraTarget
        {
            get
            {
                Vector2 at = new Vector2();
                Vector2 target = new Vector2(this.Position.x, this.Position.y);

                // 实时 Bounds（出生点~切换点）+ 动态半屏（拉远后钳制范围自动收缩）
                Bounds b = LevelBoundsUtility.Compute(FindLevel());
                float halfW, halfH;
                LevelBoundsUtility.GetHalfView(out halfW, out halfH);

                at.x = Mathf.Clamp(target.x, b.min.x + halfW, b.max.x - halfW);
                at.y = Mathf.Clamp(target.y, b.min.y + halfH, b.max.y - halfH);

                // 视觉偏移：画面中角色偏上，场景整体下移约两个角色高度（角色高约1.1）
                at.y += 2.2f;
                return at;
            }
        }

        private Level cachedLevel;

        private Level FindLevel()
        {
            if (cachedLevel == null) cachedLevel = Object.FindObjectOfType<Level>();
            return cachedLevel;
        }
    }


}
