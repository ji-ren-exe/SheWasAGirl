using UnityEngine;

namespace Myd.Platform
{
    /// <summary>
    /// 场景 BGM 声明：挂在场景任意对象上（建议挂 Game）。
    /// clip 与上一场景相同且在播 → 无缝继续（跨 scene 连播）；不同 → 自动切换；留空 → 淡出停止。
    /// 场景里不放此组件 → 保持上一首继续播（延伸到无音乐声明的场景）。
    /// loop 不勾选 → 播完一遍末尾淡出后停止，不循环。
    /// 注意：本类必须在独立 .cs 文件中——Tuanjie 对"一个文件里的第二个 MonoBehaviour 类"
    /// 只能写出场景内嵌 stub 脚本引用，场景重载后失效（组件丢失+序列化数据丢失）。
    /// </summary>
    public class SceneMusicSetter : MonoBehaviour
    {
        [Tooltip("本场景 BGM。与上一场景相同且在播→无缝继续；不同→淡出切新；留空→淡出停止")]
        [SerializeField] private AudioClip clip;
        [Range(0f, 1f)]
        [Tooltip("音量")]
        [SerializeField] private float volume = 0.6f;
        [Tooltip("淡入/淡出时长（秒），0=立即切换；非循环曲目结尾也按此时长淡出")]
        [SerializeField] private float fade = 1f;
        [Tooltip("勾选=循环播放；不勾=播完一遍末尾淡出后停止（播完即止，不重头）")]
        [SerializeField] private bool loop = true;

        private void Start()
        {
            if (clip != null)
                PersistentBGM.Play(clip, volume, fade, loop);
            else
                PersistentBGM.Stop(fade);
        }
    }
}
