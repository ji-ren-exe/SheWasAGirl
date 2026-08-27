using DG.Tweening;
using Myd.Platform.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Myd.Platform
{

    /// <summary>
    /// 这里是Unity下实现玩家表现接口
    /// 支持三状态帧动画：站立 / 跑 / 跳
    /// </summary>
    public class PlayerRenderer : MonoBehaviour, ISpriteControl
    {
        [SerializeField]
        public SpriteRenderer spriteRenderer;

        [SerializeField]
        public ParticleSystem vfxDashFlux;
        [SerializeField]
        public ParticleSystem vfxWallSlide;

        [SerializeField]
        public TrailRenderer hair;

        [SerializeField]
        public SpriteRenderer hairSprite01;
        [SerializeField]
        public SpriteRenderer hairSprite02;

        // --- 女儿帧动画 ---
        [Header("女儿动画")]
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private Sprite[] runFrames;
        [SerializeField] private Sprite[] jumpFrames;
        [SerializeField] private float idleFPS = 8f;
        [SerializeField] private float runFPS = 12f;
        [SerializeField] private float jumpFPS = 10f;
        // 像素图分辨率，用于统一缩放到碰撞盒大小
        [SerializeField] private float spritePixelHeight = 86f;

        // --- 可在 Inspector 中实时调整的碰撞盒参数 ---
        // 基于 GIF 有效像素分析：Idle 画布 85x86 像素全部为角色本体，PPU=25
        // 碰撞箱基于各 GIF 有效像素边界计算，PPU=25
        // 三种状态统一高度(70px)，宽度按各自比例，无大小突变
        [Header("碰撞箱")]
        // 碰撞箱恢复到接近原始设计：宽0.27(原0.8的1/3)，高1.1，Y=-0.25贴地
        [Header("碰撞箱")]
        [SerializeField] public Rect normalHitbox = new Rect(0f, -0.25f, 0.27f, 1.1f);
        [SerializeField] public Rect runHitbox = new Rect(0f, -0.25f, 0.27f, 1.1f);
        [SerializeField] public Rect jumpHitbox = new Rect(0f, -0.25f, 0.27f, 1.1f);
        [SerializeField] public Rect duckHitbox = new Rect(0f, -0.5f, 0.27f, 0.6f);

        // 当前动画状态（供 PlayerController 读取以切换碰撞箱）
        public string CurrentAnimName => currentAnim.ToString();

        private enum AnimState { Idle, Run, Jump }
        private AnimState currentAnim = AnimState.Idle;
        private int frameIndex;
        private float frameTimer;

        private Vector2 scale;
        private Vector2 currSpriteScale;

        // --- 音效 ---
        [Header("音效")]
        [SerializeField] private AudioClip footstepClip;
        [SerializeField] private AudioClip jumpClip;
        [SerializeField] private AudioClip heavyLandClip;
        [SerializeField] private float footstepInterval = 0.3f;
        private AudioSource audioSource;
        private float footstepTimer;
        // 记录起跳时的高度，用于判断是否为高落差落地
        private float jumpStartY;
        private bool wasInAir;

        private void Awake()
        {
            audioSource = gameObject.GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        public Vector3 SpritePosition { get => this.spriteRenderer.transform.position; }

        public void Reload()
        {
            LoadFrames();
            DisableOriginalHair();
        }

        /// <summary>
        /// 禁用原角色自带的红色头巾（hair、hairSprite01、hairSprite02）
        /// </summary>
        private void DisableOriginalHair()
        {
            if (hair != null) hair.gameObject.SetActive(false);
            if (hairSprite01 != null) hairSprite01.gameObject.SetActive(false);
            if (hairSprite02 != null) hairSprite02.gameObject.SetActive(false);
        }

        private void LoadFrames()
        {
            idleFrames = LoadAll("Assets/ProPlatformer/_Arts/Textures/Player/Daughter/DaughterIdle.png");
            runFrames  = LoadAll("Assets/ProPlatformer/_Arts/Textures/Player/Daughter/DaughterRun.png");
            jumpFrames = LoadAll("Assets/ProPlatformer/_Arts/Textures/Player/Daughter/DaughterJump.png");
        }

        private static Sprite[] LoadAll(string path)
        {
            var sprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
            var list = new List<KeyValuePair<int, Sprite>>();
            foreach (var s in sprites)
            {
                if (s is Sprite sp)
                {
                    string n = sp.name;
                    int.TryParse(n.Substring(n.LastIndexOf('_') + 1), out int idx);
                    list.Add(new KeyValuePair<int, Sprite>(idx, sp));
                }
            }
            list.Sort((a, b) => a.Key.CompareTo(b.Key));
            var result = new Sprite[list.Count];
            for (int i = 0; i < list.Count; i++) result[i] = list[i].Value;
            Debug.Log($"[PlayerRenderer] Loaded {result.Length} frames from {path}");
            return result;
        }

        public void Render(float deltaTime)
        {
            UpdateAnimation(deltaTime);

            float tempScaleX = Mathf.MoveTowards(scale.x, currSpriteScale.x, 1.75f * deltaTime);
            float tempScaleY = Mathf.MoveTowards(scale.y, currSpriteScale.y, 1.75f * deltaTime);
            this.scale = new Vector2(tempScaleX, tempScaleY);
            this.spriteRenderer.transform.localScale = scale;
        }

        private void UpdateAnimation(float deltaTime)
        {
            // 根据玩家状态决定动画
            var player = Player.Current;
            if (player == null) return;

            AnimState newAnim;
            // 使用 PlayerController 的状态信息
            bool onGround = IsOnGround();
            bool moving = Mathf.Abs(UnityEngine.Input.GetAxisRaw("Horizontal")) > 0.1f;

            if (!onGround)
                newAnim = AnimState.Jump;
            else if (moving)
                newAnim = AnimState.Run;
            else
                newAnim = AnimState.Idle;

            if (newAnim != currentAnim)
            {
                currentAnim = newAnim;
                frameIndex = 0;
                frameTimer = 0f;
            }

            Sprite[] frames;
            float fps;
            switch (currentAnim)
            {
                case AnimState.Run:  frames = runFrames;  fps = runFPS;  break;
                case AnimState.Jump:  frames = jumpFrames;  fps = jumpFPS;  break;
                default:             frames = idleFrames;  fps = idleFPS;  break;
            }

            if (frames == null || frames.Length == 0) return;

            frameTimer += deltaTime;
            float interval = 1f / fps;
            while (frameTimer >= interval)
            {
                frameTimer -= interval;
                frameIndex = (frameIndex + 1) % frames.Length;
            }

            spriteRenderer.sprite = frames[frameIndex];

            // 跑步缩小到0.8，跳跃放大到1.2，站立为1.0
            float animScale = 1f;
            if (currentAnim == AnimState.Run) animScale = 0.8f;
            else if (currentAnim == AnimState.Jump) animScale = 1.2f;
            currSpriteScale = new Vector2(animScale, animScale);

            // 脚步声：跑步时循环播放，停止/离地暂停（保留进度，恢复时从断点继续）
            if (currentAnim == AnimState.Run)
            {
                if (audioSource != null && footstepClip != null)
                {
                    if (audioSource.clip != footstepClip)
                    {
                        // 首次进入跑步：从头播放
                        audioSource.clip = footstepClip;
                        audioSource.loop = true;
                        audioSource.volume = 0.3f;
                        audioSource.Play();
                    }
                    else if (!audioSource.isPlaying)
                    {
                        // 恢复跑步：从中断处继续播放
                        audioSource.UnPause();
                    }
                }
            }
            else
            {
                // 不在跑步状态（离地或停止移动）暂停脚步声，保留播放进度
                if (audioSource != null && audioSource.clip == footstepClip)
                {
                    audioSource.Pause();
                }
                footstepTimer = 0;
            }

            // 跳跃音效：状态切到 Jump 时播放
            if (currentAnim == AnimState.Jump && !wasInAir)
            {
                PlayClip(jumpClip, 0.8f);
                jumpStartY = transform.position.y;
            }

            // 高处落地震动音效：从空中落地且落差超过 4 个角色身高(4×2.68≈10.7)
            float heavyLandThreshold = 10.7f;
            if (currentAnim != AnimState.Jump && wasInAir)
            {
                float fallDistance = jumpStartY - transform.position.y;
                if (fallDistance > heavyLandThreshold)
                {
                    PlayClip(heavyLandClip, 1f);
                }
            }

            wasInAir = (currentAnim == AnimState.Jump);
        }

        private void PlayClip(AudioClip clip, float volume)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip, volume);
            }
            else
            {
                Debug.LogWarning($"[AUDIO] PlayClip failed: clip={clip}, audioSource={audioSource}");
            }
        }

        private bool IsOnGround()
        {
            var ctrlField = typeof(Player).GetField("playerController",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (ctrlField == null) return true;
            var ctrl = ctrlField.GetValue(Player.Current) as PlayerController;
            return ctrl != null ? ctrl.OnGround : true;
        }

        public void Trail(int face)
        {
            SceneEffectManager.Instance.Add(this.spriteRenderer, face, Color.white);
        }

        public void Scale(Vector2 scale)
        {
            this.scale = scale;
        }

        public void SetSpriteScale(Vector2 scale)
        {
            this.currSpriteScale = scale;
        }

        public void DashFlux()
        {

        }

        public void Slash(bool enable)
        {
        }

        public void WallSlide(Color color, Vector2 dir)
        {
            this.vfxWallSlide.transform.rotation = Quaternion.FromToRotation(Vector2.up, dir);
            var main = this.vfxWallSlide.main;
            main.startColor = color;
            this.vfxWallSlide.Emit(1);
        }

        public void DashFlux(Vector2 dir, bool play)
        {
            if (play)
            {
                this.vfxDashFlux.transform.rotation = Quaternion.FromToRotation(Vector2.up, dir);
                this.vfxDashFlux.Play();
            }
            else
            {
                this.vfxDashFlux.transform.parent = this.transform;
                this.vfxDashFlux.Stop();
            }
        }

        public void SetHairColor(Color color)
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(color, 0.0f), new GradientColorKey(Color.black, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1, 0.0f), new GradientAlphaKey(1, 0.6f), new GradientAlphaKey(0, 1.0f) }
            );
            this.hair.colorGradient = gradient;
            this.hairSprite01.color = color;
            this.hairSprite02.color = color;
        }

        private void OnDrawGizmos()
        {
            Vector2 pos = (Vector2)transform.position;

            // 使用 Inspector 中的碰撞盒参数实时绘制
            Vector2 colliderCenter = pos + normalHitbox.position + normalHitbox.size / 2f;

            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(colliderCenter, new Vector3(normalHitbox.size.x, normalHitbox.size.y, 0));

            // 蹲伏碰撞箱（黄色）
            Vector2 duckCenter = pos + duckHitbox.position + duckHitbox.size / 2f;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(duckCenter, new Vector3(duckHitbox.size.x, duckHitbox.size.y, 0));

            // 角色位置中心点
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(pos, 0.08f);
        }
    }

    //测试用的绘制接口
    public enum EGizmoDrawType
    {
        SlipCheck,
        ClimbCheck,
    }
}
