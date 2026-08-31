using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace Myd.Platform
{
    /// <summary>
    /// 标题画面：全屏封面图 + "按任意键继续"提示（呼吸闪烁），任意键/鼠标/手柄按键后淡出并加载 scence1_1。
    /// 挂在独立场景 TitleScene 上（Build Settings 置顶，启动即见）。
    /// </summary>
    public class TitleScreen : MonoBehaviour
    {
        [Tooltip("封面图（留空=从 Assets/ProPlatformer/_Arts/Title/Cover.png 加载）")]
        [SerializeField] private Sprite coverSprite;
        [Tooltip("按任意键后进入的场景")]
        [SerializeField] private string nextSceneName = "scence1_1";
        [Tooltip("淡出时长（秒）")]
        [SerializeField] private float fadeOutDuration = 0.6f;
        [Tooltip("提示文字")]
        [SerializeField] private string hintText = "按任意键继续";
        [Tooltip("提示文字大小")]
        [SerializeField] private int hintTextSize = 30;
        [Tooltip("提示距屏幕底部距离（像素）")]
        [SerializeField] private float hintBottomMargin = 70f;

        private IEnumerator Start()
        {
            // ---- 音频系统诊断（排查打包版无声问题）----
            Debug.Log($"[Audio诊断] outputSampleRate={AudioSettings.outputSampleRate} " +
                      $"speakerMode={AudioSettings.speakerMode} " +
                      $"driverCapabilities={AudioSettings.driverCapabilities} " +
                      $"listenerCount={UnityEngine.Object.FindObjectsOfType<AudioListener>().Length}");

            if (coverSprite == null)
            {
#if UNITY_EDITOR
                coverSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/ProPlatformer/_Arts/Title/Cover.png");
#endif
                // 打包：使用场景序列化的 coverSprite 引用（编辑器已赋值）
            }

            var canvasGo = new GameObject("TitleCanvas");
            DontDestroyOnLoad(canvasGo);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 3000;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // 封面图：等比铺满屏幕（不裁切：宽高都以内边为准）
            if (coverSprite != null)
            {
                var imgGo = new GameObject("Cover");
                var img = imgGo.AddComponent<RawImage>();
                img.rectTransform.SetParent(canvas.transform, false);
                img.texture = coverSprite.texture;
                img.color = Color.white;
                img.raycastTarget = false;
                // 留住 Sprite 的九宫格/中心矩形：直接全屏拉伸显示整图
                img.rectTransform.anchorMin = Vector2.zero;
                img.rectTransform.anchorMax = Vector2.one;
                img.rectTransform.offsetMin = Vector2.zero;
                img.rectTransform.offsetMax = Vector2.zero;
            }

            // "按任意键继续"：呼吸闪烁
            var hintGo = new GameObject("Hint");
            var hintRect = hintGo.AddComponent<RectTransform>();
            hintRect.SetParent(canvas.transform, false);
            hintRect.anchorMin = new Vector2(0.5f, 0f);
            hintRect.anchorMax = new Vector2(0.5f, 0f);
            hintRect.pivot = new Vector2(0.5f, 0f);
            hintRect.anchoredPosition = new Vector2(0f, hintBottomMargin);
            hintRect.sizeDelta = new Vector2(600f, 50f);

            var label = hintGo.AddComponent<Text>();
            label.fontSize = hintTextSize;
            label.color = new Color(1f, 0.95f, 0.85f, 0.9f);
            label.alignment = TextAnchor.MiddleCenter;
            label.text = hintText;
            label.raycastTarget = false;
            Font font = Resources.Load<Font>("NotoSansSC-Regular");
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.font = font;

            // 等待任意输入（键盘/鼠标/手柄按钮），提示文字呼吸闪烁
            // 先忽略启动后 0.5 秒的输入：启动瞬间 Windows 激活事件会被 anyKeyDown 误报
            yield return new WaitForSecondsRealtime(0.5f);
            float blink = 0f;
            while (!AnyInput())
            {
                blink += Time.unscaledDeltaTime;
                float a = 0.55f + 0.35f * Mathf.Sin(blink * 2.4f);   // 呼吸：0.2~0.9
                label.color = new Color(1f, 0.95f, 0.85f, a);
                yield return null;
            }

            Debug.Log($"[TitleScreen] 按键确认 → 淡出并进入 {nextSceneName}");
            // 淡出提示与封面
            float t = 0f;
            Color c0 = label.color, imgC0 = Color.white;
            var cover = canvas.GetComponentInChildren<RawImage>();
            while (t < fadeOutDuration)
            {
                t += Time.unscaledDeltaTime;
                float a = 1f - t / fadeOutDuration;
                label.color = new Color(c0.r, c0.g, c0.b, c0.a * a);
                if (cover != null) cover.color = new Color(1f, 1f, 1f, a);
                yield return null;
            }

            UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
            // 场景加载完成后销毁标题Canvas（下一帧）
            yield return null;
            Destroy(canvasGo);
        }

        private bool AnyInput()
        {
            if (Input.anyKeyDown) return true;
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2)) return true;
            // 手柄按钮：仅按下瞬间（不含摇杆——摇杆漂移/碰触不应误触发继续）
            for (int b = 0; b < 20; b++)
            {
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.JoystickButton0 + b)))
                    return true;
            }
            return false;
        }
    }
}
