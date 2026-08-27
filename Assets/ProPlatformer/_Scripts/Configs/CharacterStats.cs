using UnityEngine;

namespace Myd.Platform
{
    /// <summary>
    /// 角色属性配置，可区分母女角色
    /// 女儿：高机动性，二段跳，耐力较低
    /// </summary>
    [CreateAssetMenu(fileName = "DaughterStats", menuName = "ProPlatformer/角色属性")]
    public class CharacterStats : ScriptableObject
    {
        [Header("基础移动")]
        public float maxRun = 9f;
        public float runAccel = 100f;
        public float runReduce = 80f;
        public float airMult = 0.65f;

        [Header("跳跃")]
        public float jumpSpeed = 12f;
        public float jumpHBoost = 5f;
        public float varJumpTime = 0.18f;
        public int maxAirJumps = 1;       // 二段跳次数

        [Header("冲刺")]
        public int maxDashes = 1;
        public float dashSpeed = 14f;
        public float dashTime = 0.18f;

        [Header("攀爬耐力")]
        public float maxStamina = 55f;
        public float climbUpCost = 100f / 2.2f;
        public float climbStillCost = 100f / 10f;
        public float climbJumpCost = 110f / 4f;
        public float climbUpSpeed = 4.5f;
        public float climbDownSpeed = -8f;
        public float climbSlipSpeed = -3f;
        public float climbAccel = 90f;

        [Header("墙跳")]
        public float wallJumpHSpeed = 14f;
        public float wallSlideTime = 1.2f;

        /// <summary>
        /// 应用到 Constants 全局参数
        /// </summary>
        public void ApplyToConstants()
        {
            Constants.MaxRun = maxRun;
            Constants.RunAccel = runAccel;
            Constants.RunReduce = runReduce;
            Constants.AirMult = airMult;

            Constants.JumpSpeed = jumpSpeed;
            Constants.JumpHBoost = jumpHBoost;
            Constants.VarJumpTime = varJumpTime;

            Constants.MaxDashes = maxDashes;
            Constants.MaxAirJumps = maxAirJumps;
            Constants.DashSpeed = dashSpeed;
            Constants.DashTime = dashTime;

            Constants.ClimbMaxStamina = maxStamina;
            Constants.ClimbUpCost = climbUpCost;
            Constants.ClimbStillCost = climbStillCost;
            Constants.ClimbJumpCost = climbJumpCost;
            Constants.ClimbUpSpeed = climbUpSpeed;
            Constants.ClimbDownSpeed = climbDownSpeed;
            Constants.ClimbSlipSpeed = climbSlipSpeed;
            Constants.ClimbAccel = climbAccel;

            Constants.WallJumpHSpeed = wallJumpHSpeed;
            Constants.WallSlideTime = wallSlideTime;
        }
    }
}
