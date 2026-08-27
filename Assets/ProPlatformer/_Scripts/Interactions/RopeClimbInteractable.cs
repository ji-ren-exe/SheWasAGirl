using UnityEngine;

namespace Myd.Platform
{
    public class RopeClimbInteractable : MonoBehaviour
    {
        [SerializeField] private float grabDistance = 3f;
        [SerializeField] private float climbSpeed = 4.5f;

        private void Update()
        {
            var player = Player.Current;
            if (player == null)
                return;

            if (player.IsAttachedToRope || !player.HasStamina)
                return;

            //按住朝向绳子的方向键即可吸附
            int moveX = System.Math.Sign(UnityEngine.Input.GetAxisRaw("Horizontal"));
            if (moveX == 0)
                return;

            // 用 SpriteRenderer bounds 计算最近点（不依赖碰撞箱）
            Vector2 closestPoint = transform.position;
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                var bounds = sr.bounds;
                closestPoint = bounds.ClosestPoint(player.Position);
            }

            Vector2 toRope = closestPoint - player.Position;
            if (Vector2.Distance(player.Position, closestPoint) > grabDistance)
                return;

            // 方向判断：朝向绳子的方向
            if (System.Math.Sign(toRope.x) == moveX || Mathf.Abs(toRope.x) < 0.5f)
            {
                player.AttachToRope(transform.position, climbSpeed);
            }
        }
    }
}
