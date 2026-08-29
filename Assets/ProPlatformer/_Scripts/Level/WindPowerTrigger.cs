using UnityEngine;

namespace Myd.Platform
{
    /// <summary>
    /// 风之力量触发器：触发后进入"疾风模式"——
    /// 相机拉远（正交尺寸增大）、角色属性增强（速度/跳跃/冲刺）、
    /// 风粒子跟随、非冲刺也出残影、播放 BGM 前40秒
    /// </summary>
    public class WindPowerTrigger : MonoBehaviour
    {
        [Header("触发设置")]
        [SerializeField] private Vector2 triggerSize = new Vector2(3f, 5f);
        [SerializeField] private bool triggerOnce = true;

        [Header("相机")]
        [Tooltip("疾风模式相机正交尺寸（默认9，拉远=视野放大）")]
        [SerializeField] private float zoomOutSize = 12f;
        [Tooltip("相机缩放过渡时长")]
        [SerializeField] private float zoomDuration = 1.2f;

        [Header("角色增强")]
        [SerializeField] private float speedMultiplier = 1.35f;
        [SerializeField] private float jumpMultiplier = 1.15f;

        [Header("BGM")]
        [SerializeField] private AudioClip windTheme;
        [Tooltip("只播放前 N 秒（到达前 2 秒开始淡出）")]
        [SerializeField] private float playSeconds = 60f;

        private bool triggered;
        private static bool windActive;

        private void Update()
        {
            if (triggered && triggerOnce) return;
            var player = Player.Current;
            if (player == null) return;

            Vector3 pos = transform.position;
            bool inRange = Mathf.Abs(player.Position.x - pos.x) <= triggerSize.x * 0.5f
                && Mathf.Abs(player.Position.y - pos.y) <= triggerSize.y * 0.5f;
            if (!inRange) return;

            triggered = true;
            Activate();
        }

        private void Activate()
        {
            windActive = true;
            Debug.Log("[WindPower] 疾风模式启动！");

            // 1. 相机拉远（协程过渡）
            StartCoroutine(ZoomCamera());

            // 2. 角色属性增强：直接改 Constants（本场景内生效）
            var stats = Resources.Load<CharacterStats>("DaughterStats");
            if (stats != null)
            {
                Constants.MaxRun *= speedMultiplier;
                Constants.RunAccel *= speedMultiplier;
                Constants.JumpSpeed *= jumpMultiplier;
                Constants.DashSpeed *= speedMultiplier;
                Constants.EndDashSpeed *= speedMultiplier;
            }

            // 3. 常驻残影（由 Game 查询 windActive 决定）
            var runner = FindObjectOfType<WindEffectRunner>();
            if (runner == null)
            {
                var go = new GameObject("WindEffectRunner");
                runner = go.AddComponent<WindEffectRunner>();
            }
            runner.enabled = true;

            // 4. BGM 播放（时长内自然结束时也淡出，被 WindEndTrigger 提前停止时同样淡出）
            if (windTheme != null)
            {
                var srcGo = new GameObject("WindBGM");
                srcGo.name = "WindBGM";
                var src = srcGo.AddComponent<AudioSource>();
                src.clip = windTheme;
                src.playOnAwake = false;
                src.spatialBlend = 0f;
                src.volume = 0.7f;
                src.Play();
                // 时长到达前 2 秒开始淡出，播完销毁
                inst = this;
                StartCoroutine(FadeOutBGM(src, playSeconds));
            }
        }

        private static WindPowerTrigger inst;

        /// <summary>
        /// BGM 淡出协程：播放 playSeconds 秒后 2 秒淡出并销毁（自然结束路径）
        /// </summary>
        private System.Collections.IEnumerator FadeOutBGM(AudioSource src, float playSec)
        {
            // 等待播放主体时长（减去淡出时长）
            float wait = Mathf.Max(playSec - 2f, 0f);
            yield return new WaitForSeconds(wait);

            // 2 秒线性淡出（若中途被 Deactivate 销毁则协程自动终止）
            float t = 0f;
            const float fade = 2f;
            while (t < fade && src != null)
            {
                t += Time.deltaTime;
                if (src != null) src.volume = Mathf.Lerp(0.7f, 0f, t / fade);
                yield return null;
            }
            if (src != null && src.gameObject != null)
                Object.Destroy(src.gameObject);
        }

        /// <summary>
        /// 结束疾风模式：恢复相机/属性/音乐/粒子（供 WindEndTrigger 调用）
        /// </summary>
        public static void Deactivate()
        {
            if (!windActive) return;
            windActive = false;
            Debug.Log("[WindPower] 疾风模式结束");

            // 1. 相机拉回正常（9）
            var cam = Camera.main;
            if (cam != null)
            {
                var trig = Object.FindObjectOfType<WindPowerTrigger>();
                if (trig != null) trig.StartCoroutine(ZoomBackCamera(cam));
            }

            // 2. 属性恢复：从 DaughterStats 重新应用原始值
            var stats = Resources.Load<CharacterStats>("DaughterStats");
            if (stats != null) stats.ApplyToConstants();

            // 3. 停止风粒子+常驻残影
            var runner = Object.FindObjectOfType<WindEffectRunner>();
            if (runner != null) runner.gameObject.SetActive(false);

            // 4. 停止 BGM——淡出而非骤停（提前触发路径）
            var bgm = GameObject.Find("WindBGM");
            if (bgm != null)
            {
                var src = bgm.GetComponent<AudioSource>();
                if (src != null && src.isPlaying)
                {
                    // 用协程 2 秒淡出后销毁
                    var trig = Object.FindObjectOfType<WindPowerTrigger>();
                    if (trig != null) trig.StartCoroutine(FadeOutAndDestroy(src, 2f));
                    else { src.Stop(); Object.Destroy(bgm); }
                }
                else
                {
                    Object.Destroy(bgm);
                }
            }
        }

        /// <summary>
        /// 从当前音量淡出到 0 后销毁（提前停止路径共用）
        /// </summary>
        private static System.Collections.IEnumerator FadeOutAndDestroy(AudioSource src, float fade)
        {
            float startVol = src != null ? src.volume : 0f;
            float t = 0f;
            while (t < fade && src != null)
            {
                t += Time.deltaTime;
                if (src != null) src.volume = Mathf.Lerp(startVol, 0f, t / fade);
                yield return null;
            }
            if (src != null && src.gameObject != null)
                Object.Destroy(src.gameObject);
        }

        private static System.Collections.IEnumerator ZoomBackCamera(Camera cam)
        {
            float from = cam.orthographicSize;
            float t = 0f;
            float dur = 1.2f;
            while (t < dur)
            {
                t += Time.deltaTime;
                cam.orthographicSize = Mathf.Lerp(from, 9f, t / dur);
                yield return null;
            }
            cam.orthographicSize = 9f;
        }

        private System.Collections.IEnumerator ZoomCamera()
        {
            var cam = Camera.main;
            if (cam == null) yield break;
            float from = cam.orthographicSize;
            float t = 0f;
            while (t < zoomDuration)
            {
                t += Time.deltaTime;
                cam.orthographicSize = Mathf.Lerp(from, zoomOutSize, t / zoomDuration);
                yield return null;
            }
            cam.orthographicSize = zoomOutSize;
        }

        public static bool IsWindActive => windActive;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.4f, 0.9f, 1f, 0.35f);
            Gizmos.DrawCube(transform.position, triggerSize);
            Gizmos.color = new Color(0.4f, 0.9f, 1f);
            Gizmos.DrawWireCube(transform.position, triggerSize);
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, "风之力量");
        }
    }

    /// <summary>
    /// 疾风效果常驻运行器：全屏风线条（稀疏三五条、不均匀）+ 常驻残影
    /// </summary>
    public class WindEffectRunner : MonoBehaviour
    {
        private ParticleSystem windParticles;
        private float trailTimer;

        private void Start()
        {
            // 全屏风线条：低频率发射（rate≈4/s ≈ 同屏3~5条存活），长寿命长线条
            var go = new GameObject("WindParticles");
            go.transform.SetParent(transform, false);
            windParticles = go.AddComponent<ParticleSystem>();
            var main = windParticles.main;
            main.duration = 999f; main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.2f); // 长寿命→线条缓慢飘过
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.09f);
            main.startColor = new Color(0.92f, 0.97f, 1f, 0.55f);
            main.maxParticles = 12; // 硬上限：同屏最多 12，实际存活 3~5 条
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            // 低频率 = 不均匀稀疏感
            var em = windParticles.emission;
            em.rateOverTime = 3.2f; // 每秒约 3 条新线，长寿命下同屏稳定 4~5 条

            // 发射源：屏幕右侧整高（粒子从右缘随机高度出现）
            var shape = windParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(0.5f, 14f, 1f); // 覆盖屏幕高的竖条

            // 每条线不同的飘行速度（不均匀感）
            var vel = windParticles.velocityOverLifetime;
            vel.enabled = true;
            vel.x = new ParticleSystem.MinMaxCurve(-26f, -16f); // 速度随机→疏密不均
            vel.y = new ParticleSystem.MinMaxCurve(-0.8f, 0.8f); // 轻微斜向

            // 材质：Sprites-Default 支持 startColor 顶点色
            var pr = go.GetComponent<ParticleSystemRenderer>();
            var mat = new Material(Shader.Find("Sprites/Default"));
            pr.material = mat;

            windParticles.Play();
        }

        private void LateUpdate()
        {
            var player = Player.Current;
            if (player == null) return;

            // 发射源跟随相机右缘（全屏覆盖，非绑角色）
            if (windParticles != null)
            {
                var cam = Camera.main;
                if (cam != null)
                {
                    float halfW = cam.orthographicSize * 16f / 9f;
                    float halfH = cam.orthographicSize;
                    Vector3 c = cam.transform.position;
                    windParticles.transform.position = new Vector3(c.x + halfW + 1f, c.y, 0f);
                }

                // 渲染器按速度拉伸成长线条
                var pr = windParticles.GetComponent<ParticleSystemRenderer>();
                if (pr != null)
                {
                    pr.renderMode = ParticleSystemRenderMode.Stretch;
                    pr.lengthScale = 4.5f;
                    pr.velocityScale = 0.15f;
                }
            }

            // 常驻残影：每 0.09s 一个（非冲刺也有拖尾）
            trailTimer -= Time.deltaTime;
            if (trailTimer <= 0)
            {
                trailTimer = 0.09f;
                var renderer = FindObjectOfType<PlayerRenderer>();
                if (renderer != null)
                    SceneEffectManager.Instance.Add(
                        typeof(PlayerRenderer).GetField("spriteRenderer",
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                            .GetValue(renderer) as SpriteRenderer,
                        (int)player.Facing, new Color(0.7f, 0.9f, 1f, 0.6f), 0.3f);
            }
        }
    }
}
