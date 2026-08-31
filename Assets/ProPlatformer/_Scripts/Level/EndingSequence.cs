using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

namespace Myd.Platform
{
    /// <summary>
    /// 结局序列（挂在 scence3_8 的 SceneTransition 同对象上，targetSceneName 留空）：
    /// 到达切换点 → 白屏渐变（1s）→ 最后一幕.png 淡入停留淡出（共8s）→ 纯黑（0.8s）
    /// → 0.8倍慢放视频（RenderTexture 全屏）→ 字幕1（6s）→ 字幕2（8s）→ 退出游戏
    /// 编辑器下不退出，只结束序列（防止误关编辑器）。
    /// </summary>
    public class EndingSequence : MonoBehaviour
    {
        [Header("素材（留空=按文件名从 Ending 目录加载）")]
        [SerializeField] private Sprite finalSceneSprite;   // 最后一幕
        [SerializeField] private VideoClip endingVideo;     // 结局视频
        [SerializeField] private Sprite subtitle1;          // 字幕1
        [SerializeField] private Sprite subtitle2;          // 字幕2

        [Header("时序（秒）")]
        [Tooltip("画面转纯白时长")]
        [SerializeField] private float whiteFadeIn = 1f;
        [Tooltip("最后一幕：淡入时长")]
        [SerializeField] private float imageFadeIn = 1.5f;
        [Tooltip("最后一幕：停留时长（淡入后）")]
        [SerializeField] private float imageHold = 4f;
        [Tooltip("最后一幕：淡出时长（淡出到纯黑）")]
        [SerializeField] private float imageFadeOut = 1.5f;
        [Tooltip("视频播放速率（0.8=慢放）")]
        [SerializeField] private float videoPlaybackSpeed = 0.8f;
        [Tooltip("字幕1 停留时长")]
        [SerializeField] private float subtitle1Duration = 6f;
        [Tooltip("字幕2 停留时长")]
        [SerializeField] private float subtitle2Duration = 8f;

        [Tooltip("触发方式：true=玩家进入触发范围自动开始；false=需外部调用 StartEnding()")]
        [SerializeField] private bool autoTriggerOnReach = true;
        [Tooltip("触发范围（世界单位，以本物体为中心的矩形）")]
        [SerializeField] private Vector2 triggerSize = new Vector2(2f, 4f);

        private bool started;

        private void Update()
        {
            if (!autoTriggerOnReach || started) return;
            var player = Player.Current;
            if (player == null) return;
            if (Mathf.Abs(player.Position.x - transform.position.x) <= triggerSize.x * 0.5f
                && Mathf.Abs(player.Position.y - transform.position.y) <= triggerSize.y * 0.5f)
            {
                StartEnding();
            }
        }

        /// <summary>外部触发入口</summary>
        public void StartEnding()
        {
            if (started) return;
            started = true;
            StartCoroutine(EndingRoutine());
        }

        private IEnumerator EndingRoutine()
        {
            // 停掉可能还在播的对话/BGM 由各自系统处理；这里冻结角色与相机
            Game.SceneBusy = true;

            LoadAssets();

            Canvas canvas = CreateOverlayCanvas();

            // 黑色底板：始终在内容层之下，白屏盖满后启用——之后所有淡入淡出都在黑底上合成，
            // 地图场景被完全遮住，不会在换素材间隙露出
            var blackTex = new Texture2D(4, 4);
            Color[] blackPixels = new Color[16];
            for (int i = 0; i < 16; i++) blackPixels[i] = Color.black;
            blackTex.SetPixels(blackPixels);
            blackTex.Apply();
            var blackBG = new GameObject("BlackBG").AddComponent<RawImage>();
            blackBG.rectTransform.SetParent(canvas.transform, false);
            StretchFullScreen(blackBG.rectTransform);
            blackBG.raycastTarget = false;
            blackBG.texture = blackTex;
            blackBG.color = new Color(1, 1, 1, 0);   // 起始透明：白屏淡入期间游戏画面可见（渐变起点）

            // 内容层
            var raw = new GameObject("Image").AddComponent<RawImage>();
            raw.rectTransform.SetParent(canvas.transform, false);
            StretchFullScreen(raw.rectTransform);
            raw.raycastTarget = false;

            // ---------- 阶段1：游戏画面 → 纯白 ----------
            var whiteTex = new Texture2D(4, 4);
            Color[] pixels = new Color[16];
            for (int i = 0; i < 16; i++) pixels[i] = Color.white;
            whiteTex.SetPixels(pixels);
            whiteTex.Apply();
            raw.texture = whiteTex;
            raw.color = new Color(1, 1, 1, 0);
            yield return Fade(raw, 0f, 1f, whiteFadeIn);

            // 白屏已不透明：启用黑底（此刻无视觉变化），从此地图被完全遮挡
            blackBG.color = new Color(1, 1, 1, 1);

            // ---------- 阶段2：最后一幕 淡入 → 停留 → 淡出（到黑） ----------
            if (finalSceneSprite != null)
            {
                raw.texture = finalSceneSprite.texture;
                raw.color = new Color(1, 1, 1, 0);
                yield return Fade(raw, 0f, 1f, imageFadeIn);
                yield return new WaitForSeconds(imageHold);
                yield return Fade(raw, 1f, 0f, imageFadeOut);   // 淡出到黑底
            }

            // ---------- 阶段3：0.8倍慢放视频（黑场之后直接播） ----------
            if (endingVideo != null)
            {
                yield return PlayVideo(canvas, raw);
            }

            // ---------- 阶段4/5：字幕1 → 字幕2 ----------
            foreach (var (sub, dur) in new[] { (subtitle1, subtitle1Duration), (subtitle2, subtitle2Duration) })
            {
                if (sub == null) continue;
                raw.texture = sub.texture;
                raw.color = new Color(1, 1, 1, 0);
                yield return Fade(raw, 0f, 1f, imageFadeIn);
                yield return new WaitForSeconds(dur);
                yield return Fade(raw, 1f, 0f, imageFadeOut);
            }

            // ---------- 阶段6：退出 ----------
            yield return new WaitForSeconds(0.5f);
#if UNITY_EDITOR
            Debug.Log("[EndingSequence] 播放完毕（编辑器下不退出，请手动停止 Play）");
            // 编辑器：移除覆盖层便于继续调试；打包版直接退出进程
            Object.Destroy(canvas.gameObject);
#else
            Application.Quit();
#endif
            Game.SceneBusy = false;
        }

        private IEnumerator PlayVideo(Canvas canvas, RawImage raw)
        {
            var go = new GameObject("VideoPlayer");
            var vp = go.AddComponent<VideoPlayer>();
            vp.playOnAwake = false;
            vp.renderMode = VideoRenderMode.RenderTexture;
            vp.clip = endingVideo;
            vp.playbackSpeed = videoPlaybackSpeed;
            vp.audioOutputMode = VideoAudioOutputMode.Direct;   // 直接送声卡，无需 AudioSource
            vp.targetTexture = new RenderTexture(
                Mathf.Max((int)endingVideo.width, 16), Mathf.Max((int)endingVideo.height, 16), 0);

            raw.texture = vp.targetTexture;
            raw.color = Color.white;
            vp.Prepare();
            while (!vp.isPrepared) yield return null;
            vp.Play();

            // 等播完（0.8 倍速实际时长 = clip.length / speed）
            float dur = (float)(endingVideo.length / videoPlaybackSpeed);
            float t = 0f;
            while (t < dur && vp.isPlaying)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            vp.Stop();
            raw.color = new Color(1, 1, 1, 0);   // 视频结束回到黑场
            vp.targetTexture.Release();
            Object.Destroy(go);
        }

        private IEnumerator Fade(RawImage img, float from, float to, float duration)
        {
            if (duration <= 0f)
            {
                img.color = new Color(1, 1, 1, to);
                yield break;
            }
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                img.color = new Color(1, 1, 1, Mathf.Lerp(from, to, t / duration));
                yield return null;
            }
            img.color = new Color(1, 1, 1, to);
        }

        private Canvas CreateOverlayCanvas()
        {
            var go = new GameObject("EndingCanvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 3000;   // 盖过对话(500)/任务UI
            go.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.referenceResolution = new Vector2(1792, 1008);   // 与素材分辨率一致，全屏等比
            scaler.matchWidthOrHeight = 0.5f;
            DontDestroyOnLoad(go);   // 防场景意外卸载中断序列
            return canvas;
        }

        private static void StretchFullScreen(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private void LoadAssets()
        {
            // 素材引用已在场景中序列化（编辑器配置）；此兜底仅编辑器下生效
#if UNITY_EDITOR
            const string dir = "Assets/ProPlatformer/_Arts/Ending";
            if (finalSceneSprite == null) finalSceneSprite = LoadSprite(System.IO.Path.Combine(dir, "最后一幕.png"));
            if (subtitle1 == null) subtitle1 = LoadSprite(System.IO.Path.Combine(dir, "字幕1.png"));
            if (subtitle2 == null) subtitle2 = LoadSprite(System.IO.Path.Combine(dir, "字幕2.png"));
            if (endingVideo == null) endingVideo = UnityEditor.AssetDatabase.LoadAssetAtPath<VideoClip>(
                System.IO.Path.Combine(dir, "视频-1788042904741.mp4"));
#endif
        }

#if UNITY_EDITOR
        private static Sprite LoadSprite(string path)
        {
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
#endif

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.95f, 0.6f, 0.4f);
            Gizmos.DrawCube(transform.position, triggerSize);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, triggerSize);
        }
    }
}
