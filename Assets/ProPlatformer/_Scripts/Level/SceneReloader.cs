using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

namespace Myd.Platform
{
    /// <summary>
    /// 场景重开：按 R 键黑屏过渡后重新加载当前场景（关卡重置/卡死自救）。
    /// 常驻单例（DontDestroyOnLoad），由 PlayerRenderer.EnsureGlobalUI 自动创建，无需手动放置。
    /// UI 弹窗（Game.UIBusy）或剧情演出（Game.SceneBusy）期间不响应，避免打断密码窗/小游戏/结局序列。
    /// </summary>
    public class SceneReloader : MonoBehaviour
    {
        private static SceneReloader instance;
        private bool restarting;

        public static void Ensure()
        {
            if (instance != null) return;
            var go = new GameObject("SceneReloader");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<SceneReloader>();
        }

        private void Update()
        {
            if (restarting) return;
            if (Input.GetKeyDown(KeyCode.R))
            {
                if (Game.UIBusy || Game.SceneBusy) return;
                restarting = true;
                StartCoroutine(RestartRoutine());
            }
        }

        private IEnumerator RestartRoutine()
        {
            // 重开前强制结束对话（气泡/音效不跨场景残留）
            var dm = FindObjectOfType<Dialogue.DialogueManager>();
            if (dm != null) dm.StopDialogue();

            // 黑屏淡入（复用 SceneTransition 的黑屏 Canvas 模式）
            var go = new GameObject("SceneReloadFadeCanvas");
            DontDestroyOnLoad(go);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000; // 盖住一切（含对话气泡500）
            go.AddComponent<UnityEngine.UI.CanvasScaler>();

            var imgGo = new GameObject("FadeImage");
            var rect = imgGo.AddComponent<RectTransform>();
            rect.SetParent(canvas.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var img = imgGo.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(0, 0, 0, 0);
            img.raycastTarget = false;

            float t = 0f;
            while (t < 0.3f)
            {
                t += Time.unscaledDeltaTime;
                img.color = new Color(0, 0, 0, Mathf.Clamp01(t / 0.3f));
                yield return null;
            }
            img.color = Color.black;

            string sceneName = SceneManager.GetActiveScene().name;
            Debug.Log($"[SceneReloader] 重新进入场景: {sceneName}");
            SceneManager.LoadScene(sceneName);

            // 淡出交 SceneFadeOut 接管（挂黑屏Canvas上，等新场景角色就绪后淡出并自毁）
            go.AddComponent<SceneFadeOut>();
            SceneFadeOut.Begin(0.5f);
            restarting = false;
        }
    }
}
