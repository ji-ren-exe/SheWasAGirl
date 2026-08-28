using UnityEngine;

namespace Myd.Platform
{
    /// <summary>
    /// Tiled 平台碰撞箱自动同步：用 Rect 工具拉伸 SpriteRenderer.size 时，
    /// 自动按 bottomInset（底部泥土垂边碰撞死区）重算 BoxCollider2D，编辑器与运行时均生效。
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class PlatformColliderSync : MonoBehaviour
    {
        [Tooltip("底部内缩（世界单位）：碰撞箱从精灵底部上收的距离，用于排除泥土扇贝垂边")]
        public float bottomInset = 0.64f;

        private SpriteRenderer spriteRenderer;
        private BoxCollider2D boxCollider;
        private Vector2 syncedSize;

        private void OnEnable()
        {
            this.spriteRenderer = GetComponent<SpriteRenderer>();
            this.boxCollider = GetComponent<BoxCollider2D>();
            SyncCollider();
        }

        private void OnValidate()
        {
            SyncCollider();
        }

#if UNITY_EDITOR
        private void Update()
        {
            // 编辑模式下 Rect 工具拖动会触发逐帧 Update，捕捉 size 变化
            if (this.spriteRenderer != null && this.spriteRenderer.size != this.syncedSize)
                SyncCollider();
        }
#endif

        public void SyncCollider()
        {
            if (this.spriteRenderer == null || this.boxCollider == null) return;
            Vector2 s = this.spriteRenderer.size;
            float height = Mathf.Max(0.05f, s.y - this.bottomInset);
            this.boxCollider.size = new Vector2(s.x, height);
            this.boxCollider.offset = new Vector2(0f, this.bottomInset * 0.5f);
            this.syncedSize = s;
        }
    }
}
