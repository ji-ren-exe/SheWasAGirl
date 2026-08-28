using UnityEngine;

namespace Myd.Platform.Quest
{
    /// <summary>
    /// 任务触发器：玩家进入范围后切换任务（删除旧任务，显示新任务）
    /// </summary>
    public class QuestTrigger : MonoBehaviour
    {
        [Header("任务数据")]
        [SerializeField] private QuestData quest;

        [Header("触发设置")]
        [Tooltip("触发范围（以本物体为中心的矩形，世界单位）")]
        [SerializeField] private Vector2 triggerSize = new Vector2(5f, 7f);
        [Tooltip("是否只触发一次")]
        [SerializeField] private bool triggerOnce = true;

        private bool hasTriggered;

        private void Update()
        {
            if (quest == null || QuestUI.Instance == null) return;
            if (triggerOnce && hasTriggered) return;

            var player = Player.Current;
            if (player == null) return;

            // 只比较 X/Y（忽略 Z，避免 Bounds 三维误判）
            Vector3 pos = transform.position;
            bool inRange = Mathf.Abs(player.Position.x - pos.x) <= triggerSize.x * 0.5f
                && Mathf.Abs(player.Position.y - pos.y) <= triggerSize.y * 0.5f;

            if (inRange)
            {
                hasTriggered = true;
                // 切换任务：QuestUI 内部会淡出旧任务、淡入新任务
                QuestUI.Instance.SetQuest(quest);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // 任务触发范围可视化（紫色）
            Gizmos.color = new Color(0.7f, 0.3f, 1f, 0.3f);
            Gizmos.DrawCube(transform.position, triggerSize);
            Gizmos.color = new Color(0.7f, 0.3f, 1f);
            Gizmos.DrawWireCube(transform.position, triggerSize);
        }

        /// <summary>
        /// 外部调用触发
        /// </summary>
        public void Trigger()
        {
            if (quest == null || hasTriggered) return;
            hasTriggered = true;
            QuestUI.Instance?.SetQuest(quest);
        }
    }
}
