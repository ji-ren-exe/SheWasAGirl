using UnityEngine;
using UnityEngine.UI;

namespace Myd.Platform
{
    /// <summary>
    /// 保险箱密码设置窗口：对话结束后弹出，输入4位密码并确认。
    /// 键盘：数字键0-9直接输入，退格删除，回车确认，Esc取消。
    /// 手柄：方向键/左摇杆选择数字（0-9 + 删除 + 确认网格），A键确认选中，B键退格。
    /// 输入期间 Time.timeScale=0 暂停（同修锁小游戏）。
    /// </summary>
    public class SafePasswordWindow : MonoBehaviour
    {
        private Canvas canvas;
        private RectTransform root;
        private Text[] digitLabels;          // 4个密码位显示
        private Text statusLabel;            // 底部提示文字
        private Button[] gridButtons;        // 0-9 / 删除 / 确认（12格）
        private int selectedGridIndex;       // 当前手柄选中格（0-11）
        private string password = "";        // 已输入的密码
        private const int PasswordLength = 4;

        private System.Action<string> onConfirm;
        private System.Action onCancel;
        private bool finished;

        public void Show(System.Action<string> confirmCallback, System.Action<string> cancelCallback = null, string titleText = "设置保险箱密码")
        {
            onConfirm = confirmCallback;
            onCancel = () => cancelCallback?.Invoke("");
            this.titleText = string.IsNullOrEmpty(titleText) ? "设置保险箱密码" : titleText;

            // Game.UpdateTime 会把 timeScale 拉回 1，必须用 UIBusy 标志真正冻结角色
            Game.UIBusy = true;
            Time.timeScale = 0f;
            BuildUI();
        }

        private string titleText;

        private void BuildUI()
        {
            // 画布（ScreenSpaceOverlay，最高层）
            var go = new GameObject("SafePasswordCanvas");
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 600;
            go.AddComponent<GraphicRaycaster>();

            // 半透明全屏遮罩（点击取消）
            var blocker = new GameObject("Blocker").AddComponent<RectTransform>();
            blocker.SetParent(canvas.transform, false);
            blocker.anchorMin = Vector2.zero;
            blocker.anchorMax = Vector2.one;
            blocker.offsetMin = Vector2.zero;
            blocker.offsetMax = Vector2.zero;
            var blockerImg = blocker.gameObject.AddComponent<Image>();
            blockerImg.color = new Color(0f, 0f, 0f, 0.6f);
            blockerImg.raycastTarget = true;
            var blockerBtn = blocker.gameObject.AddComponent<Button>();
            blockerBtn.transition = Selectable.Transition.None;
            blockerBtn.onClick.AddListener(() => Cancel());

            // 主面板：屏幕居中（默认锚点即中心），高度按屏幕自动收缩防出界
            root = new GameObject("Panel").AddComponent<RectTransform>();
            root.SetParent(canvas.transform, false);
            root.sizeDelta = new Vector2(340f, 420f);
            // 屏幕高度不足时整体缩小，保证完整显示（最小 60%）
            float maxPanelH = Screen.height * 0.92f;
            if (root.sizeDelta.y > maxPanelH)
            {
                float k = maxPanelH / root.sizeDelta.y;
                root.localScale = new Vector3(k, k, 1f);
            }
            var panelImg = root.gameObject.AddComponent<Image>();
            panelImg.color = new Color(0.12f, 0.11f, 0.16f, 0.95f);

            var title = MakeText("Panel/Title", root, titleText, 22);
            title.rectTransform.anchoredPosition = new Vector2(0f, 150f);

            // 密码显示区（4位下划线格子）
            var displayRoot = new GameObject("Display").AddComponent<RectTransform>();
            displayRoot.SetParent(root, false);
            displayRoot.anchoredPosition = new Vector2(0f, 92f);
            displayRoot.sizeDelta = new Vector2(280f, 50f);

            digitLabels = new Text[PasswordLength];
            for (int i = 0; i < PasswordLength; i++)
            {
                var cell = new GameObject($"Digit{i}").AddComponent<RectTransform>();
                cell.SetParent(displayRoot, false);
                cell.sizeDelta = new Vector2(50f, 50f);
                cell.anchoredPosition = new Vector2(-105f + i * 70f, 0f);
                var cellBg = cell.gameObject.AddComponent<Image>();
                cellBg.color = new Color(0.2f, 0.2f, 0.25f, 1f);
                var t = MakeText($"Text", cell, "", 26);
                t.rectTransform.anchorMin = Vector2.zero;
                t.rectTransform.anchorMax = Vector2.one;
                t.rectTransform.offsetMin = Vector2.zero;
                t.rectTransform.offsetMax = Vector2.zero;
                digitLabels[i] = t;
            }

            // 数字网格：0-9 + 删除 + 确认（4列3行）
            gridButtons = new Button[12];
            for (int i = 0; i < 12; i++)
            {
                int idx = i;
                var cell = new GameObject($"Key{i}").AddComponent<RectTransform>();
                cell.SetParent(root, false);
                cell.sizeDelta = new Vector2(64f, 48f);
                int col = i % 4, row = i / 4;
                cell.anchoredPosition = new Vector2(-111f + col * 74f, 22f - row * 60f);

                var img = cell.gameObject.AddComponent<Image>();
                img.color = new Color(0.25f, 0.25f, 0.32f, 1f);

                var btn = cell.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.ColorTint;
                gridButtons[i] = btn;

                string label;
                if (i < 10) label = i.ToString();
                else if (i == 10) label = "删";
                else label = "OK";

                var t = MakeText("Text", cell, label, 20);
                t.rectTransform.anchorMin = Vector2.zero;
                t.rectTransform.anchorMax = Vector2.one;
                t.rectTransform.offsetMin = Vector2.zero;
                t.rectTransform.offsetMax = Vector2.zero;

                btn.onClick.AddListener(() => PressKey(idx));
            }

            // 底部状态提示：按最近输入设备（键盘/手柄）切换操作说明
            statusLabel = MakeText("Status", root, "输入4位密码（键盘直接按数字）", 13);
            statusLabel.rectTransform.anchoredPosition = new Vector2(0f, -170f);
            statusLabel.rectTransform.sizeDelta = new Vector2(320f, 20f);
            statusLabel.color = new Color(0.75f, 0.78f, 0.85f);

            // 键盘/手柄操作说明：同一位置，按最近输入设备切换显示
            keyHint = MakeText("KeyHint", root, "键盘：数字键输入  退格删除  回车确认", 12);
            keyHint.rectTransform.anchoredPosition = new Vector2(0f, -188f);
            keyHint.rectTransform.sizeDelta = new Vector2(320f, 18f);
            keyHint.color = new Color(0.6f, 0.62f, 0.7f);

            padHint = MakeText("PadHint", root, "手柄：方向键/摇杆选格  A输入  B退格", 12);
            padHint.rectTransform.anchoredPosition = new Vector2(0f, -188f);
            padHint.rectTransform.sizeDelta = new Vector2(320f, 18f);
            padHint.color = new Color(0.45f, 0.75f, 1f);
            padHint.gameObject.SetActive(false);

            selectedGridIndex = 0;
            UpdateSelection();
            UpdateDisplay();
        }

        private Text keyHint;
        private Text padHint;

        private Text MakeText(string name, RectTransform parent, string text, int size)
        {
            var go = new GameObject(name);
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.fontSize = size;
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            t.text = text;
            t.raycastTarget = false;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.font = font;
            return t;
        }

        private void Update()
        {
            if (finished || root == null) return;

            // 手柄提示跟随最近输入设备切换（GameInput.UsingGamepad 全局跟踪）
            if (padHint != null && keyHint != null)
            {
                bool pad = GameInput.UsingGamepad;
                if (padHint.gameObject.activeSelf != pad)
                {
                    padHint.gameObject.SetActive(pad);
                    keyHint.gameObject.SetActive(!pad);
                    UpdateDisplay(); // 状态文字同步切换设备文案
                }
            }

            // --- 键盘：数字直接输入 ---
            for (int i = (int)KeyCode.Alpha0; i <= (int)KeyCode.Alpha0 + 9; i++)
                if (Input.GetKeyDown((KeyCode)i)) PressKey(i - (int)KeyCode.Alpha0);
            for (int i = (int)KeyCode.Keypad0; i <= (int)KeyCode.Keypad0 + 9; i++)
                if (Input.GetKeyDown((KeyCode)i)) PressKey(i - (int)KeyCode.Keypad0);

            if (Input.GetKeyDown(KeyCode.Backspace)) PressKey(10);
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) PressKey(11);
            if (Input.GetKeyDown(KeyCode.Escape)) { Cancel(); return; }

            // --- 手柄/键盘导航：方向移动选中格 ---
            bool left = Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.JoystickButton11) || Input.GetAxisRaw("Horizontal") < -0.5f && !navHeld;
            bool right = Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.JoystickButton12) || Input.GetAxisRaw("Horizontal") > 0.5f && !navHeld;
            bool up = Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.JoystickButton13) || Input.GetAxisRaw("Vertical") > 0.5f && !navHeld;
            bool down = Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.JoystickButton14) || Input.GetAxisRaw("Vertical") < -0.5f && !navHeld;

            if (left || right || up || down)
            {
                int col = selectedGridIndex % 4, row = selectedGridIndex / 4;
                if (left) col = (col + 3) % 4;
                if (right) col = (col + 1) % 4;
                if (up) row = Mathf.Max(0, row - 1);
                if (down) row = Mathf.Min(2, row + 1);
                selectedGridIndex = row * 4 + col;
                UpdateSelection();
                navHeld = true;
            }
            if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) < 0.3f && Mathf.Abs(Input.GetAxisRaw("Vertical")) < 0.3f)
                navHeld = false;

            // 手柄 A(0)=确认选中格，B(1)=退格
            if (Input.GetKeyDown(KeyCode.JoystickButton0)) PressKey(selectedGridIndex);
            if (Input.GetKeyDown(KeyCode.JoystickButton1)) PressKey(10);
        }

        private bool navHeld;

        private void PressKey(int idx)
        {
            if (finished) return;
            if (idx < 10)
            {
                if (password.Length < PasswordLength)
                {
                    password += idx.ToString();
                    UpdateDisplay();
                }
            }
            else if (idx == 10) // 删除
            {
                if (password.Length > 0)
                {
                    password = password.Substring(0, password.Length - 1);
                    UpdateDisplay();
                }
            }
            else if (idx == 11) // 确认
            {
                if (password.Length == PasswordLength)
                    Confirm();
                else
                    statusLabel.text = "请先输入完整的4位密码";
            }
        }

        private void UpdateDisplay()
        {
            for (int i = 0; i < PasswordLength; i++)
                digitLabels[i].text = i < password.Length ? password[i].ToString() : "_";
            bool pad = GameInput.UsingGamepad;
            if (password.Length == PasswordLength)
                statusLabel.text = pad ? "按 OK / A键 确认密码" : "按 OK / 回车 确认密码";
            else
                statusLabel.text = pad ? "用方向键/摇杆选数字，按A输入" : "输入4位密码（键盘直接按数字）";
        }

        private void UpdateSelection()
        {
            for (int i = 0; i < gridButtons.Length; i++)
            {
                var img = gridButtons[i].GetComponent<Image>();
                if (i == selectedGridIndex)
                    img.color = new Color(0.42f, 0.55f, 0.95f, 1f); // 选中高亮
                else
                    img.color = new Color(0.25f, 0.25f, 0.32f, 1f);
            }
        }

        private void Confirm()
        {
            finished = true;
            Game.UIBusy = false;
            Time.timeScale = 1f;
            onConfirm?.Invoke(password);
            Close();
        }

        private void Cancel()
        {
            if (finished) return;
            finished = true;
            Game.UIBusy = false;
            Time.timeScale = 1f;
            onCancel?.Invoke();
            Close();
        }

        private void Close()
        {
            if (canvas != null)
                Destroy(canvas.gameObject);
            Destroy(gameObject);
        }
    }
}
