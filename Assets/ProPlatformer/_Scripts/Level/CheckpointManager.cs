using UnityEngine;

namespace Myd.Platform
{
    /// <summary>
    /// 复活点管理器：跟踪当前激活的复活点，死亡后把玩家传送回去
    /// 挂在场景任意对象上（如 Game）
    /// 辅助模式：连续死亡达到次数后，跳过当前复活点到下一个更远的复活点
    /// </summary>
    public class CheckpointManager : MonoBehaviour
    {
        public static CheckpointManager Instance { get; private set; }

        [Tooltip("死亡后短暂冻帧（秒），营造死亡顿帧感）")]
        [SerializeField] private float deathFreezeTime = 0.25f;

        [Header("连死跳点（辅助模式）")]
        [Tooltip("连续死亡多少次后跳到下一个复活点（0=关闭该功能）")]
        [SerializeField] private int deathsBeforeSkip = 3;
        [Tooltip("连死计数重置的间隔（秒）：该时间内未死亡则清零计数")]
        [SerializeField] private float counterResetTime = 60f;

        private Checkpoint currentCheckpoint;
        private float respawnCooldown;
        // 连死计数与计时
        private int consecutiveDeaths;
        private float lastDeathTime;
        // 按 X 坐标排序的全部激活复活点（跳点用）
        private System.Collections.Generic.List<Checkpoint> activatedCheckpoints = new System.Collections.Generic.List<Checkpoint>();

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            if (respawnCooldown > 0)
                respawnCooldown -= Time.deltaTime;

            // 连死计数超时清零
            if (consecutiveDeaths > 0 && Time.time - lastDeathTime > counterResetTime)
                consecutiveDeaths = 0;
        }

        public void ActivateCheckpoint(Checkpoint checkpoint)
        {
            currentCheckpoint = checkpoint;
            if (!activatedCheckpoints.Contains(checkpoint))
                activatedCheckpoints.Add(checkpoint);
            // 按 X 排序（关卡推进方向）
            activatedCheckpoints.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));

            // 到达新复活点 = 推进成功，连死计数清零
            consecutiveDeaths = 0;
        }

        /// <summary>
        /// 死亡复活：回到当前激活的复活点（无则回关卡出生点）
        /// </summary>
        public void RespawnPlayer()
        {
            if (respawnCooldown > 0) return;
            respawnCooldown = 0.5f; // 防止连续触发

            var player = Player.Current;
            if (player == null) return;

            // 连死计数
            consecutiveDeaths++;
            lastDeathTime = Time.time;

            // 复活位置：激活的复活点，或关卡出生点
            Vector2 respawnPos;
            if (currentCheckpoint != null)
                respawnPos = currentCheckpoint.GetRespawnPosition();
            else
                respawnPos = FindSpawnPosition();

            // 连死达到阈值：跳到右侧下一个未激活的检查点（跳过难关推进）
            if (deathsBeforeSkip > 0 && consecutiveDeaths >= deathsBeforeSkip)
            {
                Checkpoint next = FindNextCheckpoint();
                if (next != null)
                {
                    currentCheckpoint = next;
                    respawnPos = next.GetRespawnPosition();
                    consecutiveDeaths = 0; // 跳点后计数清零
                    Debug.Log($"[Checkpoint] 连续死亡{deathsBeforeSkip}次，跳转到下一检查点 @{respawnPos}");
                }
            }

            // 通过控制器内部字段传送（直接改 transform 会被控制器每帧覆盖）
            var ctrlField = typeof(Player).GetField("playerController",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (ctrlField == null) return;
            var ctrl = ctrlField.GetValue(player) as PlayerController;
            if (ctrl == null) return;

            ctrl.Respawn(respawnPos);

            // 死亡冻帧
            var game = FindObjectOfType<Game>();
            if (game != null && deathFreezeTime > 0)
                game.Freeze(deathFreezeTime);
        }

        /// <summary>
        /// 找比当前复活点更远的下一个复活点（右侧第一个未激活的检查点——跳过难关推进）
        /// </summary>
        private Checkpoint FindNextCheckpoint()
        {
            // 基准：当前复活点（无则用玩家当前位置）
            float curX = currentCheckpoint != null
                ? currentCheckpoint.transform.position.x
                : (Player.Current != null ? Player.Current.Position.x : 0f);

            // 搜全场景所有 Checkpoint（含未激活的）
            Checkpoint best = null;
            foreach (var cp in Object.FindObjectsOfType<Checkpoint>())
            {
                float x = cp.transform.position.x;
                // 取右侧最近的（无论激活与否）
                if (x > curX && (best == null || x < best.transform.position.x))
                    best = cp;
            }
            return best;
        }

        private Vector2 FindSpawnPosition()
        {
            var level = FindObjectOfType<Level>();
            return level != null ? level.StartPosition : Vector2.zero;
        }
    }
}
