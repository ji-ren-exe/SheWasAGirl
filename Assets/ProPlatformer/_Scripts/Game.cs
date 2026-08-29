
using DG.Tweening;
using Myd.Common;
using Myd.Platform.Core;
using System.Collections;
using UnityEngine;

namespace Myd.Platform
{
    enum EGameState
    {
        Load,   //加载中
        Play,   //游戏中
        Pause,  //游戏暂停
        Fail,   //游戏失败
    }
    public class Game : MonoBehaviour, IGameContext
    {
        public static Game Instance;

        [SerializeField]
        public Level level;
        //场景特效管理器
        [SerializeField]
        private SceneEffectManager sceneEffectManager;
        [SerializeField]
        private SceneCamera gameCamera;
        //玩家
        Player player;

        EGameState gameState;

        void Awake()
        {
            Instance = this;

            gameState = EGameState.Load;

            player = new Player(this);
        }

        IEnumerator Start()
        {
            yield return null;

            //加载玩家（Bounds 用实时计算版，随关卡摆动自动适配）
            player.Reload(level.RuntimeBounds, level.StartPosition);
            this.gameState = EGameState.Play;
            yield return null;
        }

        public void Update()
        {
            float deltaTime = Time.unscaledDeltaTime;
            if (UpdateTime(deltaTime))
            {
                if (this.gameState == EGameState.Play)
                {
                    GameInput.Update(deltaTime);
                    //更新玩家逻辑数据
                    player.Update(deltaTime);
                    //更新摄像机
                    gameCamera.SetCameraPosition(player.GetCameraPosition());
                }
            }
        }

        #region 冻帧
        private float freezeTime;

        /// <summary>
        /// UI 弹窗占用中（密码窗/小游戏等）：冻结角色更新与时间流动，直到弹窗关闭
        /// 注意 UpdateTime 会把 timeScale 拉回 1，单纯设 timeScale=0 无效，必须用此标志
        /// </summary>
        public static bool UIBusy { get; set; }

        /// <summary>
        /// 剧情演出中（密码正确后的对话序列）：跳过角色更新，但时间正常流动——
        /// 对话气泡/协程用 scaled time，若用 UIBusy 冻结时间会导致气泡永不结束、剧情卡死
        /// </summary>
        public static bool SceneBusy { get; set; }

        //更新顿帧数据，如果不顿帧，返回true
        public bool UpdateTime(float deltaTime)
        {
            if (UIBusy)
            {
                Time.timeScale = 0;
                return false;
            }
            if (SceneBusy)
            {
                return false;   // 角色不更新，时间保持流动（对话可播）
            }
            if (freezeTime > 0f)
            {
                freezeTime = Mathf.Max(freezeTime - deltaTime, 0f);
                return false;
            }
            if (Time.timeScale == 0)
            {
                Time.timeScale = 1;
            }
            return true;
        }

        //冻帧
        public void Freeze(float freezeTime)
        {
            this.freezeTime = Mathf.Max(this.freezeTime, freezeTime);
            if (this.freezeTime > 0)
            {
                Time.timeScale = 0;
            }
            else
            {
                Time.timeScale = 1;
            }
        }
        #endregion
        public void CameraShake(Vector2 dir, float duration)
        {
            this.gameCamera.Shake(dir, duration);
        }

        public IEffectControl EffectControl { get=>this.sceneEffectManager; }

        public ISoundControl SoundControl { get; }
        
    }

}
