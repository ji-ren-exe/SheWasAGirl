using System.Collections;
using UnityEngine;

namespace Myd.Platform
{
    /// <summary>
    /// 跨场景常驻 BGM 播放器（DontDestroyOnLoad 单例）：切换场景不中断。
    /// 场景用 SceneMusicSetter 在加载时声明曲目：
    /// - 与当前曲子相同且仍在播 → 无缝继续（跨 scene 连播，过渡黑屏期间音乐不断）
    /// - 同曲已播完（如重进起始场景）或不同曲 → 旧曲淡出 → 新曲淡入
    /// - loop=false → 播完一遍，末尾淡出后停止（片尾曲式"播完即止"）
    /// - 场景没有声明 → 保持上一首继续播
    /// </summary>
    public class PersistentBGM : MonoBehaviour
    {
        private static PersistentBGM instance;
        private AudioSource source;
        private UnityEngine.Coroutine switchCo; // 项目有自定义 Coroutine 类，用全限定名
        private UnityEngine.Coroutine endCo;    // 非循环曲目的播完淡出监听
        private UnityEngine.Coroutine stopCo;

        /// <summary>当前正在播放的曲子（null=无）</summary>
        public static AudioClip CurrentClip =>
            instance != null && instance.source != null ? instance.source.clip : null;

        public static bool IsPlaying =>
            instance != null && instance.source != null && instance.source.isPlaying;

        /// <summary>
        /// 播放曲目：同曲仍在播→无缝继续；同曲已播完/异曲→淡出旧→淡入新。
        /// loop=false 时播完一遍末尾淡出停止（不循环，"直到播放完毕"）。
        /// </summary>
        public static void Play(AudioClip clip, float volume = 0.6f, float fade = 1f, bool loop = true)
        {
            if (clip == null) return;
            EnsureInstance();

            // 打断进行中的停止流程（复活播放）
            if (instance.stopCo != null) { instance.StopCoroutine(instance.stopCo); instance.stopCo = null; }

            if (instance.source.clip == clip && instance.source.isPlaying)
            {
                // 同一曲仍在播：跨场景无缝继续（切换协程进行中则让其自然完成，不打断）
                if (instance.switchCo == null) instance.source.volume = volume;
                return;
            }

            if (instance.switchCo != null) instance.StopCoroutine(instance.switchCo);
            instance.switchCo = instance.StartCoroutine(instance.SwitchRoutine(clip, volume, fade, loop));
        }

        /// <summary>淡出并停止当前曲目</summary>
        public static void Stop(float fade = 1f)
        {
            if (instance == null || instance.source == null || !instance.source.isPlaying) return;
            if (instance.switchCo != null) { instance.StopCoroutine(instance.switchCo); instance.switchCo = null; }
            if (instance.endCo != null) { instance.StopCoroutine(instance.endCo); instance.endCo = null; }
            if (instance.stopCo != null) instance.StopCoroutine(instance.stopCo);
            instance.stopCo = instance.StartCoroutine(instance.StopRoutine(fade));
        }

        private static void EnsureInstance()
        {
            if (instance == null)
            {
                instance = FindObjectOfType<PersistentBGM>();
                if (instance == null)
                {
                    var go = new GameObject("PersistentBGM");
                    DontDestroyOnLoad(go);
                    instance = go.AddComponent<PersistentBGM>();
                    var src = go.AddComponent<AudioSource>();
                    src.playOnAwake = false;
                    src.loop = true;
                    src.spatialBlend = 0f;
                    instance.source = src;
                }
            }
            if (instance.source == null)
                instance.source = instance.GetComponent<AudioSource>();
        }

        private IEnumerator SwitchRoutine(AudioClip clip, float volume, float fade, bool loop)
        {
            // 旧曲淡出（未在播则直接换曲）
            if (source.isPlaying && fade > 0f)
                yield return FadeVolume(source.volume, 0f, fade);

            // 打断上一首的播完监听
            if (endCo != null) { StopCoroutine(endCo); endCo = null; }

            source.clip = clip;
            source.loop = loop;
            source.volume = 0f;
            source.Play();

            // 新曲淡入
            if (fade > 0f)
                yield return FadeVolume(0f, volume, fade);
            source.volume = volume;
            switchCo = null;

            // 非循环：监听播完，末尾淡出后停止
            if (!loop)
                endCo = StartCoroutine(EndFadeMonitor(clip, fade > 0f ? fade : 1f));
        }

        /// <summary>
        /// 非循环曲目播完监听：淡入已占用 fade 秒，在曲目结束前 fade 秒开始淡出，自然衔接曲目结尾。
        /// 全程用 unscaled 时间（timeScale=0 冻帧时不影响音乐与计时）。
        /// </summary>
        private IEnumerator EndFadeMonitor(AudioClip clip, float fade)
        {
            float wait = Mathf.Max(0f, clip.length - fade - fade);
            if (wait > 0f)
                yield return new WaitForSecondsRealtime(wait);
            yield return FadeVolume(source.volume, 0f, fade);
            source.Stop();
            source.clip = null;   // 播完清空：重进起始场景会重新播放
            endCo = null;
        }

        private IEnumerator StopRoutine(float fade)
        {
            yield return FadeVolume(source.volume, 0f, fade);
            source.Stop();
            source.clip = null;
            stopCo = null;
        }

        // 用 unscaledDeltaTime：冻帧（timeScale=0）时淡入淡出不卡住
        private IEnumerator FadeVolume(float from, float to, float fade)
        {
            float t = 0f;
            while (t < fade)
            {
                t += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(from, to, t / fade);
                yield return null;
            }
            source.volume = to;
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}
