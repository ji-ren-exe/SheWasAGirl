using UnityEngine;

namespace Myd.Platform.Dialogue
{
    /// <summary>
    /// 对话触发器：玩家进入触发范围时播放对话
    /// 挂在场景中的空物体上，配置范围和对话数据
    /// </summary>
    public class DialogueTrigger : MonoBehaviour
    {
        [Header("对话数据")]
        [SerializeField] private DialogueData dialogue;

        [Header("触发设置")]
        [Tooltip("触发方式")]
        [SerializeField] private TriggerMode mode = TriggerMode.EnterRange;
        [Tooltip("触发范围（以本物体为中心的矩形区域，世界单位）")]
        [SerializeField] private Vector2 triggerSize = new Vector2(4f, 6f);
        [Tooltip("是否只触发一次")]
        [SerializeField] private bool triggerOnce = true;

        public enum TriggerMode
        {
            EnterRange,     // 玩家进入范围自动触发
            KeyInRange,     // 玩家在范围内按跳跃键触发
            GameStart,      // 游戏开始自动触发
            Condition       // 由其他脚本调用 Trigger() 触发
        }

        private bool hasTriggered;

        private void Update()
        {
            if (dialogue == null || DialogueManager.Instance == null) return;
            if (triggerOnce && hasTriggered) return;

            switch (mode)
            {
                case TriggerMode.GameStart:
                    Trigger();
                    break;

                case TriggerMode.EnterRange:
                    if (PlayerInRange() && !DialogueManager.Instance.IsPlaying)
                        Trigger();
                    break;

                case TriggerMode.KeyInRange:
                    if (PlayerInRange() && !DialogueManager.Instance.IsPlaying &&
                        (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.JoystickButton2)))
                        Trigger();
                    break;
            }
        }

        /// <summary>
        /// 外部调用触发（用于条件触发）
        /// </summary>
        public void Trigger()
        {
            if (dialogue == null || hasTriggered) return;
            if (DialogueManager.Instance == null) return;

            hasTriggered = true;
            DialogueManager.Instance.Play(dialogue);
        }

        private bool PlayerInRange()
        {
            var player = Player.Current;
            if (player == null) return false;

            // 只比较 X/Y，忽略 Z（触发器 z 可能非 0，玩家 z=0，用 Bounds.Contains 会误判）
            Vector3 pos = transform.position;
            return Mathf.Abs(player.Position.x - pos.x) <= triggerSize.x * 0.5f
                && Mathf.Abs(player.Position.y - pos.y) <= triggerSize.y * 0.5f;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 1f, 0.4f, 0.4f);
            Gizmos.DrawCube(transform.position, triggerSize);
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, triggerSize);
        }
    }
}
