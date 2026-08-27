using UnityEngine;

namespace Myd.Platform
{
    public class RopeClimbInteractable : MonoBehaviour
    {
        [SerializeField] private float grabDistance = 1.25f;
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

            Vector2 toRope = (Vector2)transform.position - player.Position;
            if (Vector2.Distance(player.Position, transform.position) > grabDistance)
                return;

            if (System.Math.Sign(toRope.x) == moveX || Mathf.Abs(toRope.x) < 0.1f)
            {
                player.AttachToRope(transform.position, climbSpeed);
            }
        }
    }
}
