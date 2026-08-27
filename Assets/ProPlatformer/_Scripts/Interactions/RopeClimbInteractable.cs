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

            if (!player.IsAttachedToRope && GameInput.Grab.Checked() &&
                Vector2.Distance(player.Position, transform.position) <= grabDistance)
            {
                player.AttachToRope(transform.position, climbSpeed);
            }
        }
    }
}
