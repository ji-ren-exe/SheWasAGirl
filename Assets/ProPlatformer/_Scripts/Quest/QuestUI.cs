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
        [SerializeField] private float panelWidth = 360f;
        [SerializeField] private float panelHeight = 140f;
        [SerializeField] private float marginX = 14f;
        [SerializeField] private float marginY = 14f;
        [SerializeField] private int titleFontSize = 12;
        [SerializeField] private int descFontSize = 20;

        [Header("黑板贴图")]
        [SerializeField] private Sprite blackboardSprite;
        [SerializeField] private Sprite chalkLineSprite;

        [Header("切换动画")]
        [SerializeField] private float fadeTime = 0.4f;

        private RectTransform panelRoot;
        private UnityEngine.UI.Image boardImage;
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
                // 分辨率自适应：与 DialogueManager 同基准（1920x1080，宽高各半），跨分辨率大小一致
                var scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
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

            // 描述（浅粉笔色，居中，自适应字号）
            var descGo = new GameObject("Description");
            var descRect = descGo.AddComponent<RectTransform>();
            descRect.SetParent(panelRoot, false);
            descRect.anchorMin = new Vector2(0, 0.5f);
            descRect.anchorMax = new Vector2(1, 0.5f);
            descRect.pivot = new Vector2(0.5f, 0.5f);
            descRect.anchoredPosition = Vector2.zero;
            descRect.sizeDelta = new Vector2(-32f, descFontSize * 3f + 8f);
            descLabel = descGo.AddComponent<UnityEngine.UI.Text>();
            descLabel.fontSize = descFontSize;
            descLabel.resizeTextForBestFit = true;       // 自适应
            descLabel.resizeTextMinSize = 18;
            descLabel.resizeTextMaxSize = descFontSize;
            descLabel.color = new Color(0.88f, 0.9f, 0.85f, 0.92f); // 浅粉笔色
            descLabel.alignment = TextAnchor.MiddleCenter;           // 居中
            descLabel.raycastTarget = false;
            descLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            descLabel.verticalOverflow = VerticalWrapMode.Overflow;

            // 字体
            Font font = Resources.Load<Font>("NotoSansSC-Regular");
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
