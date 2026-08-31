using System.Collections.Generic;
using UnityEngine;

namespace Myd.Platform
{
    /// <summary>
    /// 镜像女儿（镜像场景用）：以镜像轴（SceneTransition）为中心与玩家位置左右对称——
    /// 玩家（母亲4）向右走，女儿自动向左走，两人相向而行在轴处相遇。
    /// 纯视觉NPC（无碰撞、无交互），复用女儿帧图，动画节奏/朝向与玩家实时镜像。
    /// </summary>
    public class MirrorDaughter : MonoBehaviour
    {
        [Tooltip("镜像轴（留空=自动取 Level.mirrorSceneCamera 的镜像轴，即场景最右 SceneTransition）")]
        [SerializeField] private Transform mirrorAxis;

        // 与 PlayerRenderer 女儿分支一致的动画参数
        private const float IdleFPS = 8f;
        private const float RunFPS = 12f;
        private const float RunAnimScale = 0.8f;   // 女儿跑步缩放（站立1.0）

        private SpriteRenderer sprite;
        private Sprite[] idleFrames;
        private Sprite[] runFrames;
        private int frameIndex;
        private float frameTimer;
        private bool running;
        private bool initialized;
        private float scenePlayerScale = 1f;

        private void LateUpdate()
        {
            var player = Player.Current;
            if (player == null) return;
            if (!Initialize(player)) return;

            float axisX = ResolveAxisX();

            // 位置镜像：X 以轴对称，Y 跟随玩家（平坦地面脚底同高；玩家下落时同步下落）
            transform.position = new Vector3(2f * axisX - player.Position.x, player.Position.y, 0f);

            // 动画状态与玩家一致：有移动输入=跑（镜像场景为平坦地面角色）
            // 与 PlayerRenderer 相同的判定与重置逻辑 → 两边帧相位同步
            bool moving = Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f;
            if (moving != running)
            {
                running = moving;
                frameIndex = 0;
                frameTimer = 0f;
            }

            var frames = running ? runFrames : idleFrames;
            if (frames == null || frames.Length == 0) frames = idleFrames;
            if (frames != null && frames.Length > 0)
            {
                // unscaledDeltaTime：与 PlayerRenderer 的动画计时一致（Game.Update 传 unscaled），冻帧时不失同步
                frameTimer += Time.unscaledDeltaTime;
                float interval = 1f / (running ? RunFPS : IdleFPS);
                while (frameTimer >= interval)
                {
                    frameTimer -= interval;
                    frameIndex = (frameIndex + 1) % frames.Length;
                }
                sprite.sprite = frames[frameIndex];
            }

            // 朝向镜像：女儿GIF原生朝左；玩家面朝右→女儿朝左（原生正scale），玩家朝左→女儿朝右（翻负）
            float facingSign = (int)player.Facing > 0 ? 1f : -1f;
            float animScale = running ? RunAnimScale : 1f;
            sprite.transform.localScale = new Vector3(
                facingSign * animScale * scenePlayerScale,
                animScale * scenePlayerScale,
                1f);
        }

        /// <summary>
        /// 等待玩家渲染器就绪后完成初始化（复用其精灵子物体基准局部位置/排序/材质，保证女儿与玩家同基准贴地）
        /// </summary>
        private bool Initialize(Player player)
        {
            if (initialized) return true;
            var pr = FindObjectOfType<PlayerRenderer>();
            if (pr == null || pr.spriteRenderer == null) return false;

            idleFrames = LoadFrames("Assets/ProPlatformer/_Arts/Textures/Player/Daughter/DaughterIdle.png", pr);
            runFrames = LoadFrames("Assets/ProPlatformer/_Arts/Textures/Player/Daughter/DaughterRun.png", pr);
            if (idleFrames == null || idleFrames.Length == 0)
            {
                Debug.LogWarning("[MirrorDaughter] 女儿帧图加载失败，NPC 隐藏");
                gameObject.SetActive(false);
                return false;
            }

            var go = new GameObject("Sprite");
            go.transform.SetParent(transform, false);
            sprite = go.AddComponent<SpriteRenderer>();

            // 精灵子物体基准局部位置（女儿底部pivot → 脚底对齐逻辑与玩家完全一致）
            var basePosField = typeof(PlayerRenderer).GetField("spriteRendererBaseLocalPos",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Vector3 basePos = basePosField != null
                ? (Vector3)basePosField.GetValue(pr)
                : pr.spriteRenderer.transform.localPosition;
            go.transform.localPosition = basePos;

            // 排序与材质与玩家精灵一致（同层同序，前后遮挡关系正确）
            sprite.sortingLayerID = pr.spriteRenderer.sortingLayerID;
            sprite.sortingOrder = pr.spriteRenderer.sortingOrder;
            sprite.sharedMaterial = pr.spriteRenderer.sharedMaterial;
            sprite.sprite = idleFrames[0];

            var level = FindObjectOfType<Level>();
            if (level != null && level.playerScale > 0f) scenePlayerScale = level.playerScale;

            initialized = true;
            return true;
        }

        private float ResolveAxisX()
        {
            if (mirrorAxis != null) return mirrorAxis.position.x;
            var level = FindObjectOfType<Level>();
            return level != null ? level.GetMirrorAxisX() : transform.position.x;
        }

        /// <summary>
        /// 加载切片帧（编辑器：AssetDatabase；打包：从 PlayerRenderer 序列化数组复制）
        /// </summary>
        private static Sprite[] LoadFrames(string path, PlayerRenderer source)
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
            return result;
#else
            // 打包：PlayerRenderer 的帧数组已由 Prefab 序列化提供，按字段名复制女儿帧集
            var f = typeof(PlayerRenderer).GetField(
                path.Contains("Run") ? "runFrames" : "idleFrames",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return f != null ? f.GetValue(source) as Sprite[] : null;
#endif
        }
    }
}
