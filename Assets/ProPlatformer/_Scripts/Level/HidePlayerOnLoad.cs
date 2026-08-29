using System.Collections;
using UnityEngine;

namespace Myd.Platform
{
    /// <summary>
    /// 场景加载后隐藏角色所有渲染器（保留控制器逻辑）
    /// 挂在场景任意对象上（如 Game），Start 协程等待 PlayerRenderer 生成后隐藏
    /// </summary>
    public class HidePlayerOnLoad : MonoBehaviour
    {
        private IEnumerator Start()
        {
            // 等待 PlayerRenderer 实例化完成（Player.Current 在构造函数赋值，
            // 但 PlayerRenderer 在 Reload() 中才从 Resources 加载，存在时间差）
            float timeout = 10f;
            PlayerRenderer pr = null;
            while (pr == null && timeout > 0f)
            {
                pr = FindObjectOfType<PlayerRenderer>();
                timeout -= Time.deltaTime;
                yield return null;
            }

            if (pr == null)
            {
                Debug.LogWarning("[HidePlayerOnLoad] PlayerRenderer 未找到，放弃隐藏");
                yield break;
            }

            // 禁用角色所有 SpriteRenderer（含子物体如残影等）
            var renderers = pr.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var r in renderers)
                r.enabled = false;

            Debug.Log($"[HidePlayerOnLoad] 已隐藏角色 {renderers.Length} 个 SpriteRenderer");

            // 持续监控：PlayerRenderer.Render() 每帧操作 sprite 但不会重新 enable，
            // 但如果角色被 Respawn 重新加载，需要再次隐藏
            StartCoroutine(KeepHidden());
        }

        private IEnumerator KeepHidden()
        {
            while (true)
            {
                var pr = FindObjectOfType<PlayerRenderer>();
                if (pr != null)
                {
                    var renderers = pr.GetComponentsInChildren<SpriteRenderer>(true);
                    foreach (var r in renderers)
                    {
                        if (r.enabled) r.enabled = false;
                    }
                }
                yield return new WaitForSeconds(0.5f);
            }
        }
    }
}
