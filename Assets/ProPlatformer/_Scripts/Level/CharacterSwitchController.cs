using UnityEngine;

namespace Myd.Platform
{
    /// <summary>
    /// 双角色切换控制器：Tab键/手柄左肩键切换女儿↔母亲
    /// 切换时：换精灵帧 + 换属性 + 移动相机到对应背景 + 传送玩家
    /// 仅挂在有此组件的场景中才生效（scence2_3 / scence2_4）
    /// </summary>
    public class CharacterSwitchController : MonoBehaviour
    {
        [Header("背景参照")]
        [Tooltip("女儿对应的背景（庭院新）")]
        [SerializeField] private Transform backgroundA;
        [Tooltip("母亲对应的背景（庭院老）")]
        [SerializeField] private Transform backgroundB;

        [Header("出生点")]
        [Tooltip("女儿场景出生点")]
        [SerializeField] private Vector2 spawnA = new Vector2(0f, -2f);
        [Tooltip("母亲场景出生点")]
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

        private void SwitchCharacter()
        {
            var playerRenderer = FindObjectOfType<PlayerRenderer>();
            if (playerRenderer == null) return;

            int currentChar = playerRenderer.ActiveCharacter;
            int newChar = currentChar == 0 ? 1 : 0;

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

            // 4. 传送玩家到对应出生点
            Vector2 spawn = newChar == 0 ? spawnA : spawnB;
            var ctrlField = typeof(Player).GetField("playerController",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (ctrlField != null)
            {
                var ctrl = ctrlField.GetValue(Player.Current) as PlayerController;
                ctrl?.Respawn(spawn);
            }

            Debug.Log($"[CharacterSwitch] Switched to {(newChar == 0 ? "女儿" : "母亲")} at {spawn}");
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            if (backgroundA != null)
            {
                Gizmos.DrawWireSphere(backgroundA.position, 1f);
                UnityEditor.Handles.Label(backgroundA.position + Vector3.up * 2f, "女儿背景");
            }
            if (backgroundB != null)
            {
                Gizmos.DrawWireSphere(backgroundB.position, 1f);
                UnityEditor.Handles.Label(backgroundB.position + Vector3.up * 2f, "母亲背景");
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(spawnA, 0.5f);
            UnityEditor.Handles.Label((Vector3)(Vector2)spawnA + Vector3.up * 1.5f, "女儿出生点");
            Gizmos.DrawWireSphere(spawnB, 0.5f);
            UnityEditor.Handles.Label((Vector3)(Vector2)spawnB + Vector3.up * 1.5f, "母亲出生点");
        }
    }
}
