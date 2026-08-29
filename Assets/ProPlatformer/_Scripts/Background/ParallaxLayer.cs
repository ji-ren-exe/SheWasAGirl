using UnityEngine;

namespace Myd.Platform
{
    /// <summary>
    /// 视差背景层（增量式）。factor 1 = 跟随相机（画面上几乎不动，最远）；0 = 固定于世界（与前景同速，最近）。
    /// 画面滚动速度 = (1 - factor) × 相机速度。层可随时手动摆放，视差只叠加相机位移增量。
    /// </summary>
    [ExecuteAlways]
    public class ParallaxLayer : MonoBehaviour
    {
        [Tooltip("视差系数：1=锁相机(最远，屏速0) 0=世界固定(最近，屏速1×)")]
        [Range(0f, 1f)] public float factor = 0.5f;
        [Tooltip("只水平视差（近地中景建议开启，避免垂直脱出地面）")]
        public bool horizontalOnly = true;

        private Vector3 startPos;
        private Vector3 lastCamPos;
        private Transform cam;

        private void OnEnable()
        {
            startPos = transform.position;
            var c = Camera.main;
            if (c != null) { cam = c.transform; lastCamPos = cam.position; }
        }

        private void OnDisable()
        {
            transform.position = startPos;   // 退出播放时还原到摆放位
        }

        private void LateUpdate()
        {
            if (cam == null)
            {
                var c = Camera.main;
                if (c == null) return;
                cam = c.transform;
                lastCamPos = cam.position;
                startPos = transform.position;
            }
            Vector3 dc = cam.position - lastCamPos;
            if (dc.sqrMagnitude < 1e-12f) return;
            float dx = dc.x * factor;
            float dy = horizontalOnly ? 0f : dc.y * factor;
            transform.position += new Vector3(dx, dy, 0f);
            lastCamPos = cam.position;
        }
    }
}
