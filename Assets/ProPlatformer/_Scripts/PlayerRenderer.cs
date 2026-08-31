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

        // --- 母亲帧动画 ---
        [Header("母亲动画")]
        [SerializeField] private Sprite[] motherIdleFrames;
        [SerializeField] private Sprite[] motherRunFrames;
        [SerializeField] private Sprite[] motherJumpFrames;
        // --- 母亲2/3/4帧动画（场景固定角色用） ---
        [Header("母亲2动画")]
        [SerializeField] private Sprite[] mother2IdleFrames;
        [SerializeField] private Sprite[] mother2RunFrames;
        [SerializeField] private Sprite[] mother2JumpFrames;
        [Header("母亲3动画")]
        [SerializeField] private Sprite[] mother3IdleFrames;
        [SerializeField] private Sprite[] mother3RunFrames;
        [SerializeField] private Sprite[] mother3JumpFrames;
        [Header("母亲4动画")]
        [SerializeField] private Sprite[] mother4IdleFrames;
        [SerializeField] private Sprite[] mother4RunFrames;
        [SerializeField] private Sprite[] mother4JumpFrames;
        // 母亲精灵缩放：母亲图 182px / 女儿图 70px ≈ 2.6，取倒数使母亲视觉大小接近女儿
        [SerializeField] private float motherSpriteScale = 0.38f;
        // 母亲精灵 Y 偏移：在 Sprite 子物体原始 localPosition 基础上叠加
        [SerializeField] private float motherSpriteYOffset = 0f;
        // 当前激活角色：0=女儿, 1=母亲1, 2=母亲2, 3=母亲3, 4=母亲4
        public int ActiveCharacter { get; private set; } = 0;

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
        [SerializeField] private float footstepInterval = 0.3f;
        private AudioSource audioSource;
        // 一次性音效专用源：与脚步声循环源分离，避免被 Pause 阻塞/延迟补播
        private AudioSource oneShotAudioSource;
        private float footstepTimer;

        private void Awake()
        {
            audioSource = gameObject.GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;

            oneShotAudioSource = gameObject.AddComponent<AudioSource>();
            oneShotAudioSource.playOnAwake = false;
            oneShotAudioSource.spatialBlend = 0f;
        }

        public Vector3 SpritePosition { get => this.spriteRenderer.transform.position; }

        public void Reload()
        {
            LoadFrames();
            LoadMotherFrames();
            LoadMotherVariantFrames();
            // 缓存 Sprite 子物体初始 localPosition（切换角色时在此基础上叠加偏移）
            spriteRendererBaseLocalPos = spriteRenderer.transform.localPosition;
            DisableOriginalHair();
            AttachStaminaRing();
            EnsureGlobalUI();
            SyncSceneCameraFromMain();
        }

        /// <summary>
        /// 场景相机设置与 Main 场景对齐：抖动强度、cullingMask 等角色相关相机参数统一默认值
        /// 新建任何场景都自动使用 Main 调好的手感，无需手动配置
        /// </summary>
        private void SyncSceneCameraFromMain()
        {
            var cam = FindObjectOfType<SceneCamera>();
            if (cam == null) return;

            var shakeField = typeof(SceneCamera).GetField("ShakeStrength",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (shakeField != null)
            {
                float mainShake = 0.125f; // Main 场景调好的值
                shakeField.SetValue(cam, mainShake);
            }

            // 主相机 cullingMask 对齐 Main（119）：剔除 Post 层(26)
            // 否则 Ripple 波纹 quad 会被主相机渲染成白色方块（冲刺结束瞬间）
            var mainCam = cam.GetComponent<Camera>();
            if (mainCam != null && mainCam.cullingMask == -1)
            {
                mainCam.cullingMask = 119;
            }

            // 固定镜头场景：按锁定目标包围盒自适应正交尺寸，恰好容纳整个背景
            // 取「高度需求」与「宽度需求/宽高比」的较大者，窄屏自动放大保证左右不出界
            var level = FindObjectOfType<Level>();
            if (mainCam != null && level != null && level.lockCamera && level.lockTarget != null)
            {
                var lockRenderer = level.lockTarget.GetComponentInChildren<SpriteRenderer>();
                if (lockRenderer != null)
                {
                    Bounds b = lockRenderer.bounds;
                    if (b.extents.x > 0.01f && b.extents.y > 0.01f)
                    {
                        float aspect = Mathf.Max(mainCam.aspect, 0.01f);
                        mainCam.orthographicSize = Mathf.Max(b.extents.y, b.extents.x / aspect);
                    }
                }
            }
        }

        /// <summary>
        /// 耐力环绑定到角色：由 PlayerRenderer 创建并持有（销毁时一并清理），
        /// 但使用独立 Transform——挂在角色子物体下会被父级的翻转缩放(X=-1)和Z=0压扁导致环不可见
        /// </summary>
        private void AttachStaminaRing()
        {
            if (staminaRing != null) return;

            var ringGo = new GameObject("StaminaRing");
            staminaRing = ringGo.AddComponent<StaminaRingUI>();
        }

        // 耐力环引用：生命周期跟随角色（角色销毁时一同销毁）
        private StaminaRingUI staminaRing;

        private void OnDestroy()
        {
            if (staminaRing != null)
                Destroy(staminaRing.gameObject);
        }

        /// <summary>
        /// 自动创建场景级全局UI：对话播放器（DialogueManager）、任务面板（QuestUI）
        /// 每个场景无需手动放置，角色加载时自动补齐
        /// </summary>
        private void EnsureGlobalUI()
        {
            // 对话播放器
            if (FindObjectOfType<Dialogue.DialogueManager>() == null)
            {
                var dmGo = new GameObject("DialogueManager");
                dmGo.AddComponent<Dialogue.DialogueManager>();
            }

            // 任务面板
            if (Quest.QuestUI.Instance == null && FindObjectOfType<Quest.QuestUI>() == null)
            {
                var qGo = new GameObject("QuestUI");
                qGo.AddComponent<Quest.QuestUI>();
            }

            // 场景重开（R 键）：常驻单例，一次创建跨场景复用
            SceneReloader.Ensure();
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
#if UNITY_EDITOR
            // 编辑器：运行时动态加载（切片改动立即生效）
            idleFrames = LoadAll("Assets/ProPlatformer/_Arts/Textures/Player/Daughter/DaughterIdle.png");
            runFrames  = LoadAll("Assets/ProPlatformer/_Arts/Textures/Player/Daughter/DaughterRun.png");
            jumpFrames = LoadAll("Assets/ProPlatformer/_Arts/Textures/Player/Daughter/DaughterJump.png");
#else
            // 打包：使用 Prefab 序列化的帧数组（编辑器脚本已填充）
#endif
        }

        private static Sprite[] LoadAll(string path)
        {
#if UNITY_EDITOR
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
#else
            return null;
#endif
        }

        private void LoadMotherFrames()
        {
#if UNITY_EDITOR
            motherIdleFrames = LoadAll("Assets/ProPlatformer/_Arts/Textures/Player/Mother/MotherIdle.png");
            motherRunFrames  = LoadAll("Assets/ProPlatformer/_Arts/Textures/Player/Mother/MotherRun.png");
            motherJumpFrames = LoadAll("Assets/ProPlatformer/_Arts/Textures/Player/Mother/MotherJump.png");
#endif
        }

        /// <summary>
        /// 加载母亲2/3/4帧集（与母亲1同管线：水平条带+8帧切片）
        /// </summary>
        private void LoadMotherVariantFrames()
        {
#if UNITY_EDITOR
            mother2IdleFrames = LoadAll("Assets/ProPlatformer/_Arts/Textures/Player/Mother2/Mother2Idle.png");
            mother2RunFrames  = LoadAll("Assets/ProPlatformer/_Arts/Textures/Player/Mother2/Mother2Run.png");
            mother2JumpFrames = LoadAll("Assets/ProPlatformer/_Arts/Textures/Player/Mother2/Mother2Jump.png");
            mother3IdleFrames = LoadAll("Assets/ProPlatformer/_Arts/Textures/Player/Mother3/Mother3Idle.png");
            mother3RunFrames  = LoadAll("Assets/ProPlatformer/_Arts/Textures/Player/Mother3/Mother3Run.png");
            mother3JumpFrames = LoadAll("Assets/ProPlatformer/_Arts/Textures/Player/Mother3/Mother3Jump.png");
            mother4IdleFrames = LoadAll("Assets/ProPlatformer/_Arts/Textures/Player/Mother4/Mother4Idle.png");
            mother4RunFrames  = LoadAll("Assets/ProPlatformer/_Arts/Textures/Player/Mother4/Mother4Run.png");
            mother4JumpFrames = LoadAll("Assets/ProPlatformer/_Arts/Textures/Player/Mother4/Mother4Jump.png");
#endif
        }

        /// <summary>
        /// 按当前角色取帧数组（0=女儿, 1~4=母亲1~4）
        /// </summary>
        private Sprite[] GetFrames(AnimState state)
        {
            switch (state)
            {
                case AnimState.Run:  return GetFramesByChar(runFrames, motherRunFrames, mother2RunFrames, mother3RunFrames, mother4RunFrames);
                case AnimState.Jump: return GetFramesByChar(jumpFrames, motherJumpFrames, mother2JumpFrames, mother3JumpFrames, mother4JumpFrames);
                default:             return GetFramesByChar(idleFrames, motherIdleFrames, mother2IdleFrames, mother3IdleFrames, mother4IdleFrames);
            }
        }

        private Sprite[] GetFramesByChar(Sprite[] daughter, Sprite[] m1, Sprite[] m2, Sprite[] m3, Sprite[] m4)
        {
            switch (ActiveCharacter)
            {
                case 1: return m1 != null && m1.Length > 0 ? m1 : daughter;
                case 2: return m2 != null && m2.Length > 0 ? m2 : daughter;
                case 3: return m3 != null && m3.Length > 0 ? m3 : daughter;
                case 4: return m4 != null && m4.Length > 0 ? m4 : daughter;
                default: return daughter;
            }
        }

        /// <summary>
        /// 角色是否为母亲系（1~4）：使用母亲渲染分支（中心pivot+等比缩放+Y偏移补偿）
        /// </summary>
        private bool IsMotherActive => ActiveCharacter >= 1;

        /// <summary>
        /// 切换角色（0=女儿, 1=母亲1, 2=母亲2, 3=母亲3, 4=母亲4），重置动画状态
        /// </summary>
        public void SwitchCharacter(int charId)
        {
            ActiveCharacter = Mathf.Clamp(charId, 0, 4);
            CharacterAbilities.CurrentCharacter = ActiveCharacter;
            currentAnim = AnimState.Idle;
            frameIndex = 0;
            frameTimer = 0f;
        }

        public void Render(float deltaTime)
        {
            UpdateAnimation(deltaTime);

            float tempScaleX = Mathf.MoveTowards(scale.x, currSpriteScale.x, 1.75f * deltaTime);
            float tempScaleY = Mathf.MoveTowards(scale.y, currSpriteScale.y, 1.75f * deltaTime);
            this.scale = new Vector2(tempScaleX, tempScaleY);

            float sceneScale = GetScenePlayerScale();

            if (IsMotherActive)
            {
                // 母亲：等比缩放（不拉伸）+ 在原始 localPosition 基础上叠加 Y 偏移
                this.spriteRenderer.transform.localScale = new Vector3(scale.x * motherSpriteScale * sceneScale, scale.y * motherSpriteScale * sceneScale, 1f);
                var lp = spriteRendererBaseLocalPos;
                float yExtra = motherSpriteYOffset;
                // 母亲切片是中心 pivot：整体放大后脚底会下陷，需上移补偿保持贴地
                if (sceneScale != 1f && spriteRenderer.sprite != null)
                {
                    float baseH = spriteRenderer.sprite.bounds.size.y * scale.y * motherSpriteScale;
                    yExtra += (sceneScale - 1f) * baseH * 0.5f;
                }
                this.spriteRenderer.transform.localPosition = new Vector3(lp.x, lp.y + yExtra, lp.z);
            }
            else
            {
                // 女儿：保持原始逻辑（切片 pivot 在底部，缩放后脚底天然对齐）
                this.spriteRenderer.transform.localScale = new Vector3(scale.x * sceneScale, scale.y * sceneScale, 0f);
                this.spriteRenderer.transform.localPosition = spriteRendererBaseLocalPos;
            }
        }

        // 场景级角色缩放（Level.playerScale）：缓存避免每帧 FindObjectsOfType
        private Level sceneLevelCache;
        private float cachedScenePlayerScale = -1f;

        private float GetScenePlayerScale()
        {
            if (cachedScenePlayerScale < 0f)
            {
                sceneLevelCache = FindObjectOfType<Level>();
                cachedScenePlayerScale = sceneLevelCache != null ? sceneLevelCache.playerScale : 1f;
                if (cachedScenePlayerScale <= 0f) cachedScenePlayerScale = 1f;
            }
            return cachedScenePlayerScale;
        }

        private Vector3 spriteRendererBaseLocalPos;

        private void UpdateAnimation(float deltaTime)
        {
            // 根据玩家状态决定动画
            var player = Player.Current;
            if (player == null) return;

            AnimState newAnim;
            // 使用 PlayerController 的状态信息
            bool onGround = IsOnGround();
            bool moving = Mathf.Abs(UnityEngine.Input.GetAxisRaw("Horizontal")) > 0.1f;
            // 冲刺中：使用跑步动画（冲刺状态优先于空中判定）
            bool dashing = IsDashing();

            if (dashing)
                newAnim = AnimState.Run;
            else if (player.IsAttachedToRope)
                newAnim = AnimState.Jump;
            else if (!onGround)
            {
                // 滞空：有横向速度→奔跑图（空中奔跑感）；无横向速度（原地起跳）→跳跃GIF
                if (Mathf.Abs(GetHorizontalSpeed()) > 0.5f)
                    newAnim = AnimState.Run;
                else
                    newAnim = AnimState.Jump;
            }
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
            // 根据当前角色选择帧数组（0=女儿,1~4=母亲1~4）
            frames = GetFrames(currentAnim);
            switch (currentAnim)
            {
                case AnimState.Run:  fps = runFPS;  break;
                case AnimState.Jump: fps = jumpFPS; break;
                default:             fps = idleFPS; break;
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

            // 跑步缩小到0.8（母亲跑步放大到1.2），跳跃放大到1.2，站立为1.0
            float animScale = 1f;
            if (currentAnim == AnimState.Run) animScale = IsMotherActive ? 1.1f : 0.8f;
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

            // 跳跃音效：上一帧在地面、本帧离地且向上运动（真正起跳）才播放，走出平台坠落不播
            // 与动画解耦：滞空动画已改用奔跑图，AnimState.Jump 不再被赋值，不能用动画状态判定
            if (wasOnGroundLastFrame && !onGround)
            {
                if (GetVerticalSpeed() > 0.1f)
                {
                    PlayClip(jumpClip, 0.8f);
                }
            }

            wasOnGroundLastFrame = onGround;
            onGround_lastKnown = onGround;

            // 被禁能力输入反馈：母亲3/4按了不可用指令时播放"身体好沉重"
            CheckBlockedAbilityFeedback();
        }

        // ---- 被禁能力反馈 ----
        private float blockedFeedbackCooldown = -10f;
        private const float BlockedFeedbackInterval = 1.5f;

        /// <summary>
        /// 检测被禁指令输入（在 UpdateAnimation 末尾调用，此时本帧控制器已更新：
        /// 按键缓冲未被消费=这次按键没有产生任何动作）
        /// 母亲3：冲刺键、空中跳跃键（二段跳被禁）
        /// 母亲4：跳跃键（全禁）、冲刺键
        /// </summary>
        private void CheckBlockedAbilityFeedback()
        {
            if (ActiveCharacter < 3) return;
            if (Time.time < blockedFeedbackCooldown) return;
            var dm = Dialogue.DialogueManager.Instance;
            if (dm != null && dm.IsPlaying) return;

            bool jumpRaw = UnityEngine.Input.GetKeyDown(KeyCode.Space) || UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton0);
            bool dashRaw = UnityEngine.Input.GetKeyDown(KeyCode.K) || UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton5);

            bool blocked = false;
            if (dashRaw)
            {
                // 冲刺：母亲3/4均被禁
                blocked = true;
            }
            else if (jumpRaw)
            {
                if (ActiveCharacter == 4)
                {
                    // 跳跃：母亲4全禁
                    blocked = true;
                }
                else if (!onGround_lastKnown && GameInput.Jump.BufferActive)
                {
                    // 母亲3空中按跳：缓冲未被消费（二段跳被禁且无墙跳可触发）
                    blocked = true;
                }
            }

            if (!blocked) return;
            blockedFeedbackCooldown = Time.time + BlockedFeedbackInterval;
            PlayBlockedAbilityDialogue();
        }

        /// <summary>UpdateAnimation 缓存的地面状态（反馈判定用）</summary>
        private bool onGround_lastKnown = true;

        /// <summary>
        /// 动态构造单气泡对话"身体好沉重"并播放（跟随玩家头顶）
        /// </summary>
        private void PlayBlockedAbilityDialogue()
        {
            var dm = Dialogue.DialogueManager.Instance;
            if (dm == null) return;

            var data = ScriptableObject.CreateInstance<Dialogue.DialogueData>();
            data.bubbles.Add(new Dialogue.DialogueBubble
            {
                text = "身体好沉重",
                duration = 1.6f,
                speakerId = 0
            });
            dm.Play(data);
        }

        private void PlayClip(AudioClip clip, float volume)
        {
            if (clip != null && oneShotAudioSource != null)
            {
                oneShotAudioSource.PlayOneShot(clip, volume);
            }
            else
            {
                Debug.LogWarning($"[AUDIO] PlayClip failed: clip={clip}, oneShotAudioSource={oneShotAudioSource}");
            }
        }

        private bool IsOnGround()
        {
            var ctrl = GetController();
            return ctrl != null ? ctrl.OnGround : true;
        }

        /// <summary>
        /// 玩家是否处于冲刺状态（状态机 State == Dash）
        /// </summary>
        private bool IsDashing()
        {
            var ctrl = GetController();
            if (ctrl == null) return false;

            var smField = typeof(PlayerController).GetField("stateMachine",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (smField == null) return false;
            var sm = smField.GetValue(ctrl);
            if (sm == null) return false;

            var stateProp = sm.GetType().GetProperty("State");
            if (stateProp == null) return false;
            return stateProp.GetValue(sm) is EActionState es && es == EActionState.Dash;
        }

        private float GetVerticalSpeed()
        {
            var ctrl = GetController();
            return ctrl != null ? ctrl.Speed.y : 0f;
        }

        private float GetHorizontalSpeed()
        {
            var ctrl = GetController();
            return ctrl != null ? ctrl.Speed.x : 0f;
        }

        // 滞空动画判定：上一帧是否在地面（起跳瞬间检测）
        private bool wasOnGroundLastFrame = true;

        private PlayerController GetController()
        {
            var ctrlField = typeof(Player).GetField("playerController",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (ctrlField == null || Player.Current == null) return null;
            return ctrlField.GetValue(Player.Current) as PlayerController;
        }

        public void Trail(int face)
        {
            // duration 0.45秒：残影保留更久，形成更明显的拖尾带
            SceneEffectManager.Instance.Add(this.spriteRenderer, face, Color.white, 0.45f);
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
