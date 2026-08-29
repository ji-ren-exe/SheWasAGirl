using UnityEngine;
using TMPro;

namespace Myd.Platform
{
    /// <summary>
    /// 锚定在世界坐标的新手提示牌：玩家进入显示范围后淡入，离开淡出。
    /// 支持键盘/手柄双套文案，跟随最近使用的输入设备自动切换。
    /// </summary>
    public class WorldSpaceHint : MonoBehaviour
    {
        [Tooltip("提示文本（支持中文）")]
        [TextArea(2, 4)]
        [SerializeField] private string hintText = "按 空格 跳跃";
        [Tooltip("手柄提示文本（检测到最近输入来自手柄时显示）")]
        [TextArea(2, 4)]
        [SerializeField] private string hintTextGamepad = "按 A 跳跃";
        [Tooltip("玩家进入此距离后显示")]
        [SerializeField] private float showDistance = 5f;
        [Tooltip("淡入淡出时长")]
        [SerializeField] private float fadeDuration = 0.4f;
        [Tooltip("离开范围后是否保持显示（一次性教学）")]
        [SerializeField] private bool keepOnceShown = false;

        private TextMeshPro tmp;
        private float targetAlpha;
        private float currentAlpha;
        private bool shownOnce;
        private bool lastGamepadState;
        // 关联的底板（Hint_*_Backdrop），与文字同步淡入淡出
        private SpriteRenderer backdrop;

        private void Awake()
        {
            tmp = GetComponent<TextMeshPro>();
            if (tmp != null)
            {
                currentAlpha = 0f;
                SetAlpha(0f);
            }
            FindBackdrop();
        }

        private void FindBackdrop()
        {
            // 同级找 命名+"_Backdrop" 的对象（不区分父级）
            string bdName = gameObject.name + "_Backdrop";
            var all = FindObjectsOfType<SpriteRenderer>(true);
            foreach (var sr in all)
            {
                if (sr.gameObject.name == bdName)
                {
                    backdrop = sr;
                    if (Application.isPlaying) SetBackdropAlpha(0f);
                    break;
                }
            }
        }

        private void Update()
        {
            if (tmp == null) return;

            // 输入设备切换文案（整套提示跟随最近使用的设备）
            bool useGamepad = Myd.Platform.GameInput.UsingGamepad;
            if (useGamepad != lastGamepadState)
            {
                lastGamepadState = useGamepad;
                tmp.text = useGamepad ? hintTextGamepad : hintText;
            }

            bool inRange = Player.Current != null &&
                Vector2.Distance(Player.Current.Position, transform.position) <= showDistance;

            if (inRange) shownOnce = true;
            targetAlpha = (inRange || (keepOnceShown && shownOnce)) ? 1f : 0f;

            if (!Mathf.Approximately(currentAlpha, targetAlpha))
            {
                float speed = fadeDuration > 0f ? 1f / fadeDuration : float.MaxValue;
                currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, speed * Time.deltaTime);
                SetAlpha(currentAlpha);
                SetBackdropAlpha(currentAlpha);
            }
        }

        private void SetAlpha(float a)
        {
            var c = tmp.color;
            c.a = a;
            tmp.color = c;
        }

        private void SetBackdropAlpha(float a)
        {
            if (backdrop == null) return;
            var c = backdrop.color;
            c.a = a;
            backdrop.color = c;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 编辑器里同步 Inspector 文本到 TMP（不用 Play 也能看到效果）
            if (tmp == null) tmp = GetComponent<TextMeshPro>();
            if (tmp != null && !Application.isPlaying)
            {
                tmp.text = hintText;
                SetAlpha(1f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, showDistance);
        }
#endif
    }
}
