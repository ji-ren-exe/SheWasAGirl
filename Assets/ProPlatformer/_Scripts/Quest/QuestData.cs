using UnityEngine;

namespace Myd.Platform.Quest
{
    /// <summary>
    /// 任务数据：任务标题 + 任务描述列表
    /// </summary>
    [CreateAssetMenu(fileName = "NewQuest", menuName = "ProPlatformer/任务数据")]
    public class QuestData : ScriptableObject
    {
        [Tooltip("任务ID（唯一，用于切换逻辑）")]
        public string questId;
        [Tooltip("任务标题（像素风UI左上角显示）")]
        public string title;
        [TextArea(2, 3)]
        [Tooltip("任务描述（可多行）")]
        public string description;
    }
}
