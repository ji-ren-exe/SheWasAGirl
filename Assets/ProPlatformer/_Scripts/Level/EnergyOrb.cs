using UnityEngine;

namespace Myd.Platform
{
    /// <summary>
    /// 能量球：拾取后恢复二段跳次数（金色能量球）
    /// </summary>
    public class EnergyOrb : MonoBehaviour
    {
        [Header("拾取设置")]
        [SerializeField] private float pickupDistance = 1.2f;
        [SerializeField] private float bobSpeed = 2.5f;      // 上下浮动速度
        [SerializeField] private float bobHeight = 0.18f;   // 浮动幅度
        [Header("拾取音效")]
        [SerializeField] private AudioClip pickupClip;
        [Range(0f, 1f)] [SerializeField] private float volume = 0.7f;

        private bool collected;
        private Vector3 basePos;
        private AudioSource audioSource;
        private SpriteRenderer sr;

        private void Awake()
        {
            basePos = transform.position;
            sr = GetComponent<SpriteRenderer>();
            audioSource = gameObject.GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;

            BuildOrbitParticles();
        }

        /// <summary>
        /// 环绕粒子：蓝色小光点绕球缓慢公转（能量感）
        /// </summary>
        private void BuildOrbitParticles()
        {
            var go = new GameObject("OrbitParticles");
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.duration = 999f; main.loop = true;
            main.startLifetime = 1.6f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.2f); // 向外飘一点
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color32(120, 200, 255, 200));
            main.maxParticles = 40;
            main.simulationSpace = ParticleSystemSimulationSpace.Local; // 跟随球移动

            var em = ps.emission;
            em.rateOverTime = 16f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.35f; // 从球周围发出

            // 轻微向外+切向速度（环绕感）
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.orbitalY = new ParticleSystem.MinMaxCurve(1.2f); // 绕 Y 轴公转（2D 中即绕球转）
            vel.radial = new ParticleSystem.MinMaxCurve(0.3f);    // 微微外扩

            // 淡出
            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color32(150, 215, 255, 255), 0f), new GradientColorKey(new Color32(80, 160, 255, 255), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
            fade.color = new ParticleSystem.MinMaxGradient(grad);

            var pr = go.GetComponent<ParticleSystemRenderer>();
            pr.material = new Material(Shader.Find("Sprites/Default"));

            ps.Play();
        }

        private void Update()
        {
            if (collected) return;

            // 上下浮动动画
            transform.position = basePos + Vector3.up * Mathf.Sin(Time.time * bobSpeed + basePos.x) * bobHeight;

            var player = Player.Current;
            if (player == null) return;

            if (Vector2.Distance(player.Position, transform.position) <= pickupDistance)
            {
                collected = true;
                // 恢复二段跳：airJumps 是 public 字段，反射用 Public 标志
                var ctrlField = typeof(Player).GetField("playerController",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var ctrl = ctrlField?.GetValue(player) as PlayerController;
                if (ctrl != null)
                {
                    var airField = typeof(PlayerController).GetField("airJumps",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    airField?.SetValue(ctrl, Constants.MaxAirJumps);
                    Debug.Log($"[EnergyOrb] 二段跳已恢复: airJumps={ctrl.airJumps}/{Constants.MaxAirJumps}");
                }

                // 音效（延迟隐藏让声音播完）
                if (pickupClip != null) audioSource.PlayOneShot(pickupClip, volume);
                if (sr != null) sr.enabled = false;
                Destroy(gameObject, pickupClip != null ? pickupClip.length : 0.1f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, pickupDistance);
        }
    }
}
