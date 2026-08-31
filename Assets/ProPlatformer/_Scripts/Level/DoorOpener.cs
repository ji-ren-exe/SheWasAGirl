using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Myd.Platform
{
    /// <summary>
    /// 开门触发器：靠近显示"按 X 开门"，按X后黑屏切换场景。
    /// 由 InteractableObject 在小游戏成功后动态创建。
    /// </summary>
    public class DoorOpener : MonoBehaviour
    {
        [HideInInspector] public string targetSceneName = "scence2_4";
        public Vector2 interactRange = new Vector2(3f, 4f);

        private RectTransform hintRoot;
        private Text hintLabel;
        private bool triggered;

        private void Start()
        {
            BuildHintUI();
        }

        private void BuildHintUI()
        {
            Canvas canvas = null;
            var playerRenderer = FindObjectOfType<PlayerRenderer>();
            if (playerRenderer != null)
            {
                // 等 DialogueManager 的 Canvas
                var dm = FindObjectOfType<Dialogue.DialogueManager>();
                if (dm != null) canvas = dm.GetComponent<Canvas>();
            }
            if (canvas == null) canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                var go = new GameObject("DoorOpenerCanvas");
                canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 500;
                go.AddComponent<CanvasScaler>();
            }

            hintRoot = new GameObject("DoorHint").AddComponent<RectTransform>();
            hintRoot.SetParent(canvas.transform, false);
            hintRoot.sizeDelta = new Vector2(180f, 51f);
            hintRoot.gameObject.SetActive(false);

            var bg = hintRoot.gameObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.6f);
            bg.raycastTarget = false;

            var textGo = new GameObject("HintText");
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.SetParent(hintRoot, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(6f, 3f);
            textRect.offsetMax = new Vector2(-6f, -3f);
            hintLabel = textGo.AddComponent<Text>();
            hintLabel.fontSize = 27;
            hintLabel.color = Color.white;
            hintLabel.alignment = TextAnchor.MiddleCenter;
            hintLabel.text = "按 X 开门";
            hintLabel.raycastTarget = false;

            Font font = Resources.Load<Font>("NotoSansSC-Regular");
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hintLabel.font = font;
        }

        private void Update()
        {
            if (triggered) return;
            var player = Player.Current;
            if (player == null) return;

            Vector3 pos = transform.position;
            bool inRange = Mathf.Abs(player.Position.x - pos.x) <= interactRange.x * 0.5f
                && Mathf.Abs(player.Position.y - pos.y) <= interactRange.y * 0.5f;

            if (inRange)
            {
                ShowHint();
                if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.JoystickButton2))
                {
                    triggered = true;
                    HideHint();
                    StartCoroutine(TransitionRoutine());
                }
            }
            else
            {
                HideHint();
            }
        }

        private void ShowHint()
        {
            if (hintRoot == null) return;
            hintRoot.gameObject.SetActive(true);
            var cam = Camera.main;
            if (cam == null) return;
            Vector2 worldPos = transform.position + Vector3.up * 1.2f;
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
            var canvas = hintRoot.GetComponentInParent<Canvas>();
            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform, screenPos, canvas.worldCamera, out localPos);
            hintRoot.anchoredPosition = localPos;
        }

        private void HideHint()
        {
            if (hintRoot != null && hintRoot.gameObject.activeSelf)
                hintRoot.gameObject.SetActive(false);
        }

        private System.Collections.IEnumerator TransitionRoutine()
        {
            // 切场景前强制结束对话
            var dm = FindObjectOfType<Dialogue.DialogueManager>();
            if (dm != null && dm.IsPlaying)
            {
                dm.StopDialogue();
            }

            // 黑屏淡入
            var go = new GameObject("DoorFadeCanvas");
            DontDestroyOnLoad(go);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;
            go.AddComponent<CanvasScaler>();

            var imgGo = new GameObject("FadeImage");
            var rect = imgGo.AddComponent<RectTransform>();
            rect.SetParent(canvas.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var img = imgGo.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0);
            img.raycastTarget = false;

            float t = 0f;
            while (t < 0.4f)
            {
                t += Time.unscaledDeltaTime;
                img.color = new Color(0, 0, 0, Mathf.Clamp01(t / 0.4f));
                yield return null;
            }
            img.color = Color.black;

            // 加载场景
            SceneManager.LoadScene(targetSceneName);

            // 本协程宿主（DoorOpener）随旧场景卸载销毁而中止，
            // 淡出必须由挂在 DontDestroyOnLoad 黑屏 Canvas 上的 SceneFadeOut 接管，
            // 否则黑屏永不淡出（卡死黑屏即此 bug）
            go.AddComponent<SceneFadeOut>();
            SceneFadeOut.Begin(0.5f);
        }

        private void OnDestroy()
        {
            if (hintRoot != null) Destroy(hintRoot.gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.3f);
            Gizmos.DrawCube(transform.position, interactRange);
            Gizmos.color = new Color(0.3f, 0.8f, 1f);
            Gizmos.DrawWireCube(transform.position, interactRange);
        }
    }
}
