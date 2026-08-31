using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Myd.Platform
{
    public enum Facings
    {
        Right = 1,
        Left = -1
    }

    public struct VirtualIntegerAxis
    {

    }
    public struct VirtualJoystick
    {
        public Vector2 Value { get => new Vector2(UnityEngine.Input.GetAxisRaw("Horizontal"), UnityEngine.Input.GetAxisRaw("Vertical"));}
    }
    public struct VisualButton
    {
        private KeyCode key;
        private KeyCode gamepadKey;   // 手柄按键（KeyCode.JoystickButtonN），None=无手柄映射
        private float bufferTime;
        private bool consumed;
        private float bufferCounter;
        public VisualButton(KeyCode key) : this(key, KeyCode.None, 0) {
        }

        public VisualButton(KeyCode key, float bufferTime) : this(key, KeyCode.None, bufferTime)
        {
        }

        public VisualButton(KeyCode key, KeyCode gamepadKey, float bufferTime)
        {
            this.key = key;
            this.gamepadKey = gamepadKey;
            this.bufferTime = bufferTime;
            this.consumed = false;
            this.bufferCounter = 0f;
        }
        public void ConsumeBuffer()
        {
            this.bufferCounter = 0f;
        }

        /// <summary>按键缓冲是否仍在（未被任何动作消费）——用于"按了键但没动作"检测</summary>
        public bool BufferActive => this.bufferCounter > 0f;

        public bool Pressed()
        {
            bool keyboard = UnityEngine.Input.GetKeyDown(key);
            bool gamepad = gamepadKey != KeyCode.None && UnityEngine.Input.GetKeyDown(gamepadKey);
            return keyboard || gamepad || (!this.consumed && (this.bufferCounter > 0f));
        }

        public bool Checked()
        {
            bool keyboard = UnityEngine.Input.GetKey(key);
            bool gamepad = gamepadKey != KeyCode.None && UnityEngine.Input.GetKey(gamepadKey);
            return keyboard || gamepad;
        }

        public void Update(float deltaTime)
        {
            this.consumed = false;
            this.bufferCounter -= deltaTime;
            bool flag = false;
            if (UnityEngine.Input.GetKeyDown(key) || UnityEngine.Input.GetKey(key))
            {
                flag = true;
                GameInput.ReportKeyboardInput();
            }
            else if (gamepadKey != KeyCode.None && (UnityEngine.Input.GetKeyDown(gamepadKey) || UnityEngine.Input.GetKey(gamepadKey)))
            {
                flag = true;
                GameInput.ReportGamepadInput();
            }
            if (UnityEngine.Input.GetKeyDown(key) || (gamepadKey != KeyCode.None && UnityEngine.Input.GetKeyDown(gamepadKey)))
            {
                this.bufferCounter = this.bufferTime;
                flag = true;
            }
            if (!flag)
            {
                this.bufferCounter = 0f;
                return;
            }
        }
    }
    public static class GameInput
    {
        // Jump: 键盘空格 / 手柄 A键(JoystickButton0)
        public static VisualButton Jump = new VisualButton(KeyCode.Space, KeyCode.JoystickButton0, 0.08f);
        // Dash: 键盘K / 手柄 右肩键RB(JoystickButton5)
        public static VisualButton Dash = new VisualButton(KeyCode.K, KeyCode.JoystickButton5, 0.08f);
        public static VirtualJoystick Aim = new VirtualJoystick();
        public static Vector2 LastAim;

        // ---- 输入设备检测 ----
        /// <summary>最近一次输入是否来自手柄（用于教学提示切换文案）</summary>
        public static bool UsingGamepad { get; private set; }
        private static float lastKeyboardTime = -999f;
        private static float lastGamepadTime = -999f;

        /// <summary>是否检测到已连接的手柄</summary>
        public static bool IsGamepadConnected
        {
            get
            {
                foreach (var name in UnityEngine.Input.GetJoystickNames())
                    if (!string.IsNullOrEmpty(name)) return true;
                return false;
            }
        }

        internal static void ReportKeyboardInput()
        {
            lastKeyboardTime = Time.unscaledTime;
            UsingGamepad = lastGamepadTime > lastKeyboardTime;
        }

        internal static void ReportGamepadInput()
        {
            lastGamepadTime = Time.unscaledTime;
            UsingGamepad = lastGamepadTime > lastKeyboardTime;
        }

        //根据当前朝向,决定移动方向.
        public static Vector2 GetAimVector(Facings defaultFacing = Facings.Right)
        {
            Vector2 value = GameInput.Aim.Value;
            //TODO 考虑辅助模式

            //TODO 考虑摇杆
            if (value == Vector2.zero)
            {
                GameInput.LastAim = Vector2.right * ((int)defaultFacing);
            }
            else
            {
                GameInput.LastAim = value;
            }
            return GameInput.LastAim.normalized;
        }

        public static void Update(float deltaTime)
        {
            Jump.Update(deltaTime);
            Dash.Update(deltaTime);

            // 摇杆/键盘移动输入的设备来源检测
            float h = UnityEngine.Input.GetAxisRaw("Horizontal");
            float v = UnityEngine.Input.GetAxisRaw("Vertical");
            if (Mathf.Abs(h) > 0.3f || Mathf.Abs(v) > 0.3f)
            {
                // 摇杆倾斜（含模拟量）视为手柄；键盘方向键/WASD 走按键路径
                if (Mathf.Abs(UnityEngine.Input.GetAxisRaw("Mouse X")) < 0.01f)
                {
                    // 有键按住（键盘）则报键盘，否则视为摇杆
                    bool anyKey = UnityEngine.Input.anyKey;
                    bool stickTilted = Mathf.Abs(h) > 0.5f || Mathf.Abs(v) > 0.5f;
                    if (anyKey) ReportKeyboardInput();
                    else if (stickTilted) ReportGamepadInput();
                }
            }
        }
    }




}
