using Myd.Common;
using System;
using System.Collections;
using UnityEngine;

namespace Myd.Platform
{
    public class TrailSnapshot : MonoBehaviour
    {
        public Vector2 SpriteScale;
        public int Index;
        public Color Color;
        public float Percent;
        public float Duration;
        public bool Drawn;
        public bool UseRawDeltaTime;

        private SpriteRenderer spriteRenderer;

        void Awake()
        {
            this.spriteRenderer = this.GetComponent<SpriteRenderer>();
        }

        private Action onRemoved;
        public void Init(int index, Vector2 position, Sprite sprite, Vector2 scale, Color color, 
            float duration, int depth, bool frozenUpdate, bool useRawDeltaTime, Action onRemoved)
        {
            this.Index = index;
            this.SpriteScale = scale;
            this.Color = color;
            this.Percent = 0.0f;
            this.Duration = duration;
            this.spriteRenderer.sortingOrder = depth;
            this.Drawn = false;
            this.UseRawDeltaTime = useRawDeltaTime;
            this.spriteRenderer.color = color;
            this.spriteRenderer.sprite = sprite;
            this.transform.position = position;
            // Z 缩放强制为 1：2D 角色 Z=0，残影继承会压扁成白色方块
            this.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            this.onRemoved = onRemoved;
        }

        private void Update()
        {
            OnUpdate();
            OnRender();
        }

        private void OnUpdate()
        {
            if ((double)this.Duration <= 0.0)
            {
                if (!this.Drawn)
                    return;
                Removed();
            }
            else
            {
                if ((double)this.Percent >= 1.0)
                {
                    Removed();
                }
                this.Percent += Time.deltaTime / this.Duration;
            }
        }

        private void OnRender()
        {
            float num = (double)this.Duration > 0.0 ? (float)(0.75 * (1.0 - (double)Ease.CubeOut(this.Percent))) : 1f;
            this.spriteRenderer.color = this.Color * num;
        }

        private void Removed()
        {
            onRemoved?.Invoke();
            Destroy(this.gameObject);
        }
    }
}