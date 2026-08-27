using UnityEngine;

namespace Myd.Platform
{
    public class MedalPickup : MonoBehaviour
    {
        [SerializeField] private string medalType = "Medal";
        [SerializeField] private float pickupDistance = 1.1f;
        private bool collected;

        private void Update()
        {
            if (collected || Player.Current == null)
                return;

            if (Vector2.Distance(Player.Current.Position, transform.position) <= pickupDistance)
            {
                collected = true;
                Debug.Log($"Collected {medalType}");
                gameObject.SetActive(false);
            }
        }
    }
}
