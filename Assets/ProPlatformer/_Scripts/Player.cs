

using Myd.Common;
using Myd.Platform;
using Myd.Platform.Core;
using UnityEngine;

namespace Myd.Platform
{
    /// <summary>
    /// 玩家类：包含
    /// 1、玩家显示器
    /// 2、玩家控制器（核心控制器）
    /// 并允许两者在内部进行交互
    /// </summary>
    public class Player
    {
        public static Player Current { get; private set; }

        private PlayerRenderer playerRenderer;
        private PlayerController playerController;

        private IGameContext gameContext;

        public Vector2 Position => playerController != null ? playerController.Position : Vector2.zero;

        /// <summary>核心控制器（供外部系统调用，如受击僵直）</summary>
        public PlayerController Controller => playerController;
        public bool IsAttachedToRope => playerController != null && playerController.IsAttachedToRope;
        public bool HasStamina => playerController == null || playerController.HasStamina;
        public float StaminaPercent => playerController != null ? playerController.StaminaPercent : 1f;
        public Facings Facing => playerController != null ? playerController.Facing : Facings.Right;

        public Player(IGameContext gameContext)
        {
            this.gameContext = gameContext;
            Current = this;
        }

        public void AttachToRope(Vector2 ropePosition, float climbSpeed)
        {
            playerController?.AttachToRope(ropePosition, climbSpeed);
        }

        //加载玩家实体
        public void Reload(Bounds bounds, Vector2 startPosition)
        {
            // 场景固定角色（Level.playerCharacter）：0=女儿加载女儿属性，1~4=母亲系加载母亲属性
            var level = UnityEngine.Object.FindObjectOfType<Level>();
            int sceneChar = level != null ? Mathf.Clamp(level.playerCharacter, 0, 4) : 0;

            // 加载角色属性并应用到 Constants
            var stats = Resources.Load<CharacterStats>(sceneChar >= 1 ? "MotherStats" : "DaughterStats");
            if (stats != null)
            {
                stats.ApplyToConstants();
            }

            this.playerRenderer = UnityEngine.Object.Instantiate(Resources.Load<PlayerRenderer>("PlayerRenderer"));
            this.playerRenderer.Reload();
            // 场景指定非女儿时切换到对应母亲（帧集在 Reload 中已加载）
            if (sceneChar >= 1)
                this.playerRenderer.SwitchCharacter(sceneChar);
            //初始化
            this.playerController = new PlayerController(playerRenderer, gameContext.EffectControl);
            this.playerController.Init(bounds, startPosition);

            PlayerParams playerParams = Resources.Load<PlayerParams>("PlayerParam");
            playerParams.SetReloadCallback(() => this.playerController.RefreshAbility());
            playerParams.ReloadParams();
        }

        public void Update(float deltaTime)
        {
            playerController.Update(deltaTime);
            Render();
        }

        private void Render()
        {
            playerRenderer.Render(Time.deltaTime);

            Vector2 scale = playerRenderer.transform.localScale;
            //GIF角色原始朝向为左，因此朝右时翻转X
            scale.x = -Mathf.Abs(scale.x) * (int)playerController.Facing;
            playerRenderer.transform.localScale = scale;
            playerRenderer.transform.position = playerController.Position;

            //if (!lastFrameOnGround && this.playerController.OnGround)
            //{
            //    this.playerRenderer.PlayMoveEffect(true, this.playerController.GroundColor);
            //}
            //else if (lastFrameOnGround && !this.playerController.OnGround)
            //{
            //    this.playerRenderer.PlayMoveEffect(false, this.playerController.GroundColor);
            //}
            //this.playerRenderer.UpdateMoveEffect();

            this.lastFrameOnGround = this.playerController.OnGround;
        }

        private bool lastFrameOnGround;

        public Vector2 GetCameraPosition()
        {
            if (this.playerController == null)
            {
                return Vector3.zero;
            }
            return playerController.GetCameraPosition();
        }
    }

}
