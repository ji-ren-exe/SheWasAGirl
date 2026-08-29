using UnityEngine;

namespace Myd.Platform.Quest
{
    /// <summary>
    /// 任务UI：黑板样式任务面板（木框黑板 + 粉笔字）
    /// 标题粉笔白，下方粉笔手绘下划线，描述浅粉笔色
    /// </summary>
    public class QuestUI : MonoBehaviour
    {
        public static QuestUI Instance { get; private set; }

        [Header("UI样式")]
        [SerializeField] private float panelWidth = 320f;
        [SerializeField] private float panelHeight = 150f;
        [SerializeField] private float marginX = 24f;
        [SerializeField] private float marginY = 24f;
        [SerializeField] private int titleFontSize = 18;
        [SerializeField] private int descFontSize = 13;

        [Header("黑板贴图")]
        [SerializeField] private Sprite blackboardSprite;
        [SerializeField] private Sprite chalkLineSprite;

        [Header("切换动画")]
        [SerializeField] private float fadeTime = 0.4f;

        private RectTransform panelRoot;
        private UnityEngine.UI.Image boardImage;
        private UnityEngine.UI.Image chalkLineImage;
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
            // 贴图兜底：动态创建时序列化字段为空，自动加载
            if (blackboardSprite == null)
                blackboardSprite = Resources.Load<Sprite>("Blackboard");
            if (chalkLineSprite == null)
                chalkLineSprite = Resources.Load<Sprite>("ChalkLine");

            // Canvas（Screen Space Overlay）
            var canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 400; // 在对话气泡(500)之下
                gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            }

            // 黑板根节点：锚定左上角，固定尺寸（贴图原生比例 320x150）
            panelRoot = new GameObject("QuestPanel").AddComponent<RectTransform>();
            panelRoot.SetParent(canvas.transform, false);
            panelRoot.anchorMin = new Vector2(0, 1);
            panelRoot.anchorMax = new Vector2(0, 1);
            panelRoot.pivot = new Vector2(0, 1);
            panelRoot.anchoredPosition = new Vector2(marginX, -marginY);
            panelRoot.sizeDelta = new Vector2(panelWidth, panelHeight);

            canvasGroup = panelRoot.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f; // 初始隐藏，有任务后显示

            // 黑板底图（Image 拉伸填充，保持原生像素）
            var boardGo = new GameObject("Board");
            var boardRect = boardGo.AddComponent<RectTransform>();
            boardRect.SetParent(panelRoot, false);
            boardRect.anchorMin = Vector2.zero;
            boardRect.anchorMax = Vector2.one;
            boardRect.offsetMin = Vector2.zero;
            boardRect.offsetMax = Vector2.zero;
            boardImage = boardGo.AddComponent<UnityEngine.UI.Image>();
            boardImage.sprite = blackboardSprite;
            boardImage.type = UnityEngine.UI.Image.Type.Simple;
            boardImage.preserveAspect = false;
            boardImage.raycastTarget = false;

            // 标题（粉笔白，居中，自动缩字号适应宽度）
            var titleGo = new GameObject("Title");
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.SetParent(panelRoot, false);
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -16f);
            titleRect.sizeDelta = new Vector2(-32f, titleFontSize + 8f);
            titleLabel = titleGo.AddComponent<UnityEngine.UI.Text>();
            titleLabel.fontSize = titleFontSize;
            titleLabel.resizeTextForBestFit = true;      // 自适应：字号在范围内自动缩小
            titleLabel.resizeTextMinSize = 10;
            titleLabel.resizeTextMaxSize = titleFontSize;
            titleLabel.color = new Color(0.97f, 0.96f, 0.92f, 1f); // 粉笔白
            titleLabel.alignment = TextAnchor.UpperCenter;          // 居中
            titleLabel.raycastTarget = false;
            titleLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            titleLabel.verticalOverflow = VerticalWrapMode.Overflow;

            // 粉笔下划线（标题下方，手绘风，居中）
            var lineGo = new GameObject("ChalkLine");
            var lineRect = lineGo.AddComponent<RectTransform>();
            lineRect.SetParent(panelRoot, false);
            lineRect.anchorMin = new Vector2(0.5f, 1);
            lineRect.anchorMax = new Vector2(0.5f, 1);
            lineRect.pivot = new Vector2(0.5f, 1);
            lineRect.anchoredPosition = new Vector2(0, -(titleFontSize + 24f));
            lineRect.sizeDelta = new Vector2(200f, 6f);
            chalkLineImage = lineGo.AddComponent<UnityEngine.UI.Image>();
            chalkLineImage.sprite = chalkLineSprite;
            chalkLineImage.preserveAspect = true;
            chalkLineImage.raycastTarget = false;

            // 描述（浅粉笔色，居中，自适应字号）
            var descGo = new GameObject("Description");
            var descRect = descGo.AddComponent<RectTransform>();
            descRect.SetParent(panelRoot, false);
            descRect.anchorMin = new Vector2(0, 1);
            descRect.anchorMax = new Vector2(1, 1);
            descRect.pivot = new Vector2(0.5f, 1);
            descRect.anchoredPosition = new Vector2(0, -(titleFontSize + 36f));
            descRect.sizeDelta = new Vector2(-32f, descFontSize * 3f + 8f);
            descLabel = descGo.AddComponent<UnityEngine.UI.Text>();
            descLabel.fontSize = descFontSize;
            descLabel.resizeTextForBestFit = true;       // 自适应
            descLabel.resizeTextMinSize = 9;
            descLabel.resizeTextMaxSize = descFontSize;
            descLabel.color = new Color(0.88f, 0.9f, 0.85f, 0.92f); // 浅粉笔色
            descLabel.alignment = TextAnchor.UpperCenter;           // 居中
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
