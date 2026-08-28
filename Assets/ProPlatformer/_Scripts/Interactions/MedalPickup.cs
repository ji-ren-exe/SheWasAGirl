using System.Collections;
using UnityEngine;

namespace Myd.Platform
{
    public class MedalPickup : MonoBehaviour
    {
        [SerializeField] private string medalType = "Medal";
        [SerializeField] private float pickupDistance = 1.1f;
        [Header("拾取音效")]
        [SerializeField] private AudioClip pickupClip;
        [Range(0f, 1f)]
        [SerializeField] private float pickupVolume = 0.8f;
        private bool collected;
        private AudioSource audioSource;

        private void Awake()
        {
            audioSource = gameObject.GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        private void Update()
        {
            if (collected || Player.Current == null)
                return;

            if (Vector2.Distance(Player.Current.Position, transform.position) <= pickupDistance)
            {
                collected = true;
                Debug.Log($"Collected {medalType}");
                // 先隐藏 visuals（Sprite/Collider），延迟到音效播完再失活整个对象
                var sr = GetComponent<SpriteRenderer>();
                if (sr != null) sr.enabled = false;
                var col = GetComponent<Collider2D>();
                if (col != null) col.enabled = false;

                if (pickupClip != null && audioSource != null)
                {
                    audioSource.PlayOneShot(pickupClip, pickupVolume);
                    StartCoroutine(HideAfterSound(pickupClip.length));
                }
                else
                {
                    gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 音效播完后再失活对象（SetActive(false) 会立刻掐断 AudioSource 的声音）
        /// </summary>
        private IEnumerator HideAfterSound(float delay)
        {
            yield return new WaitForSeconds(delay);
            gameObject.SetActive(false);
        }
    }
}
