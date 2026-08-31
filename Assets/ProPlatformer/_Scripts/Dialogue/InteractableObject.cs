using UnityEngine;
using System.Collections;

namespace Myd.Platform.Dialogue
{
    /// <summary>
    /// 可交互物品：玩家靠近后按 X 键（手柄X键）交互，触发对话
    /// 挂在场景中的物品对象上（如收音机、路牌、NPC）
    /// 靠近时物体旁显示"按 E 交互"提示
    /// </summary>
    public class InteractableObject : MonoBehaviour
    {
        [Header("对话数据")]
        [SerializeField] private DialogueData dialogue;

        [Header("交互设置")]
        [Tooltip("交互范围（以本物体为中心的矩形，世界单位）")]
        [SerializeField] private Vector2 interactRange = new Vector2(2.5f, 3f);
        [Tooltip("是否只能交互一次")]
        [SerializeField] private bool interactOnce = true;
        [Tooltip("本物体自身作为说话者（气泡默认跟随本物体，需要同时挂 DialogueSpeaker 组件并编号）")]
        [SerializeField] private bool selfAsSpeaker = false;
        [Tooltip("交互提示文本")]
        [SerializeField] private string hintText = "按 X 交互";
        [Tooltip("按X后启动修锁小游戏（而非对话）")]
        [SerializeField] private bool lockPickingMode = false;
        [Tooltip("对话播放完成后自动启动修锁小游戏")]
        [SerializeField] private bool lockPickingAfterDialogue = false;
        [Tooltip("对话播放完成后弹出保险箱密码设置窗口")]
        [SerializeField] private bool safePasswordAfterDialogue = false;

        private bool hasInteracted;
        private bool playerInRange;
        private DialogueManager manager;

        // 提示UI（动态创建，复用 DialogueManager 的 Canvas）
        private RectTransform hintRoot;
        private UnityEngine.UI.Text hintLabel;

        private void Start()
        {
            StartCoroutine(DelayedInit());
        }

        private IEnumerator DelayedInit()
        {
            // 等待 DialogueManager 就绪（PlayerRenderer.EnsureGlobalUI 创建）
            float timeout = 5f;
            while (DialogueManager.Instance == null && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }
            manager = DialogueManager.Instance;
            BuildHintUI();
        }

        private void BuildHintUI()
        {
            if (manager == null) return;
            var canvas = manager.GetComponent<Canvas>();
            if (canvas == null) return;

            hintRoot = new GameObject("InteractHint").AddComponent<RectTransform>();
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
            if (dialogue == null || manager == null) return;
            if (interactOnce && hasInteracted) { HideHint(); return; }

            var player = Player.Current;
            if (player == null) return;

            // 检测玩家是否在交互范围内（只比较 X/Y，忽略 Z）
            Vector3 pos = transform.position;
            playerInRange = Mathf.Abs(player.Position.x - pos.x) <= interactRange.x * 0.5f
                && Mathf.Abs(player.Position.y - pos.y) <= interactRange.y * 0.5f;

            // 显示/隐藏交互提示
            bool showHint = playerInRange && !manager.IsPlaying;
            if (showHint) ShowHint(); else HideHint();

            // 在范围内且按下交互键（X 键 / 手柄 X 键 = joystick button 2）
            if (playerInRange && !manager.IsPlaying &&
                (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.JoystickButton2)))
            {
                Interact();
            }
        }

        private void ShowHint()
        {
            if (hintRoot == null) return;
            hintRoot.gameObject.SetActive(true);

            // 提示跟随物体头顶
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

        private void OnDrawGizmosSelected()
        {
            // 交互范围可视化（橙色）
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.3f);
            Gizmos.DrawCube(transform.position, interactRange);
            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawWireCube(transform.position, interactRange);
        }

        /// <summary>
        /// 外部触发交互（如被其他脚本调用）
        /// </summary>
        public void Interact()
        {
            if (interactOnce && hasInteracted) return;
            hasInteracted = true;
            HideHint();

            if (lockPickingMode)
            {
                var gameGo = new GameObject("LockPickingGame");
                var game = gameGo.AddComponent<Myd.Platform.LockPickingGame>();
                game.StartGame(
                    onSuccess: () =>
                    {
                        // 在新门上创建开门触发器
                        var newDoor = GameObject.Find("新门");
                        if (newDoor != null)
                        {
                            var openerGo = new GameObject("DoorOpener");
                            openerGo.transform.SetParent(newDoor.transform, false);
                            openerGo.transform.localPosition = Vector3.zero;
                            var opener = openerGo.AddComponent<Myd.Platform.DoorOpener>();
                            opener.targetSceneName = "scence2_4";
                        }
                    },
                    onCancel: () => { /* 重新允许交互 */ hasInteracted = false; }
                );
                return;
            }

            if (dialogue == null || manager == null) return;
            DialogueSpeaker speaker = selfAsSpeaker ? GetComponent<DialogueSpeaker>() : null;

            if (lockPickingAfterDialogue)
            {
                StartCoroutine(PlayDialogueThenLockPicking(speaker));
                return;
            }

            if (safePasswordAfterDialogue)
            {
                StartCoroutine(PlayDialogueThenSafePassword(speaker));
                return;
            }

            manager.Play(dialogue, speaker);
        }

        /// <summary>
        /// 对话播放完成后弹出保险箱密码设置窗口
        /// </summary>
        private System.Collections.IEnumerator PlayDialogueThenSafePassword(DialogueSpeaker speaker)
        {
            manager.Play(dialogue, speaker);
            yield return new WaitWhile(() => manager.IsPlaying);

            var windowGo = new GameObject("SafePasswordWindow");
            var window = windowGo.AddComponent<Myd.Platform.SafePasswordWindow>();
            window.Show(
                confirmCallback: (pwd) =>
                {
                    Debug.Log($"[SafePassword] 密码已设置: {pwd}（{gameObject.name}）");
                    // 全局记录新密码：女儿庭院保险箱（保险箱 新）用此密码校验
                    Myd.Platform.SafeController.Combination = pwd;
                },
                cancelCallback: (pwd) => { hasInteracted = false; } // 取消后允许再次交互
            );
        }

        private IEnumerator PlayDialogueThenLockPicking(DialogueSpeaker speaker)
        {
            manager.Play(dialogue, speaker);
            yield return new WaitWhile(() => manager.IsPlaying);

            // 对话结束后启动修锁小游戏
            var gameGo = new GameObject("LockPickingGame");
            var game = gameGo.AddComponent<Myd.Platform.LockPickingGame>();
            game.StartGame(
                onSuccess: () =>
                {
                    // 在新门上创建一个独立的提示+开门触发器
                    var newDoor = GameObject.Find("新门");
                    if (newDoor != null)
                    {
                        var openerGo = new GameObject("DoorOpener");
                        openerGo.transform.SetParent(newDoor.transform, false);
                        openerGo.transform.localPosition = Vector3.zero;
                        var opener = openerGo.AddComponent<Myd.Platform.DoorOpener>();
                        opener.targetSceneName = "scence2_4";
                    }
                },
                onCancel: () => { hasInteracted = false; }
            );
        }

        private void OnDestroy()
        {
            if (hintRoot != null)
                Destroy(hintRoot.gameObject);
        }
    }
}
