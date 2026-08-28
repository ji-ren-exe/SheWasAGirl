using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Myd.Platform.Dialogue
{
    /// <summary>
    /// 对话管理器：在世界坐标中角色头顶显示头像气泡对话
    /// 挂在场景任意对象上，通过 DialogueTrigger 触发
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        [Header("UI设置")]
        [SerializeField] private float bubbleWidth = 240f;   // 气泡宽度（像素）
        [SerializeField] private float bubbleHeight = 90f;   // 气泡高度（像素）
        [SerializeField] private int fontSize = 20;
        [SerializeField] private float portraitSize = 64f;   // 头像尺寸（像素）
        [SerializeField] private float charRevealInterval = 0.03f; // 打字机速度
        [SerializeField] private float fadeTime = 0.25f;

        [Header("音效")]
        [Tooltip("气泡出现时的打字音效")]
        [SerializeField] private AudioClip bubbleSound;
        [Range(0f, 1f)]
        [SerializeField] private float bubbleSoundVolume = 0.6f;

        private Canvas canvas;
        private RectTransform bubbleRoot;
        private UnityEngine.UI.Image bubbleBg;
        private UnityEngine.UI.Image portraitImage;
        private UnityEngine.UI.Text textLabel;
        private UnityEngine.Coroutine playing;
        private AudioSource audioSource;
        // 当前气泡的说话者（null=玩家）
        private DialogueSpeaker currentSpeaker;

        // 已播放过的对话ID（运行时去重）
        private HashSet<string> playedIds = new HashSet<string>();

        public bool IsPlaying => playing != null;

        private void Awake()
        {
            Instance = this;
            audioSource = gameObject.GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            BuildUI();
        }

        private void BuildUI()
        {
            // 创建 Screen Space Overlay Canvas
            canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 500;
                gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
                gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }

            // 气泡根节点
            bubbleRoot = new GameObject("BubbleRoot").AddComponent<RectTransform>();
            bubbleRoot.SetParent(canvas.transform, false);
            bubbleRoot.sizeDelta = new Vector2(bubbleWidth, bubbleHeight);
            bubbleRoot.gameObject.SetActive(false);

            // 气泡背景（白色半透明圆角矩形用九宫格sprite，这里用纯色块）
            bubbleBg = bubbleRoot.gameObject.AddComponent<UnityEngine.UI.Image>();
            bubbleBg.color = new Color(1f, 1f, 1f, 0.92f);
            bubbleBg.raycastTarget = false;

            // 头像
            var portraitGo = new GameObject("Portrait");
            var portraitRect = portraitGo.AddComponent<RectTransform>();
            portraitRect.SetParent(bubbleRoot, false);
            portraitRect.anchorMin = new Vector2(0, 0.5f);
            portraitRect.anchorMax = new Vector2(0, 0.5f);
            portraitRect.pivot = new Vector2(0, 0.5f);
            portraitRect.anchoredPosition = new Vector2(6f, 0);
            portraitRect.sizeDelta = new Vector2(portraitSize, portraitSize);
            portraitImage = portraitGo.AddComponent<UnityEngine.UI.Image>();
            portraitImage.color = Color.white;
            portraitImage.preserveAspect = true;
            portraitImage.raycastTarget = false;

            // 文本
            var textGo = new GameObject("Text");
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.SetParent(bubbleRoot, false);
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.offsetMin = new Vector2(portraitSize + 14f, 6f);
            textRect.offsetMax = new Vector2(-8f, -6f);
            textLabel = textGo.AddComponent<UnityEngine.UI.Text>();
            textLabel.fontSize = fontSize;
            textLabel.color = new Color(0.15f, 0.15f, 0.15f, 1f);
            textLabel.alignment = TextAnchor.MiddleLeft;
            textLabel.raycastTarget = false;

            // 默认字体
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textLabel.font = font;
        }

        /// <summary>
        /// 播放对话（在玩家头顶显示气泡）
        /// </summary>
        public void Play(DialogueData data)
        {
            Play(data, null);
        }

        /// <summary>
        /// 播放对话（可指定默认说话者，气泡中 speakerId 匹配场景 Speaker 或回落到该默认值/玩家）
        /// </summary>
        public void Play(DialogueData data, DialogueSpeaker defaultSpeaker)
        {
            if (data == null) return;
            if (IsPlaying) return;

            // 去重：同ID对话只播一次
            if (!string.IsNullOrEmpty(data.dialogueId))
            {
                if (playedIds.Contains(data.dialogueId)) return;
                playedIds.Add(data.dialogueId);
            }

            defaultSpeakerForDialogue = defaultSpeaker;
            if (playing != null) StopCoroutine(playing);
            playing = StartCoroutine(PlayRoutine(data));
        }

        private DialogueSpeaker defaultSpeakerForDialogue;

        private IEnumerator PlayRoutine(DialogueData data)
        {
            bubbleRoot.gameObject.SetActive(true);

            foreach (var bubble in data.bubbles)
            {
                // 按编号解析说话者：0=玩家，1/2/3=场景 Speaker，未找到则回落到玩家
                currentSpeaker = ResolveSpeaker(bubble.speakerId);

                // 设置头像
                portraitImage.sprite = bubble.portrait;
                portraitImage.gameObject.SetActive(bubble.portrait != null);
                if (bubble.portrait == null)
                    textLabel.rectTransform.offsetMin = new Vector2(10f, 6f);
                else
                    textLabel.rectTransform.offsetMin = new Vector2(portraitSize + 14f, 6f);

                // 播放气泡音效：先停止上一个，避免连续气泡声音重叠
                if (audioSource != null && bubbleSound != null)
                {
                    audioSource.Stop();
                    audioSource.PlayOneShot(bubbleSound, bubbleSoundVolume);
                }

                // 打字机效果
                textLabel.text = "";
                foreach (char c in bubble.text)
                {
                    textLabel.text += c;
                    yield return new WaitForSeconds(charRevealInterval);
                }

                if (bubble.duration > 0)
                {
                    // 自动推进
                    yield return new WaitForSeconds(bubble.duration);
                }
                else
                {
                    // 按键推进（空格/回车/手柄X键）
                    yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space)
                        || Input.GetKeyDown(KeyCode.Return)
                        || Input.GetKeyDown(KeyCode.JoystickButton2));
                }
            }

            // 淡出
            yield return StartCoroutine(FadeOut());
            bubbleRoot.gameObject.SetActive(false);
            // 对话结束，停止残留音效
            if (audioSource != null && audioSource.isPlaying)
                audioSource.Stop();
            currentSpeaker = null;
            playing = null;
        }

        /// <summary>
        /// 按编号解析说话者：0=玩家；>=1 时在场景中查找对应编号的 DialogueSpeaker
        /// </summary>
        private DialogueSpeaker ResolveSpeaker(int speakerId)
        {
            if (speakerId <= 0) return null;
            var speakers = FindObjectsOfType<DialogueSpeaker>();
            foreach (var s in speakers)
            {
                if (s.speakerId == speakerId) return s;
            }
            return null; // 找不到则回落到玩家
        }

        private IEnumerator FadeOut()
        {
            float t = 0;
            var startColor = bubbleBg.color;
            while (t < fadeTime)
            {
                t += Time.deltaTime;
                float a = Mathf.Lerp(startColor.a, 0, t / fadeTime);
                bubbleBg.color = new Color(startColor.r, startColor.g, startColor.b, a);
                yield return null;
            }
            bubbleBg.color = startColor;
        }

        [Header("气泡位置")]
        [Tooltip("气泡水平偏移（像素，正=右侧，负=左侧）")]
        [SerializeField] private float bubbleOffsetX = 130f;
        [Tooltip("气泡垂直偏移（像素，正=上方）")]
        [SerializeField] private float bubbleOffsetY = 60f;

        private void LateUpdate()
        {
            if (bubbleRoot == null || !bubbleRoot.gameObject.activeSelf) return;

            // 确定气泡锚点：说话者角色（NPC）或玩家
            Vector2 anchorWorld;
            float side;
            if (currentSpeaker != null)
            {
                // NPC说话者：气泡出现在远离玩家的一侧（玩家在左→气泡在右，玩家在右→气泡在左）
                anchorWorld = currentSpeaker.GetBubbleAnchor();
                if (Player.Current != null)
                    side = Player.Current.Position.x < currentSpeaker.transform.position.x ? 1f : -1f;
                else
                    side = 1f;
            }
            else if (Player.Current != null)
            {
                // 玩家：气泡出现在面朝方向的一侧
                anchorWorld = Player.Current.Position + Vector2.up * 1.5f;
                side = (int)Player.Current.Facing;
            }
            else
            {
                return;
            }

            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(Camera.main, anchorWorld);
            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform, screenPos, canvas.worldCamera, out localPos);
            bubbleRoot.anchoredPosition = localPos + new Vector2(bubbleOffsetX * side, bubbleOffsetY);
        }
    }
}
