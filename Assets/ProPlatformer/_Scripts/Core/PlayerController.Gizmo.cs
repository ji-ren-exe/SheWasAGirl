

using UnityEngine;

namespace Myd.Platform
{
    public partial class PlayerController
    {
        public void Draw(EGizmoDrawType type)
        {
            //始终绘制碰撞箱
            DrawCollider();

            switch (type)
            {
                case EGizmoDrawType.SlipCheck:
                    DrawSlipCheck();
                    break;
                case EGizmoDrawType.ClimbCheck:
                    DrawClimbCheck();
                    break;
            }
        }

        private void DrawCollider()
        {
            //绘制当前碰撞箱（绿色线框）
            Vector2 pos = this.Position + collider.position;
            Vector2 size = collider.size;
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(pos + size / 2f, size);

            //绘制脚下检测点（蓝色小球）
            Vector2 groundPos = this.Position + collider.position + Vector2.down * 0.02f;
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(groundPos, 0.05f);
        }

        private void DrawSlipCheck()
        {
            int direct = Facing == Facings.Right ? 1 : -1;
            {
                Gizmos.color = Color.blue;
                Vector2 origin = this.Position + collider.position + Vector2.up * collider.size.y / 2f + Vector2.right * direct * (collider.size.x / 2f + STEP);
                Vector2 point1 = origin + Vector2.up * (-0.4f + 0.1f);
                Gizmos.DrawWireSphere(point1, 0.1f);

                Gizmos.color = Color.red;
                Vector2 point2 = origin + Vector2.up * (0.4f + 0.1f);
                Gizmos.DrawWireSphere(point2, 0.1f);
            }
        }

        private void DrawClimbCheck()
        {
            //Gizmos.color = Color.blue;
            //Vector2 origin = this.Position + 
            //Vector2 point1 = origin + Vector2.up * (-0.4f + 0.1f);
            //Gizmos.DrawWireSphere(point1, 0.1f);
        }
    }


}
