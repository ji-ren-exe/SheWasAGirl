using System;
using UnityEngine;
using UnityEngine.UI;

namespace Myd.Platform
{
    /// <summary>
    /// 修锁小游戏：三个同心圆环各有缺口，旋转到缺口对齐即成功。
    /// 像素风：程序化绘制圆环贴图。A/D切换圆环，W/S旋转，手柄左摇杆同效。
    /// </summary>
    public class LockPickingGame : MonoBehaviour
    {
        private const int RingCount = 3;
        private const int TexSize = 256;
        private const float SnapThreshold = 12f;

        private float[] ringAngles = new float[RingCount];
        private int selectedRing;
        private bool isPlaying;
        private bool isDone;

        private Canvas canvas;
        private Image[] ringImages;
        private Image indicator;
        private Text statusText;
        private float rotationStep = 15f;

        private Action onSuccess;
        private Action onCancel;

        private float inputCooldown;
        private const float CooldownTime = 0.2f;

        private static readonly Color SelectedColor = new Color(1f, 0.85f, 0.3f);
        private static readonly Color NormalColor = new Color(0.7f, 0.7f, 0.7f);
        private static readonly Color SuccessColor = new Color(0.3f, 1f, 0.4f);

        private const int RingThickness = 10;
        private const int GapAngleWidth = 40;
        private static readonly int[] RingRadii = { 110, 80, 50 };

        public void StartGame(Action onSuccess = null, Action onCancel = null)
        {
            this.onSuccess = onSuccess;
            this.onCancel = onCancel;

            for (int i = 0; i < RingCount; i++)
                ringAngles[i] = UnityEngine.Random.Range(0, 360);

            selectedRing = 0;
            isPlaying = true;
            isDone = false;
            inputCooldown = 0f;

            BuildUI();
            UpdateRingVisuals();
            Debug.Log("[LockPickingGame] Started");
        }

        private void Update()
        {
            if (!isPlaying || isDone) return;

            inputCooldown -= Time.unscaledDeltaTime;
            if (inputCooldown > 0f) return;

            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow) || h < -0.5f)
            {
                selectedRing = (selectedRing - 1 + RingCount) % RingCount;
                inputCooldown = CooldownTime;
                UpdateRingVisuals();
            }
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow) || h > 0.5f)
            {
                selectedRing = (selectedRing + 1) % RingCount;
                inputCooldown = CooldownTime;
                UpdateRingVisuals();
            }

            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || v > 0.5f)
            {
                ringAngles[selectedRing] = (ringAngles[selectedRing] + rotationStep) % 360f;
                inputCooldown = CooldownTime;
                UpdateRingVisuals();
                CheckAlignment();
            }
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow) || v < -0.5f)
            {
                ringAngles[selectedRing] = (ringAngles[selectedRing] - rotationStep + 360f) % 360f;
                inputCooldown = CooldownTime;
                UpdateRingVisuals();
                CheckAlignment();
            }

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton1))
            {
                Cancel();
            }
        }

        private void CheckAlignment()
        {
            float refAngle = ringAngles[0];
            for (int i = 1; i < RingCount; i++)
            {
                float diff = Mathf.Abs(AngleDiff(ringAngles[i], refAngle));
                if (diff > SnapThreshold) return;
            }

            isDone = true;
            isPlaying = false;

            for (int i = 0; i < RingCount; i++)
            {
                if (ringImages[i] != null)
                    ringImages[i].color = SuccessColor;
            }

            if (statusText != null)
                statusText.text = "咔嗒！锁开了！";

            Debug.Log("[LockPickingGame] Success!");
            StartCoroutine(DelayedCallback(1.5f, () => { onSuccess?.Invoke(); Cleanup(); }));
        }

        private void Cancel()
        {
            isPlaying = false;
            isDone = true;
            Debug.Log("[LockPickingGame] Cancelled");
            onCancel?.Invoke();
            Cleanup();
        }

        private System.Collections.IEnumerator DelayedCallback(float delay, Action callback)
        {
            float t = 0f;
            while (t < delay) { t += Time.unscaledDeltaTime; yield return null; }
            callback?.Invoke();
        }

        private static float AngleDiff(float a, float b)
        {
            float d = (a - b) % 360f;
            if (d > 180f) d -= 360f;
            if (d < -180f) d += 360f;
            return d;
        }

        private void BuildUI()
        {
            var go = new GameObject("LockPickingCanvas");
            DontDestroyOnLoad(go);
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10000;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            go.AddComponent<GraphicRaycaster>();

            var bgGo = new GameObject("BG");
            var bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.SetParent(canvas.transform, false);
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.05f, 0.05f, 0.08f, 0.95f);
            bgImg.raycastTarget = true;

            var containerGo = new GameObject("LockContainer");
            var containerRect = containerGo.AddComponent<RectTransform>();
            containerRect.SetParent(canvas.transform, false);
            containerRect.anchoredPosition = Vector2.zero;
            containerRect.sizeDelta = new Vector2(TexSize, TexSize);

            ringImages = new Image[RingCount];
            for (int i = 0; i < RingCount; i++)
            {
                var ringGo = new GameObject($"Ring_{i}");
                var ringRect = ringGo.AddComponent<RectTransform>();
                ringRect.SetParent(containerRect, false);
                ringRect.anchoredPosition = Vector2.zero;
                ringRect.sizeDelta = new Vector2(TexSize, TexSize);
                ringImages[i] = ringGo.AddComponent<Image>();
                ringImages[i].sprite = CreateRingSprite(i);
                ringImages[i].color = NormalColor;
                ringImages[i].raycastTarget = false;
                ringRect.pivot = new Vector2(0.5f, 0.5f);
            }

            var arrowGo = new GameObject("Indicator");
            var arrowRect = arrowGo.AddComponent<RectTransform>();
            arrowRect.SetParent(containerRect, false);
            arrowRect.anchoredPosition = new Vector2(0, TexSize * 0.5f + 10);
            arrowRect.sizeDelta = new Vector2(20, 20);
            indicator = arrowGo.AddComponent<Image>();
            indicator.sprite = CreateArrowSprite();
            indicator.color = new Color(1f, 0.3f, 0.3f);
            indicator.raycastTarget = false;

            var textGo = new GameObject("StatusText");
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.SetParent(canvas.transform, false);
            textRect.anchoredPosition = new Vector2(0, -TexSize * 0.5f - 60);
            textRect.sizeDelta = new Vector2(600, 60);
            statusText = textGo.AddComponent<Text>();
            statusText.fontSize = 24;
            statusText.color = new Color(0.8f, 0.8f, 0.85f);
            statusText.alignment = TextAnchor.MiddleCenter;
            statusText.text = "A/D 切换圆环  W/S 旋转  Esc 取消";
            statusText.raycastTarget = false;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            statusText.font = font;
        }

        private void UpdateRingVisuals()
        {
            for (int i = 0; i < RingCount; i++)
            {
                if (ringImages[i] == null) continue;
                ringImages[i].rectTransform.localRotation = Quaternion.Euler(0, 0, -ringAngles[i]);
                ringImages[i].color = (i == selectedRing && !isDone) ? SelectedColor : NormalColor;
            }
        }

        private void Cleanup()
        {
            if (canvas != null)
                Destroy(canvas.gameObject);
            Destroy(gameObject);
        }

        private Sprite CreateRingSprite(int ringIndex)
        {
            int radius = RingRadii[ringIndex];
            var tex = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var pixels = new Color32[TexSize * TexSize];
            int center = TexSize / 2;

            int gapStart = -GapAngleWidth / 2;
            int gapEnd = GapAngleWidth / 2;

            for (int y = 0; y < TexSize; y++)
            {
                for (int x = 0; x < TexSize; x++)
                {
                    int dx = x - center;
                    int dy = y - center;
                    int dist = Mathf.RoundToInt(Mathf.Sqrt(dx * dx + dy * dy));

                    if (dist >= radius - RingThickness && dist <= radius)
                    {
                        float angle = Mathf.Atan2(dx, dy) * Mathf.Rad2Deg;
                        angle = (angle + 360f) % 360f;

                        float normAngle = angle > 180f ? angle - 360f : angle;
                        if (normAngle >= gapStart && normAngle <= gapEnd)
                        {
                            pixels[y * TexSize + x] = new Color32(0, 0, 0, 0);
                        }
                        else
                        {
                            float t = (float)(dist - (radius - RingThickness)) / RingThickness;
                            byte c = (byte)(200 - (int)(t * 60));
                            pixels[y * TexSize + x] = new Color32(c, c, c, 255);
                        }
                    }
                    else
                    {
                        pixels[y * TexSize + x] = new Color32(0, 0, 0, 0);
                    }
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, TexSize, TexSize), new Vector2(0.5f, 0.5f), TexSize);
        }

        private Sprite CreateArrowSprite()
        {
            int size = 20;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int halfW = y / 2 + 1;
                    if (x >= size / 2 - halfW && x <= size / 2 + halfW)
                        pixels[y * size + x] = new Color32(255, 60, 60, 255);
                    else
                        pixels[y * size + x] = new Color32(0, 0, 0, 0);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
