using UnityEngine;

namespace Myd.Platform
{
    /// <summary>
    /// 双角色切换控制器：Tab键/手柄左肩键切换女儿↔母亲
    /// 切换时：换精灵帧 + 换属性 + 移动相机到对应背景 + 保持玩家位置（按对应点差值平移）
    /// 仅挂在有此组件的场景中才生效（scence2_3 / scence2_4）
    /// </summary>
    public class CharacterSwitchController : MonoBehaviour
    {
        [Header("背景参照")]
        [Tooltip("女儿对应的背景（庭院新）")]
        [SerializeField] private Transform backgroundA;
        [Tooltip("母亲对应的背景（庭院老）")]
        [SerializeField] private Transform backgroundB;

        [Header("两庭院对应点")]
        [Tooltip("女儿庭院中的一点")]
        [SerializeField] private Vector2 spawnA = new Vector2(0f, -2f);
        [Tooltip("母亲庭院中与 spawnA 相对应的一点（两点差值=切换时的平移量，保持人物原位置）")]
        [SerializeField] private Vector2 spawnB = new Vector2(0f, -35f);

        [Header("切换设置")]
        [Tooltip("切换冷却时间（秒）")]
        [SerializeField] private float switchCooldown = 0.5f;

        private float lastSwitchTime = -1f;

        private void Update()
        {
            if (Player.Current == null) return;

            // Tab键 或 手柄左肩键(JoystickButton4)
            if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.JoystickButton4))
            {
                if (Time.time - lastSwitchTime < switchCooldown) return;
                lastSwitchTime = Time.time;
                SwitchCharacter();
            }
        }

        public void SwitchCharacter()
        {
            var playerRenderer = FindObjectOfType<PlayerRenderer>();
            if (playerRenderer == null) return;

            int currentChar = playerRenderer.ActiveCharacter;
            int newChar = currentChar == 0 ? 1 : 0;

            // 0. 立即结束正在显示的对话：气泡/打字音效不随视角切换残留
            var dm = FindObjectOfType<Dialogue.DialogueManager>();
            if (dm != null && dm.IsPlaying) dm.StopDialogue();

            // 1. 切换角色精灵
            playerRenderer.SwitchCharacter(newChar);

            // 2. 切换属性
            string statsName = newChar == 0 ? "DaughterStats" : "MotherStats";
            var stats = Resources.Load<CharacterStats>(statsName);
            if (stats != null)
            {
                stats.ApplyToConstants();
                Debug.Log($"[CharacterSwitch] Stats switched to {statsName}");
            }

            // 3. 切换相机 lockTarget 到对应背景
            var level = FindObjectOfType<Level>();
            if (level != null)
            {
                level.lockTarget = newChar == 0 ? backgroundA : backgroundB;
            }

            // 4. 保持玩家原位置：按两个对应点的差值平移（庭院布局相同、整体错开，屏幕上人物原地不动）
            if (Player.Current == null) return;
            var ctrlField = typeof(Player).GetField("playerController",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (ctrlField != null)
            {
                var ctrl = ctrlField.GetValue(Player.Current) as PlayerController;
                if (ctrl != null)
                {
                    Vector2 delta = newChar == 0 ? (spawnA - spawnB) : (spawnB - spawnA);
                    ctrl.Respawn(ctrl.Position + delta);
                    Debug.Log($"[CharacterSwitch] Switched to {(newChar == 0 ? "女儿" : "母亲")} at {ctrl.Position} (position kept, delta={delta})");
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            if (backgroundA != null)
            {
                Gizmos.DrawWireSphere(backgroundA.position, 1f);
#if UNITY_EDITOR
                UnityEditor.Handles.Label(backgroundA.position + Vector3.up * 2f, "女儿背景");
#endif
            }
            if (backgroundB != null)
            {
                Gizmos.DrawWireSphere(backgroundB.position, 1f);
#if UNITY_EDITOR
                UnityEditor.Handles.Label(backgroundB.position + Vector3.up * 2f, "母亲背景");
#endif
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(spawnA, 0.5f);
            Gizmos.DrawWireSphere(spawnB, 0.5f);
#if UNITY_EDITOR
            UnityEditor.Handles.Label((Vector3)(Vector2)spawnA + Vector3.up * 1.5f, "女儿对应点");
            UnityEditor.Handles.Label((Vector3)(Vector2)spawnB + Vector3.up * 1.5f, "母亲对应点");
#endif
        }
    }
}
