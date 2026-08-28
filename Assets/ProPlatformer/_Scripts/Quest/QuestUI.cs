using UnityEngine;

namespace Myd.Platform.Quest
{
    /// <summary>
    /// 任务UI：屏幕左上角像素风任务面板
    /// 显示当前任务标题和描述
    /// </summary>
    public class QuestUI : MonoBehaviour
    {
        public static QuestUI Instance { get; private set; }

        [Header("UI样式")]
        [SerializeField] private float panelWidth = 300f;
        [SerializeField] private float marginX = 24f;
        [SerializeField] private float marginY = 24f;
        [SerializeField] private int titleFontSize = 17;
        [SerializeField] private int descFontSize = 13;

        [Header("切换动画")]
        [SerializeField] private float fadeTime = 0.4f;

        private RectTransform panelRoot;
        private UnityEngine.UI.Text titleLabel;
        private UnityEngine.UI.Text descLabel;
        private CanvasGroup canvasGroup;

        private QuestData currentQuest;

        private void Awake()
        {
            Instance = this;
            BuildUI();
        }

        private void BuildUI()
        {
            // Canvas（Screen Space Overlay）
            var canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 400; // 在对话气泡(500)之下
                gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            }

            // 面板根节点：锚定左上角
            panelRoot = new GameObject("QuestPanel").AddComponent<RectTransform>();
            panelRoot.SetParent(canvas.transform, false);
            panelRoot.anchorMin = new Vector2(0, 1);
            panelRoot.anchorMax = new Vector2(0, 1);
            panelRoot.pivot = new Vector2(0, 1);
            panelRoot.anchoredPosition = new Vector2(marginX, -marginY);
            panelRoot.sizeDelta = new Vector2(panelWidth, 0f); // 高度由文字自然撑开

            canvasGroup = panelRoot.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f; // 初始隐藏，有任务后显示

            // 无底板：只有文字，清新简约

            // 标题（暖白，小字号）
            var titleGo = new GameObject("Title");
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.SetParent(panelRoot, false);
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, 0f);
            titleRect.sizeDelta = new Vector2(0, titleFontSize + 8f);
            titleLabel = titleGo.AddComponent<UnityEngine.UI.Text>();
            titleLabel.fontSize = titleFontSize;
            titleLabel.color = new Color(0.95f, 0.95f, 0.9f, 0.95f); // 暖白
            titleLabel.alignment = TextAnchor.UpperLeft;
            titleLabel.raycastTarget = false;
            titleLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            titleLabel.verticalOverflow = VerticalWrapMode.Overflow;

            // 描述（浅灰，更小字号）
            var descGo = new GameObject("Description");
            var descRect = descGo.AddComponent<RectTransform>();
            descRect.SetParent(panelRoot, false);
            descRect.anchorMin = new Vector2(0, 1);
            descRect.anchorMax = new Vector2(1, 1);
            descRect.pivot = new Vector2(0.5f, 1);
            descRect.anchoredPosition = new Vector2(0, -(titleFontSize + 12f));
            descRect.sizeDelta = new Vector2(0, descFontSize * 3f);
            descLabel = descGo.AddComponent<UnityEngine.UI.Text>();
            descLabel.fontSize = descFontSize;
            descLabel.color = new Color(0.8f, 0.82f, 0.85f, 0.85f); // 浅灰
            descLabel.alignment = TextAnchor.UpperLeft;
            descLabel.raycastTarget = false;
            descLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            descLabel.verticalOverflow = VerticalWrapMode.Overflow;

            // 字体
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleLabel.font = font;
            descLabel.font = font;

            panelRoot.gameObject.SetActive(false);
        }

        /// <summary>
        /// 切换任务：淡出旧任务，淡入新任务
        /// </summary>
        public void SetQuest(QuestData quest)
        {
            if (quest == null || quest == currentQuest) return;
            currentQuest = quest;

            StopAllCoroutines();
            if (panelRoot.gameObject.activeSelf)
            {
                StartCoroutine(SwitchRoutine(quest));
            }
            else
            {
                panelRoot.gameObject.SetActive(true);
                ApplyQuestText(quest);
                StartCoroutine(FadeRoutine(0f, 1f));
            }
        }

        private System.Collections.IEnumerator SwitchRoutine(QuestData quest)
        {
            // 淡出旧任务
            yield return FadeRoutine(canvasGroup.alpha, 0f);
            // 换文本
            ApplyQuestText(quest);
            // 淡入新任务
            yield return FadeRoutine(0f, 1f);
        }

        private void ApplyQuestText(QuestData quest)
        {
            titleLabel.text = quest.title;
            descLabel.text = quest.description;
        }

        private System.Collections.IEnumerator FadeRoutine(float from, float to)
        {
            float t = 0;
            while (t < fadeTime)
            {
                t += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, to, t / fadeTime);
                yield return null;
            }
            canvasGroup.alpha = to;
        }

        /// <summary>
        /// 隐藏任务面板（如任务完成时）
        /// </summary>
        public void ClearQuest()
        {
            currentQuest = null;
            if (panelRoot != null && panelRoot.gameObject.activeSelf)
            {
                StopAllCoroutines();
                StartCoroutine(HideRoutine());
            }
        }

        private System.Collections.IEnumerator HideRoutine()
        {
            yield return FadeRoutine(canvasGroup.alpha, 0f);
            panelRoot.gameObject.SetActive(false);
        }
    }
}
