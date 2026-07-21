using System;
using LastTrain.Data;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.UI
{
    /// <summary>단일 VFX 인스턴스. UiVfxPool이 재사용한다.</summary>
    [RequireComponent(typeof(Image))]
    public sealed class UiVfxController : MonoBehaviour
    {
        [SerializeField] private Image image;
        [SerializeField] private UiSpriteAnimator animator;

        private UiVfxPool _owner;
        private bool _active;

        public bool IsActive => _active;

        public void Configure(UiVfxPool owner)
        {
            _owner = owner;
            if (image == null)
            {
                image = GetComponent<Image>();
            }

            if (animator == null)
            {
                animator = GetComponent<UiSpriteAnimator>();
                if (animator == null)
                {
                    animator = gameObject.AddComponent<UiSpriteAnimator>();
                }

                animator.SetImage(image);
            }
        }

        public void Play(VfxVisualSet visual, Vector2 worldPosition)
        {
            if (visual == null || !visual.Clip.HasFrames)
            {
                return;
            }

            RectTransform rect = transform as RectTransform;
            if (rect != null)
            {
                rect.position = worldPosition;
                rect.sizeDelta = new Vector2(visual.Size, visual.Size);
            }

            image.color = visual.Tint;
            _active = true;
            gameObject.SetActive(true);
            animator.PlayOneShot(visual.Clip, ReleaseToPool);
        }

        public void PlayClip(SpriteAnimationClip clip, Vector2 worldPosition, Color tint, float size)
        {
            if (!clip.HasFrames)
            {
                return;
            }

            RectTransform rect = transform as RectTransform;
            if (rect != null)
            {
                rect.position = worldPosition;
                rect.sizeDelta = new Vector2(size, size);
            }

            image.color = tint;
            _active = true;
            gameObject.SetActive(true);
            animator.PlayOneShot(clip, ReleaseToPool);
        }

        private void ReleaseToPool()
        {
            _active = false;
            gameObject.SetActive(false);
            _owner?.Release(this);
        }

        private void Reset()
        {
            image = GetComponent<Image>();
        }
    }
}
