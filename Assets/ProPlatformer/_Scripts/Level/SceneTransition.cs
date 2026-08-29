using UnityEngine;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Myd.Platform
{
    /// <summary>
    /// 场景切换触发器：玩家进入范围后黑屏淡入→切换→新场景黑屏淡出
    /// 场景名填 Build Settings 中的名称或 Assets 相对路径
    /// </summary>
    public class SceneTransition : MonoBehaviour
    {
        [Header("目标场景")]
        [Tooltip("目标场景名（Build Settings 中的名称，如 Main）")]
        [SerializeField] private string targetSceneName = "Main";

        [Header("触发设置")]
        [Tooltip("触发范围（以本物体为中心的矩形，世界单位）")]
        [SerializeField] private Vector2 triggerSize = new Vector2(2f, 4f);
        [Tooltip("切换前是否保留目标场景已有内容（Additive）——一般不用")]
        [SerializeField] private bool additive = false;
        [Tooltip("需要按X键交互才触发（而非自动触发）")]
        [SerializeField] private bool requireInteraction = false;
        [Tooltip("交互提示文本（requireInteraction=true 时显示）")]
        [SerializeField] private string hintText = "按 X 交互";
        [Tooltip("仅允许外部脚本调用 Trigger() 触发，禁用 Update 自动/按键检测")]
        [SerializeField] private bool externalTriggerOnly = false;

        [Header("黑屏过渡")]
        [Tooltip("黑屏淡入时长（触发→全黑）")]
        [SerializeField] private float fadeInDuration = 0.4f;
        [Tooltip("新场景黑屏淡出时长（全黑→清晰）")]
        [SerializeField] private float fadeOutDuration = 0.5f;

        private bool triggered;
        private RectTransform hintRoot;
        private UnityEngine.UI.Text hintLabel;

        private void Start()
        {
            if (requireInteraction) BuildHintUI();
        }

        private void BuildHintUI()
        {
            // 查找任意 Canvas（DialogueManager 的 Canvas 或自建）
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                var go = new GameObject("SceneTransitionCanvas");
                canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 500;
                go.AddComponent<UnityEngine.UI.CanvasScaler>();
            }

            hintRoot = new GameObject("TransitionHint").AddComponent<RectTransform>();
            hintRoot.SetParent(canvas.transform, false);
            hintRoot.sizeDelta = new Vector2(120f, 34f);
            hintRoot.gameObject.SetActive(false);

            var bg = hintRoot.gameObject.AddComponent<UnityEngine.UI.Image>();
            bg.color = new Color(0f, 0f, 0f, 0.6f);
            bg.raycastTarget = false;

            var textGo = new GameObject("HintText");
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.SetParent(hintRoot, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(4f, 2f);
            textRect.offsetMax = new Vector2(-4f, -2f);
            hintLabel = textGo.AddComponent<UnityEngine.UI.Text>();
            hintLabel.fontSize = 18;
            hintLabel.color = Color.white;
            hintLabel.alignment = TextAnchor.MiddleCenter;
            hintLabel.text = hintText;
            hintLabel.raycastTarget = false;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            hintLabel.font = font;
        }

        private void Update()
        {
            if (triggered || externalTriggerOnly) return;
            var player = Player.Current;
            if (player == null) return;

            // 只比较 X/Y（忽略 Z，规避 Bounds 三维误判）
            Vector3 pos = transform.position;
            bool inRange = Mathf.Abs(player.Position.x - pos.x) <= triggerSize.x * 0.5f
                && Mathf.Abs(player.Position.y - pos.y) <= triggerSize.y * 0.5f;

            if (requireInteraction)
            {
                // 显示/隐藏提示
                if (inRange) ShowHint(); else HideHint();

                // 按X键触发
                if (inRange && (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.JoystickButton2)))
                {
                    triggered = true;
                    HideHint();
                    StartCoroutine(TransitionRoutine());
                }
            }
            else
            {
                if (inRange)
                {
                    triggered = true;
                    StartCoroutine(TransitionRoutine());
                }
            }
        }

        /// <summary>
        /// 外部调用触发场景切换（如对话结束后由 DialogueTrigger 调用）
        /// </summary>
        public void Trigger()
        {
            if (triggered) return;
            triggered = true;
            HideHint();
            StartCoroutine(TransitionRoutine());
        }

        private void ShowHint()
        {
            if (hintRoot == null) return;
            hintRoot.gameObject.SetActive(true);

            var cam = Camera.main;
            if (cam == null) return;
            Vector2 worldPos = transform.position + Vector3.up * 1.2f;
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
            var canvas = hintRoot.GetComponentInParent<Canvas>();
            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform, screenPos, canvas.worldCamera, out localPos);
            hintRoot.anchoredPosition = localPos;
        }

        private void HideHint()
        {
            if (hintRoot != null && hintRoot.gameObject.activeSelf)
                hintRoot.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (hintRoot != null)
                Destroy(hintRoot.gameObject);
        }

        private IEnumerator TransitionRoutine()
        {
            if (string.IsNullOrEmpty(targetSceneName))
            {
                Debug.LogError("[SceneTransition] targetSceneName 未设置");
                yield break;
            }

            // ===== 阶段1：黑屏淡入 =====
            Canvas canvas = CreateFadeCanvas();
            GameObject fadeGo = null;
            UnityEngine.UI.Image fadeImg = null;
            if (canvas != null)
            {
                fadeGo = canvas.gameObject;
                fadeImg = CreateFadeImage(canvas);
                // 淡入：alpha 0 → 1
                float t = 0f;
                while (t < fadeInDuration)
                {
                    t += Time.unscaledDeltaTime;
                    if (fadeImg != null) fadeImg.color = new Color(0, 0, 0, Mathf.Clamp01(t / fadeInDuration));
                    yield return null;
                }
                if (fadeImg != null) fadeImg.color = Color.black;
            }

            Debug.Log($"[SceneTransition] 切换到场景: {targetSceneName}");

            // ===== 阶段2：加载场景 =====
            // DontDestroyOnLoad 让黑屏跨场景存活
            if (fadeGo != null) Object.DontDestroyOnLoad(fadeGo);
            if (additive)
                SceneManager.LoadScene(targetSceneName, LoadSceneMode.Additive);
            else
                SceneManager.LoadScene(targetSceneName);

            // 加载后，由挂载在黑屏 Canvas 上的 SceneFadeOut 接管淡出
            // （触发器协程随旧场景销毁，不能在新场景里继续 yield）
            if (fadeGo != null)
            {
                fadeGo.AddComponent<SceneFadeOut>();
                SceneFadeOut.Begin(fadeOutDuration);
            }
        }

        /// <summary>
        /// 跨场景黑屏淡出管理器：挂在与 DontDestroyOnLoad Canvas 同对象上。
        /// 在新场景等待角色就绪（Player.Current != null）+ 缓冲几帧后再淡出，
        /// 确保新场景的 GameStart 对话/任务等开场事件发生在黑屏之后，
        /// 不会被黑屏遮住而"无法退出"。
        /// </summary>
        private class SceneFadeOut : MonoBehaviour
        {
            private float duration = 0.5f;
            private bool begun;
            private float waited;

            public static void Begin(float dur)
            {
                var inst = Object.FindObjectOfType<SceneFadeOut>();
                if (inst != null) { inst.duration = dur; inst.begun = true; }
            }

            private IEnumerator Start()
            {
                var img = GetComponentInChildren<UnityEngine.UI.Image>();

                // 等待新场景角色加载（Player 在 Game.Start 协程 yield 后创建）
                while (Player.Current == null && waited < 10f)
                {
                    waited += Time.unscaledDeltaTime;
                    yield return null;
                }

                // 角色就绪后再等几帧，让开场事件（GameStart 对话/任务触发/相机定位）完成首帧
                yield return null;
                yield return null;
                yield return null;

                // 淡出：alpha 1 → 0
                float t = 0f;
                while (t < duration)
                {
                    t += Time.unscaledDeltaTime;
                    if (img != null) img.color = new Color(0, 0, 0, Mathf.Clamp01(1f - t / duration));
                    yield return null;
                }

                // 清理跨场景对象
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 创建顶层黑屏 Canvas（DontDestroyOnLoad，跨场景）
        /// </summary>
        private Canvas CreateFadeCanvas()
        {
            var go = new GameObject("SceneFadeCanvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000; // 盖住一切（含对话气泡500/黑板400）
            go.AddComponent<UnityEngine.UI.CanvasScaler>();
            return canvas;
        }

        /// <summary>
        /// 创建全屏黑色 Image（初始透明）
        /// </summary>
        private UnityEngine.UI.Image CreateFadeImage(Canvas canvas)
        {
            var imgGo = new GameObject("FadeImage");
            var rect = imgGo.AddComponent<RectTransform>();
            rect.SetParent(canvas.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var img = imgGo.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(0, 0, 0, 0); // 初始全透明
            img.raycastTarget = false;
            return img;
        }

        private void OnDrawGizmosSelected()
        {
            // 触发范围可视化（橙黄色=自动触发，青色=按键交互）
            var c = requireInteraction ? new Color(0.3f, 0.8f, 1f) : new Color(1f, 0.75f, 0.1f);
            Gizmos.color = new Color(c.r, c.g, c.b, 0.3f);
            Gizmos.DrawCube(transform.position, triggerSize);
            Gizmos.color = c;
            Gizmos.DrawWireCube(transform.position, triggerSize);

            // 标注目标场景名
            var label = requireInteraction ? $"→ {targetSceneName} (按X)" : $"→ {targetSceneName}";
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, label);
        }
    }
}
