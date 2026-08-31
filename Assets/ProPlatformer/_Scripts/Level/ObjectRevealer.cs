using UnityEngine;
using System.Collections;

namespace Myd.Platform
{
    /// <summary>
    /// 按键交互后激活指定物体（们）。
    /// 挂在可交互对象上，玩家靠近按X键后激活 targets 列表中的物体。
    /// 交互前 targets 默认隐藏（SetActive(false)）。
    /// </summary>
    public class ObjectRevealer : MonoBehaviour
    {
        [Header("交互设置")]
        [Tooltip("交互范围（以本物体为中心的矩形，世界单位）")]
        [SerializeField] private Vector2 interactRange = new Vector2(3f, 4f);
        [Tooltip("是否只能交互一次")]
        [SerializeField] private bool interactOnce = true;
        [Tooltip("交互提示文本")]
        [SerializeField] private string hintText = "按 X 交互";

        [Header("激活目标")]
        [Tooltip("交互后激活的物体列表")]
        [SerializeField] private GameObject[] targets;

        private bool hasInteracted;
        private bool playerInRange;
        private RectTransform hintRoot;
        private UnityEngine.UI.Text hintLabel;

        private void Start()
        {
            // 初始隐藏所有目标物体
            if (targets != null)
            {
                foreach (var t in targets)
                {
                    if (t != null) t.SetActive(false);
                }
            }

            // 延迟创建提示UI，等待 PlayerRenderer.EnsureGlobalUI 创建 Canvas
            StartCoroutine(DelayedBuildHintUI());
        }

        private IEnumerator DelayedBuildHintUI()
        {
            // 等 DialogueManager 的画布——不能用 FindObjectOfType<Canvas>()：
            // 经场景切换进入时过渡黑屏 Canvas（DontDestroyOnLoad，淡出后自毁）会被抢先命中，
            // 提示条挂上去后随其销毁，导致提示永远不弹
            float timeout = 5f;
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

            hintRoot = new GameObject("RevealHint").AddComponent<RectTransform>();
            hintRoot.SetParent(canvas.transform, false);
            hintRoot.sizeDelta = new Vector2(180f, 51f);
            hintRoot.gameObject.SetActive(false);

            var bg = hintRoot.gameObject.AddComponent<UnityEngine.UI.Image>();
            bg.color = new Color(0f, 0f, 0f, 0.6f);
            bg.raycastTarget = false;

            var textGo = new GameObject("HintText");
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.SetParent(hintRoot, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(6f, 3f);
            textRect.offsetMax = new Vector2(-6f, -3f);
            hintLabel = textGo.AddComponent<UnityEngine.UI.Text>();
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
            if (interactOnce && hasInteracted) { HideHint(); return; }

            var player = Player.Current;
            if (player == null) return;

            Vector3 pos = transform.position;
            playerInRange = Mathf.Abs(player.Position.x - pos.x) <= interactRange.x * 0.5f
                && Mathf.Abs(player.Position.y - pos.y) <= interactRange.y * 0.5f;

            if (playerInRange) ShowHint(); else HideHint();

            if (playerInRange && (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.JoystickButton2)))
            {
                Reveal();
            }
        }

        private void Reveal()
        {
            if (interactOnce && hasInteracted) return;
            hasInteracted = true;
            HideHint();

            if (targets != null)
            {
                foreach (var t in targets)
                {
                    if (t != null) t.SetActive(true);
                }
            }

            Debug.Log($"[ObjectRevealer] Activated {targets?.Length ?? 0} objects");

            // 大树生成时刻：手柄震动反馈（Xbox/桥接手柄有效）
            RumbleDriver.Play(0.6f, 0.4f);

            // 交互后同时触发角色切换（等效按 Tab）
            var csc = FindObjectOfType<CharacterSwitchController>();
            if (csc != null) csc.SwitchCharacter();
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
            Gizmos.color = new Color(0.3f, 1f, 0.5f, 0.3f);
            Gizmos.DrawCube(transform.position, interactRange);
            Gizmos.color = new Color(0.3f, 1f, 0.5f);
            Gizmos.DrawWireCube(transform.position, interactRange);
        }
    }
}
