using UnityEngine;
using System.Collections;

namespace Myd.Platform.Dialogue
{
    /// <summary>
    /// 对话触发器：玩家进入触发范围时播放对话
    /// 挂在场景中的空物体上，配置范围和对话数据
    /// </summary>
    public class DialogueTrigger : MonoBehaviour
    {
        [Header("对话数据")]
        [SerializeField] private DialogueData dialogue;

        [Header("触发设置")]
        [Tooltip("触发方式")]
        [SerializeField] private TriggerMode mode = TriggerMode.EnterRange;
        [Tooltip("触发范围（以本物体为中心的矩形区域，世界单位）")]
        [SerializeField] private Vector2 triggerSize = new Vector2(4f, 6f);
        [Tooltip("是否只触发一次")]
        [SerializeField] private bool triggerOnce = true;

        [Header("按键提示（KeyInRange 模式）")]
        [Tooltip("玩家进入范围时是否显示按键提示条（仅 KeyInRange 模式）")]
        [SerializeField] private bool showKeyHint = true;
        [Tooltip("提示文本")]
        [SerializeField] private string hintText = "按 X 交互";

        [Header("对话后切换场景")]
        [Tooltip("对话播放完成后自动触发场景切换")]
        [SerializeField] private bool transitionAfterDialogue = false;
        [Tooltip("目标 SceneTransition 组件（拖入场景中挂了 SceneTransition 的对象）")]
        [SerializeField] private SceneTransition targetTransition;

        public enum TriggerMode
        {
            EnterRange,     // 玩家进入范围自动触发
            KeyInRange,     // 玩家在范围内按交互键(X/手柄X)触发
            GameStart,      // 游戏开始自动触发
            Condition,      // 由其他脚本调用 Trigger() 触发
            KeyPress        // 任意位置按交互键(X/手柄X)触发，不限范围
        }

        private bool hasTriggered;

        // 提示UI（动态创建，复用 DialogueManager 的 Canvas，与 InteractableObject 同款样式）
        private RectTransform hintRoot;
        private UnityEngine.UI.Text hintLabel;

        private void Start()
        {
            StartCoroutine(BuildHintWhenReady());
        }

        private IEnumerator BuildHintWhenReady()
        {
            // 等待 DialogueManager 就绪（PlayerRenderer.EnsureGlobalUI 运行时创建）
            float timeout = 5f;
            while (DialogueManager.Instance == null && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }
            if (DialogueManager.Instance == null) yield break;
            var canvas = DialogueManager.Instance.GetComponent<Canvas>();
            if (canvas == null) yield break;

            hintRoot = new GameObject("DialogueKeyHint").AddComponent<RectTransform>();
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

            Font font = Resources.Load<Font>("NotoSansSC-Regular");
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hintLabel.font = font;
        }

        private void SetHintVisible(bool visible)
        {
            if (hintRoot == null) return;
            if (visible)
            {
                hintRoot.gameObject.SetActive(true);

                // 提示跟随触发器头顶
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
            else if (hintRoot.gameObject.activeSelf)
            {
                hintRoot.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (dialogue == null || DialogueManager.Instance == null) return;

            // KeyInRange 提示：范围内、对话未播放、未消耗一次性触发时显示
            bool hintVisible = mode == TriggerMode.KeyInRange
                && showKeyHint
                && !(triggerOnce && hasTriggered)
                && PlayerInRange()
                && !DialogueManager.Instance.IsPlaying;
            SetHintVisible(hintVisible);

            if (triggerOnce && hasTriggered) return;

            switch (mode)
            {
                case TriggerMode.GameStart:
                    Trigger();
                    break;

                case TriggerMode.EnterRange:
                    if (PlayerInRange() && !DialogueManager.Instance.IsPlaying)
                        Trigger();
                    break;

                case TriggerMode.KeyInRange:
                    if (PlayerInRange() && !DialogueManager.Instance.IsPlaying &&
                        (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.JoystickButton2)))
                        Trigger();
                    break;

                case TriggerMode.KeyPress:
                    if (!DialogueManager.Instance.IsPlaying &&
                        (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.JoystickButton2)))
                        Trigger();
                    break;
            }
        }

        /// <summary>
        /// 外部调用触发（用于条件触发）
        /// </summary>
        public void Trigger()
        {
            if (dialogue == null || hasTriggered) return;
            if (DialogueManager.Instance == null) return;

            hasTriggered = true;
            SetHintVisible(false);

            if (transitionAfterDialogue && targetTransition != null)
                StartCoroutine(PlayAndWaitForTransition());
            else
                DialogueManager.Instance.Play(dialogue);
        }

        private IEnumerator PlayAndWaitForTransition()
        {
            DialogueManager.Instance.Play(dialogue);
            // 等待对话播放完成
            yield return new WaitWhile(() => DialogueManager.Instance.IsPlaying);
            // 触发场景切换
            targetTransition.Trigger();
        }

        private bool PlayerInRange()
        {
            var player = Player.Current;
            if (player == null) return false;

            // 只比较 X/Y，忽略 Z（触发器 z 可能非 0，玩家 z=0，用 Bounds.Contains 会误判）
            Vector3 pos = transform.position;
            return Mathf.Abs(player.Position.x - pos.x) <= triggerSize.x * 0.5f
                && Mathf.Abs(player.Position.y - pos.y) <= triggerSize.y * 0.5f;
        }

        private void OnDestroy()
        {
            if (hintRoot != null)
                Destroy(hintRoot.gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 1f, 0.4f, 0.4f);
            Gizmos.DrawCube(transform.position, triggerSize);
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, triggerSize);
        }
    }
}
