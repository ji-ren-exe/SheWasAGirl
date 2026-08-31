using UnityEngine;
using System.Collections;

namespace Myd.Platform
{
    /// <summary>
    /// 按住拖动交互：玩家靠近显示提示，按住X键（手柄X键）后物体跟随玩家水平移动，
    /// 松开X键放下——被拖物体原地不动，联动物体（对侧庭院）瞬移到玩家镜像位置旁（保留缝隙）。
    /// 只跟随X轴（Y不变），玩家跳跃/站上物体顶也不会带起物体。
    /// </summary>
    public class ObjectMover : MonoBehaviour
    {
        [Header("交互设置")]
        [Tooltip("交互范围（以本物体为中心的矩形，世界单位）")]
        [SerializeField] private Vector2 interactRange = new Vector2(4f, 5f);
        [Tooltip("交互提示文本")]
        [SerializeField] private string hintText = "按住 X 拖动";

        [Header("拖动目标")]
        [Tooltip("要拖动的物体（空=拖动自身）")]
        [SerializeField] private GameObject moveTarget;

        [Header("联动镜像")]
        [Tooltip("拖动本物体时，此物体也水平移动相同距离（如对侧庭院的对应杂物堆），Y 不变")]
        [SerializeField] private GameObject linkedTarget;

        [Tooltip("可主动拖动：玩家靠近按住X拖动本物体。关闭=只能被动（作为联动目标跟着动）")]
        [SerializeField] private bool canDrag = true;

        [Tooltip("松开吸附时物体与玩家碰撞箱之间的缝隙（世界单位）")]
        [SerializeField] private float snapGap = 0.35f;

        [Tooltip("联动物体所在庭院的玩家镜像位置与本庭院玩家的X偏移（scence2_4两庭院X对齐，为0）")]
        [SerializeField] private float linkedPlayerOffsetX = 0f;

        [Header("提示UI")]
        [Tooltip("提示条相对物体中心的Y偏移")]
        [SerializeField] private float hintYOffset = 2.6f;

        private bool dragging;
        private float dragOffsetX;
        private float rumbleTimer;   // 拖动中震动脉冲计时
        private RectTransform hintRoot;
        private UnityEngine.UI.Text hintLabel;

        // 拖动中临时禁用的碰撞体（自身+联动物体）：避免大碰撞体推挤/排斥玩家
        // 同步缓存禁用时的世界 AABB（禁用后 collider.bounds 失效）
        private readonly System.Collections.Generic.List<Collider2D> disabledColliders = new System.Collections.Generic.List<Collider2D>();
        private readonly System.Collections.Generic.List<Bounds> disabledBounds = new System.Collections.Generic.List<Bounds>();
        // 松开后等待玩家离开重叠区再恢复碰撞（防止恢复时把玩家卡在碰撞体内）
        private bool pendingRestore;
        // 各被禁碰撞体禁用时的 transform 位置（重建移动后的 AABB 用）
        private readonly System.Collections.Generic.List<Vector3> disabledStartPos = new System.Collections.Generic.List<Vector3>();

        private void Start()
        {
            if (moveTarget == null) moveTarget = gameObject;
            // 延迟创建提示UI，等待 PlayerRenderer.EnsureGlobalUI 创建 Canvas
            StartCoroutine(DelayedBuildHintUI());
        }

        private IEnumerator DelayedBuildHintUI()
        {
            float timeout = 5f;
            // 等 DialogueManager 的画布（PlayerRenderer.EnsureGlobalUI 运行时创建）——
            // 不能用 FindObjectOfType<Canvas>()：经 SceneTransition 进场景时过渡黑屏 Canvas
            // （DontDestroyOnLoad，淡出后自毁）会被抢先命中，提示条挂上去后随其销毁，
            // 之后 ShowHint 因 hintRoot==null 静默跳过，提示永远不弹
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

            hintRoot = new GameObject("MoveHint").AddComponent<RectTransform>();
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

        private bool HoldKey => Input.GetKey(KeyCode.X) || Input.GetKey(KeyCode.JoystickButton2);

        private void Update()
        {
            var player = Player.Current;
            if (player == null) return;

            // 松开后：玩家离开所有被禁碰撞体的重叠区才恢复碰撞
            if (pendingRestore)
            {
                if (!PlayerOverlapsAnyDisabled())
                {
                    foreach (var c in disabledColliders)
                        if (c != null) c.enabled = true;
                    disabledColliders.Clear();
                    disabledBounds.Clear();
                    disabledStartPos.Clear();
                    pendingRestore = false;
                }
                return;
            }

            if (dragging)
            {
                // 松开X键 → 放下：被拖物体原地不动，联动物体瞬移到对侧玩家镜像位置旁
                if (!HoldKey)
                {
                    dragging = false;
                    rumbleTimer = 0f;
                    TeleportLinkedBesidePlayer();
                    pendingRestore = disabledColliders.Count > 0;
                    return;
                }
                // 拖动中持续震动：每 0.35s 一个短脉冲（0.2s/低强度），模拟拖拽的沉闷阻力感；
                // 脉冲式而非一条长震，避免盖过拖动期间可能触发的其他事件震动
                rumbleTimer += Time.deltaTime;
                if (rumbleTimer >= 0.35f)
                {
                    rumbleTimer = 0f;
                    RumbleDriver.Play(0.35f, 0.2f);
                }
                // 跟随玩家水平移动（Y/Z 不变，保持贴地）。联动物体保持静止（松开时才瞬移）。
                var t = moveTarget.transform;
                float targetX = player.Position.x + dragOffsetX;
                t.position = new Vector3(targetX, t.position.y, t.position.z);
                return;
            }

            // 非拖动状态：范围检查 + 提示（canDrag=false 的物体只被动联动，不可主动拖）
            if (!canDrag) return;
            Vector3 pos = transform.position;
            bool playerInRange = Mathf.Abs(player.Position.x - pos.x) <= interactRange.x * 0.5f
                && Mathf.Abs(player.Position.y - pos.y) <= interactRange.y * 0.5f;

            if (playerInRange) ShowHint(); else HideHint();

            // 按住X开始拖动：锁定当前水平相对距离，并临时禁用被拖物体碰撞体（拖动中不排斥玩家）
            if (playerInRange && HoldKey)
            {
                dragging = true;
                dragOffsetX = moveTarget.transform.position.x - player.Position.x;
                HideHint();
                DisableColliders(moveTarget);
                Debug.Log($"[ObjectMover] 开始拖动 {moveTarget.name} (offset={dragOffsetX:F2})");
            }
        }

        private void DisableColliders(GameObject go)
        {
            foreach (var c in go.GetComponentsInChildren<Collider2D>())
            {
                if (c.enabled)
                {
                    c.enabled = false;
                    disabledColliders.Add(c);
                    disabledBounds.Add(c.bounds);      // 禁用前缓存有效 AABB
                    disabledStartPos.Add(c.transform.position);
                }
            }
        }

        /// <summary>
        /// 松开时：被拖物体原地不动；联动物体（对侧庭院）瞬移到玩家镜像位置旁，
        /// 中心距 = 联动碰撞半宽 + 玩家半宽 + snapGap 缝隙，方向与被拖物体相对玩家的方向一致。
        /// </summary>
        private void TeleportLinkedBesidePlayer()
        {
            var player = Player.Current;
            if (player == null || linkedTarget == null) return;

            var t = moveTarget.transform;
            float rel = t.position.x - player.Position.x;
            if (Mathf.Abs(rel) < 0.001f) return; // 正中无法定方向，不瞬移
            float sign = Mathf.Sign(rel);

            var lbc = linkedTarget.GetComponent<BoxCollider2D>();
            float lHalf = lbc != null ? Mathf.Abs(lbc.size.x * lbc.transform.lossyScale.x) * 0.5f : 0.5f;
            const float playerHalf = 0.135f; // 玩家碰撞箱宽0.27
            float lDist = lHalf + playerHalf + snapGap;

            float lTargetX = player.Position.x + linkedPlayerOffsetX + sign * lDist;
            var lt = linkedTarget.transform;
            lt.position = new Vector3(lTargetX, lt.position.y, lt.position.z);
            Debug.Log($"[ObjectMover] {moveTarget.name} 松开：原地放下；{linkedTarget.name} 瞬移至镜像位置旁 (dist={lDist:F2})");
        }

        // 玩家近似碰撞盒（宽0.27高1.1，中心在脚底上方0.55）是否与任一被禁碰撞体当前实际范围重叠
        private bool PlayerOverlapsAnyDisabled()
        {
            var player = Player.Current;
            if (player == null) return false;
            Vector2 pc = player.Position + Vector2.up * 0.55f;
            const float pw = 0.135f, ph = 0.55f;
            for (int i = 0; i < disabledColliders.Count; i++)
            {
                var c = disabledColliders[i];
                if (c == null) continue;
                // 禁用后 bounds 失效：用缓存 AABB 的尺寸 + 碰撞体当前位置重建（拖动中物体会移动）
                var b = disabledBounds[i];
                Vector2 center = c.transform.position + (Vector3)b.center - disabledStartPos[i];
                Vector2 ext = b.extents;
                if (Mathf.Abs(pc.x - center.x) < pw + ext.x && Mathf.Abs(pc.y - center.y) < ph + ext.y)
                    return true;
            }
            return false;
        }

        private void ShowHint()
        {
            if (hintRoot == null) return;
            hintRoot.gameObject.SetActive(true);

            var cam = Camera.main;
            if (cam == null) return;
            Vector2 worldPos = transform.position + Vector3.up * hintYOffset;
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
