using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Myd.Platform
{
    /// <summary>
    /// 保险箱开启交互（保险箱 新）：
    /// 玩家靠近提示"按 X 打开"→ 按X弹出密码输入窗口（可退出）→
    /// 密码正确后依次播放：对话"妈妈居然..."(2s) → 隐藏的秘密特写(2s) →
    /// "那到底为什么"(1s) → "我一定要找她问清楚"(1s) → 切换到 scence3_1。
    /// 密码来源：SafeController.Combination——母亲庭院保险箱设置过密码则用新密码，否则默认 0516。
    /// </summary>
    public class SafeUnlocker : MonoBehaviour
    {
        [Header("交互设置")]
        [Tooltip("交互范围（以本物体为中心的矩形，世界单位）")]
        [SerializeField] private Vector2 interactRange = new Vector2(4f, 4f);
        [Tooltip("交互提示文本")]
        [SerializeField] private string hintText = "按 X 打开";

        [Header("结局流程")]
        [Tooltip("隐藏的秘密特写贴图")]
        [SerializeField] private Texture2D secretTexture;
        [Tooltip("密码正确后进入的场景")]
        [SerializeField] private string targetSceneName = "scence3_1";
        [Tooltip("结局对话资产（3条气泡：[0]妈妈居然 [1]那到底为什么 [2]我一定要找她问清楚）")]
        [SerializeField] private Dialogue.DialogueData endingDialogue;

        private bool hasInteracted;
        private RectTransform hintRoot;
        private Text hintLabel;

        private void Start()
        {
            StartCoroutine(DelayedBuildHintUI());
        }

        private IEnumerator DelayedBuildHintUI()
        {
            float timeout = 5f;
            // 等 DialogueManager 的画布——不能用 FindObjectOfType<Canvas>()：
            // 经场景切换进入时过渡黑屏 Canvas（DontDestroyOnLoad，淡出后自毁）会被抢先命中，
            // 提示条挂上去后随其销毁，导致提示永远不弹
            while (Dialogue.DialogueManager.Instance == null && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }
            BuildHintUI();
        }

        private void BuildHintUI()
        {
            var dm = Dialogue.DialogueManager.Instance;
            if (dm == null) return;
            Canvas canvas = dm.GetComponent<Canvas>();
            if (canvas == null) return;

            hintRoot = new GameObject("SafeHint").AddComponent<RectTransform>();
            hintRoot.SetParent(canvas.transform, false);
            hintRoot.sizeDelta = new Vector2(180f, 51f);
            hintRoot.gameObject.SetActive(false);

            var bg = hintRoot.gameObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.6f);
            bg.raycastTarget = false;

            var textGo = new GameObject("HintText");
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.SetParent(hintRoot, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(6f, 3f);
            textRect.offsetMax = new Vector2(-6f, -3f);
            hintLabel = textGo.AddComponent<Text>();
            hintLabel.fontSize = 27;
            hintLabel.color = Color.white;
            hintLabel.alignment = TextAnchor.MiddleCenter;
            hintLabel.text = hintText;
            hintLabel.raycastTarget = false;

            Font font = Resources.Load<Font>("NotoSansSC-Regular");
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hintLabel.font = font;
        }

        private void Update()
        {
            if (hasInteracted) { HideHint(); return; }
            var player = Player.Current;
            if (player == null) return;

            Vector3 pos = transform.position;
            bool inRange = Mathf.Abs(player.Position.x - pos.x) <= interactRange.x * 0.5f
                && Mathf.Abs(player.Position.y - pos.y) <= interactRange.y * 0.5f;

            // 滞空时不显示提示也不可交互（着地才能开保险箱）
            bool onGround = IsPlayerOnGround();
            if (inRange) { if (onGround) ShowHint(); else HideHint(); }
            else HideHint();

            if (inRange && onGround && (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.JoystickButton2)))
            {
                OpenPasswordWindow();
            }
        }

        // 玩家是否在地面（反射取 PlayerController.OnGround）
        private bool IsPlayerOnGround()
        {
            var ctrlField = typeof(Player).GetField("playerController",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (ctrlField == null || Player.Current == null) return true;
            var ctrl = ctrlField.GetValue(Player.Current) as PlayerController;
            return ctrl == null ? true : ctrl.OnGround;
        }

        private void OpenPasswordWindow()
        {
            var windowGo = new GameObject("SafePasswordWindow");
            var window = windowGo.AddComponent<SafePasswordWindow>();
            window.Show(
                confirmCallback: (pwd) =>
                {
                    if (pwd == SafeController.Combination)
                    {
                        // 密码正确：开箱轻震（Xbox/桥接手柄有效）
                        RumbleDriver.Play(0.45f, 0.25f);
                        StartCoroutine(SuccessSequence());
                    }
                    else
                    {
                        // 密码错误：提示并可重试
                        ShowWrongPasswordToast();
                        hasInteracted = false;
                    }
                },
                cancelCallback: (pwd) => { hasInteracted = false; },
                titleText: "输入密码"
            );
            hasInteracted = true;
            HideHint();
        }

        // 密码错误轻提示（非阻断，2s 后消失）
        private void ShowWrongPasswordToast()
        {
            if (hintRoot != null)
            {
                hintLabel.text = "密码错误";
                hintRoot.gameObject.SetActive(true);
                StartCoroutine(HideToastAfter(2f));
            }
        }

        private IEnumerator HideToastAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            hintLabel.text = hintText;
            if (!hasInteracted) HideHint();
        }

        // ===== 密码正确后的剧情序列 =====
        private IEnumerator SuccessSequence()
        {
            // 剧情模式：跳过角色更新但时间正常流动（SceneBusy）——
            // 对话气泡用 scaled time，若用 UIBusy 冻结时间，气泡永不结束、剧情卡死
            Game.SceneBusy = true;

            // 1. 对话"妈妈居然..." 2s（画面正中央独白）
            yield return ShowBubble(0);

            // 2. 隐藏的秘密特写 4s（全屏居中黑边展示）
            yield return ShowSecretCloseup(4f);

            // 3. "那到底为什么" 1s —— 最后两句独白直接显示在画面正中央
            yield return ShowBubble(1);

            // 4. "我一定要找她问清楚" 1s
            yield return ShowBubble(2);

            Game.SceneBusy = false;

            // 5. 黑屏淡入 → 切场景 → 新场景淡出
            yield return TransitionToScene(targetSceneName);
        }

        // 借用 DialogueManager 播放结局对话资产中的单条气泡（按索引取）
        private IEnumerator ShowBubble(int bubbleIndex)
        {
            var dm = FindObjectOfType<Dialogue.DialogueManager>();
            if (dm != null && endingDialogue != null && bubbleIndex < endingDialogue.bubbles.Count)
            {
                var src = endingDialogue.bubbles[bubbleIndex];
                // 动态构造单条气泡的临时 DialogueData（保留原文/时长/位置模式）
                var data = ScriptableObject.CreateInstance<Dialogue.DialogueData>();
                data.dialogueId = $"ending_{bubbleIndex}";
                var bubble = new Dialogue.DialogueBubble();
                bubble.text = src.text;
                bubble.duration = src.duration;
                bubble.speakerId = src.speakerId;
                data.bubbles = new System.Collections.Generic.List<Dialogue.DialogueBubble> { bubble };
                data.bubblePosition = endingDialogue.bubblePosition;
                dm.Play(data, null);
                yield return new WaitWhile(() => dm.IsPlaying);
            }
            else
            {
                // 资产未配置时的兜底：按索引用默认文本
                string[] fallback = { "妈妈居然...", "那到底为什么", "我一定要找她问清楚" };
                float[] fallbackDur = { 2f, 1f, 1f };
                if (bubbleIndex < fallback.Length)
                    yield return new WaitForSeconds(fallbackDur[bubbleIndex]);
            }
        }

        // 全屏特写：贴图居中放大展示，黑底，持续 duration 秒（unscaled）
        private IEnumerator ShowSecretCloseup(float duration)
        {
            var go = new GameObject("SecretCloseupCanvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 700;

            var imgGo = new GameObject("SecretImage");
            var rect = imgGo.AddComponent<RectTransform>();
            rect.SetParent(canvas.transform, false);
            var img = imgGo.AddComponent<RawImage>();
            img.texture = secretTexture;
            img.raycastTarget = false;

            // 居中 + 尽量放大（保留宽高比，留 8% 边距）
            if (secretTexture != null)
            {
                float texAspect = (float)secretTexture.width / secretTexture.height;
                float screenAspect = (float)Screen.width / Screen.height;
                float w, h;
                if (texAspect > screenAspect) { w = Screen.width * 0.92f; h = w / texAspect; }
                else { h = Screen.height * 0.92f; w = h * texAspect; }
                rect.sizeDelta = new Vector2(w, h);
            }
            else
            {
                rect.sizeDelta = new Vector2(600f, 400f);
            }

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            Destroy(go);
        }

        // 黑屏过渡进新场景（复用 SceneTransition 的三阶段模式）
        private IEnumerator TransitionToScene(string sceneName)
        {
            // 黑屏淡入
            var go = new GameObject("SafeFadeCanvas");
            Object.DontDestroyOnLoad(go);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;
            go.AddComponent<CanvasScaler>();

            var imgGo = new GameObject("FadeImage");
            var rect = imgGo.AddComponent<RectTransform>();
            rect.SetParent(canvas.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var img = imgGo.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0);
            img.raycastTarget = false;

            float t = 0f;
            while (t < 0.4f)
            {
                t += Time.unscaledDeltaTime;
                img.color = new Color(0, 0, 0, Mathf.Clamp01(t / 0.4f));
                yield return null;
            }
            img.color = Color.black;

            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);

            // 本协程宿主（SafeUnlocker）随旧场景卸载销毁而中止，
            // 淡出必须由挂在 DontDestroyOnLoad 黑屏 Canvas 上的 SceneFadeOut 接管，
            // 否则黑屏永不淡出（卡死黑屏即此 bug）
            go.AddComponent<SceneFadeOut>();
            SceneFadeOut.Begin(0.5f);
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
            if (hintRoot != null) Destroy(hintRoot.gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.3f);
            Gizmos.DrawCube(transform.position, interactRange);
            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawWireCube(transform.position, interactRange);
        }
    }
}
