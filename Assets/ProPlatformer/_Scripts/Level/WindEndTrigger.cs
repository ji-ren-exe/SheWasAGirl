using UnityEngine;

namespace Myd.Platform
{
    /// <summary>
    /// 疾风结束触发器：进入范围后结束疾风模式——
    /// 音乐停止、相机拉回正常（9）、角色运动能力恢复、风粒子/常驻残影关闭
    /// </summary>
    public class WindEndTrigger : MonoBehaviour
    {
        [Header("触发设置")]
        [SerializeField] private Vector2 triggerSize = new Vector2(3f, 5f);
        [SerializeField] private bool triggerOnce = true;

        private bool triggered;

        private void Update()
        {
            if (triggered && triggerOnce) return;
            var player = Player.Current;
            if (player == null) return;
            if (!WindPowerTrigger.IsWindActive) return; // 疾风未开启时不触发

            Vector3 pos = transform.position;
            bool inRange = Mathf.Abs(player.Position.x - pos.x) <= triggerSize.x * 0.5f
                && Mathf.Abs(player.Position.y - pos.y) <= triggerSize.y * 0.5f;
            if (!inRange) return;

            triggered = true;
            WindPowerTrigger.Deactivate();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.6f, 0.3f, 0.35f);
            Gizmos.DrawCube(transform.position, triggerSize);
            Gizmos.color = new Color(1f, 0.6f, 0.3f);
            Gizmos.DrawWireCube(transform.position, triggerSize);
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, "疾风结束");
        }
    }
}
