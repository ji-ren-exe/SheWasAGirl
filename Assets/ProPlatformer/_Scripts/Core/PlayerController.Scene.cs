

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
                // 固定镜头（仅配置了 Level.lockCamera 的场景）：相机锁定在目标中心，不跟随角色
                Level level = FindLevel();
                if (level != null && level.lockCamera)
                    return level.GetCameraLockCenter();

                // 镜像场景相机：横向中心锁定在镜像轴（SceneTransition）上；
                // 通过动态缩放让角色始终处于屏幕左1/3（镜像女儿自动处于右1/3），两人相向而行时镜头缓缓拉近
                if (level != null && level.mirrorSceneCamera)
                {
                    float axisX = level.GetMirrorAxisX();
                    float d = Mathf.Max(axisX - this.Position.x, 0.1f);   // 角色在轴左侧的距离
                    var cam = Camera.main;
                    float aspect = cam != null ? Mathf.Max(cam.aspect, 0.01f) : 16f / 9f;
                    // 角色固定在左1/3：屏宽=3d → 半宽=1.5d → ortho=1.5d/aspect
                    float ortho = Mathf.Clamp(1.5f * d / aspect, level.mirrorMinOrtho, level.mirrorMaxOrtho);
                    if (cam != null && !Mathf.Approximately(cam.orthographicSize, ortho))
                        cam.orthographicSize = ortho;
                    // 纵向偏移随缩放等比缩放：角色在画面中的上下位置不随缩放漂移
                    return new Vector2(axisX, this.Position.y + 2.2f * (ortho / 7f));
                }

                Vector2 target = new Vector2(this.Position.x, this.Position.y);

                // 关闭 Bounds 钳制的场景：相机完全跟随角色（横向纵向均无限制）
                if (level != null && level.disableCameraBounds)
                {
                    target.y += 2.2f;
                    return target;
                }

                Vector2 at = new Vector2();

                // 实时 Bounds（出生点~切换点）+ 动态半屏（拉远后钳制范围自动收缩）
                Bounds b = LevelBoundsUtility.Compute(level);
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
