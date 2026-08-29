using System;
using System.Collections.Generic;
using UnityEngine;

namespace Myd.Platform.Dialogue
{
    /// <summary>
    /// 对话数据：一组对话气泡（头像 + 文本）
    /// </summary>
    [Serializable]
    public class DialogueBubble
    {
        [Tooltip("说话者头像（null 则不显示头像）")]
        public Sprite portrait;
        [TextArea(2, 4)]
        public string text;
        [Tooltip("气泡持续时间（秒），<=0 表示需要按跳跃键推进")]
        public float duration = 3f;
        [Tooltip("说话者标记：0=玩家（跟随玩家），1/2/3...=场景中对应 Speaker 角色的编号。气泡会出现在该角色旁边")]
        public int speakerId = 0;

        [Header("延迟音效")]
        [Tooltip("气泡出现后延迟播放的音效（null 则不播放）")]
        public AudioClip delayedSound;
        [Tooltip("延迟时间（秒），从气泡出现开始计时")]
        public float delayedSoundDelay = 1f;
        [Range(0f, 1f)]
        [Tooltip("延迟音效音量")]
        public float delayedSoundVolume = 1f;
    }

    [CreateAssetMenu(fileName = "NewDialogue", menuName = "ProPlatformer/对话数据")]
    public class DialogueData : ScriptableObject
    {
        [Tooltip("对话ID，用于触发去重")]
        public string dialogueId;
        public List<DialogueBubble> bubbles = new List<DialogueBubble>();

        [Header("气泡位置")]
        [Tooltip("气泡位置模式：Default=跟随面朝方向，LeftBottom=固定角色左下方")]
        public BubblePositionMode bubblePosition = BubblePositionMode.Default;
    }

    public enum BubblePositionMode
    {
        Default,
        LeftBottom
    }
}
