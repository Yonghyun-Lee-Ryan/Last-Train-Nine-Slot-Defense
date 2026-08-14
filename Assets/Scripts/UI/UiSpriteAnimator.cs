using System;
using LastTrain.Data;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.UI
{
    /// <summary>UGUI Image에서 SpriteAnimationClip을 재생한다.</summary>
    [RequireComponent(typeof(Image))]
    public sealed class UiSpriteAnimator : MonoBehaviour
    {
        [SerializeField] private Image targetImage;
        [SerializeField] private bool playOnEnable = true;

        private SpriteAnimationClip _currentClip;
        private int _frameIndex;
        private float _frameTimer;
        private bool _playing;
        private bool _holdLastFrame;
        private Action _onComplete;

        public bool IsPlaying => _playing;

        private void Awake()
        {
            if (targetImage == null)
            {
                targetImage = GetComponent<Image>();
            }
        }

        private void OnEnable()
        {
            if (playOnEnable && _currentClip.HasFrames)
            {
                Play(_currentClip, holdLastFrame: _currentClip.Loop, _onComplete);
            }
        }

        private void Update()
        {
            Tick(Time.unscaledDeltaTime);
        }

        public void Tick(float deltaTime)
        {
            if (!_playing || !_currentClip.HasFrames || targetImage == null)
            {
                return;
            }

            _frameTimer += deltaTime;
            float frameDuration = 1f / _currentClip.FramesPerSecond;
            if (_frameTimer < frameDuration)
            {
                return;
            }

            _frameTimer -= frameDuration;
            _frameIndex++;

            Sprite[] frames = _currentClip.Frames;
            if (_frameIndex >= frames.Length)
            {
                if (_currentClip.Loop && !_holdLastFrame)
                {
                    _frameIndex = 0;
                }
                else
                {
                    _frameIndex = frames.Length - 1;
                    _playing = false;
                    _onComplete?.Invoke();
                    _onComplete = null;
                }
            }

            targetImage.sprite = frames[_frameIndex];
        }

        public void SetImage(Image image)
        {
            targetImage = image;
        }

        public void PlayIdle(SpriteAnimationClip clip)
        {
            Play(clip, holdLastFrame: false, null);
        }

        public void PlayOneShot(SpriteAnimationClip clip, Action onComplete = null)
        {
            Play(clip, holdLastFrame: true, onComplete);
        }

        public void Play(SpriteAnimationClip clip, bool holdLastFrame, Action onComplete)
        {
            _currentClip = clip;
            _holdLastFrame = holdLastFrame;
            _onComplete = onComplete;
            _frameIndex = 0;
            _frameTimer = 0f;
            _playing = clip.HasFrames;

            if (targetImage != null)
            {
                Sprite first = clip.FirstFrame;
                if (first != null)
                {
                    targetImage.sprite = first;
                    targetImage.enabled = true;
                }
            }
        }

        public void StopOnFirstFrame(Sprite sprite)
        {
            _playing = false;
            _onComplete = null;
            if (targetImage != null && sprite != null)
            {
                targetImage.sprite = sprite;
                targetImage.enabled = true;
            }
        }

        public void SetTint(Color color)
        {
            if (targetImage != null)
            {
                targetImage.color = color;
            }
        }
    }
}
