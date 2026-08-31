using System.Collections;
using UnityEngine;

namespace Myd.Platform
{
    /// <summary>
    /// 角色能力限制（按角色ID全局门控）：0=女儿, 1=母亲1(中年), 2=母亲2(年轻), 3=母亲3(老年), 4=母亲4(最老)
    /// 母亲3：不能冲刺、不能二段跳
    /// 母亲4：不能跳跃、不能冲刺、不能二段跳；但免疫尖刺死亡（受击只震动不复活重置）
    /// </summary>
    public static class CharacterAbilities
    {
        /// <summary>当前角色ID，由 PlayerRenderer.SwitchCharacter 同步</summary>
        public static int CurrentCharacter = 0;

        /// <summary>能否跳跃（母亲4不能）</summary>
        public static bool AllowJump => CurrentCharacter != 4;

        /// <summary>能否冲刺（母亲3/4不能）</summary>
        public static bool AllowDash => CurrentCharacter <= 2;

        /// <summary>能否二段跳（母亲3/4不能）</summary>
        public static bool AllowDoubleJump => CurrentCharacter <= 2;

        /// <summary>免疫尖刺死亡（母亲4）：受击只触发震动反馈</summary>
        public static bool SpikeImmune => CurrentCharacter == 4;

        // ---- 受击反馈（母亲4踩尖刺）----
        private static float lastHurtTime = -10f;

        /// <summary>
        /// 受击反馈：镜头震动 + 手柄震动（带冷却防连触）
        /// </summary>
        public static void PlayHurtFeedback()
        {
            if (Time.time - lastHurtTime < 0.8f) return;
            lastHurtTime = Time.time;

            // 镜头震动
            var cam = Object.FindObjectOfType<SceneCamera>();
            cam?.Shake(Vector2.up, 0.35f);

            // 手柄震动
            RumbleDriver.Play(0.65f, 0.3f);

            // 僵直 0.5s + 面朝反方向微小后退（滑退约 0.7 世界单位）
            Player.Current?.Controller?.HurtStun(0.5f, 4f);
        }
    }

    /// <summary>
    /// 手柄震动驱动：隐藏常驻对象上挂协程，XInputSetState 延迟归零
    /// </summary>
    internal class RumbleDriver : MonoBehaviour
    {
        private static RumbleDriver instance;

        public static void Play(float strength, float duration)
        {
            if (instance == null)
            {
                var go = new GameObject("XInputRumbleDriver");
                instance = go.AddComponent<RumbleDriver>();
                Object.DontDestroyOnLoad(go);
            }
            instance.StartCoroutine(instance.RumbleRoutine(strength, duration));
        }

        private IEnumerator RumbleRoutine(float strength, float duration)
        {
            XInputRumble.Set(strength, strength);
            yield return new WaitForSecondsRealtime(duration);
            XInputRumble.Set(0f, 0f);
        }
    }

    /// <summary>
    /// XInput 手柄震动（P/Invoke，Windows；无手柄/无DLL时静默失败）
    /// </summary>
    internal static class XInputRumble
    {
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct XINPUT_VIBRATION
        {
            public ushort LeftMotor;
            public ushort RightMotor;
        }

        [System.Runtime.InteropServices.DllImport("xinput1_4.dll")]
        private static extern uint XInputSetState(uint dwUserIndex, ref XINPUT_VIBRATION pVibration);

        private static bool failed;

        public static void Set(float left, float right)
        {
            if (failed) return;
            try
            {
                var vib = new XINPUT_VIBRATION
                {
                    LeftMotor = (ushort)Mathf.Clamp01(left).Map(0f, 1f, 0f, 65535f),
                    RightMotor = (ushort)Mathf.Clamp01(right).Map(0f, 1f, 0f, 65535f)
                };
                XInputSetState(0, ref vib);
            }
            catch (System.DllNotFoundException) { failed = true; }
            catch (System.EntryPointNotFoundException) { failed = true; }
        }
    }

    internal static class FloatExt
    {
        public static float Map(this float v, float a1, float a2, float b1, float b2)
        {
            return b1 + (v - a1) * (b2 - b1) / (a2 - a1);
        }
    }
}
