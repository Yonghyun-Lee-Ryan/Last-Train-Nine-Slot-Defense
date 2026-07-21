using LastTrain.Data;
using LastTrain.UI;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.Enemy
{
    /// <summary>
    /// 적 View. EnemyRuntime 위치를 표시하며 전투 로직은 포함하지 않는다.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class EnemyController : MonoBehaviour
    {
        [SerializeField] private Image bodyImage;
        [SerializeField] private Text nameLabel;
        [SerializeField] private UiSpriteAnimator bodyAnimator;

        private RectTransform _rectTransform;
        private EnemyRuntime _runtime;
        private EnemyPool _ownerPool;
        private VisualDatabase _visualDatabase;
        private EnemyVisualSet _visualSet;
        private bool _isDying;

        public EnemyRuntime Runtime => _runtime;

        public void Bind(EnemyPool ownerPool, EnemyRuntime runtime)
        {
            _ownerPool = ownerPool;
            _runtime = runtime;
            _isDying = false;
            SyncTransform();
            UpdateLabel();
            ApplyVisuals();
            SubscribeRuntimeEvents();
            gameObject.SetActive(true);
        }

        public void SyncTransform()
        {
            if (_runtime == null)
            {
                return;
            }

            if (_rectTransform == null)
            {
                _rectTransform = GetComponent<RectTransform>();
            }

            _rectTransform.position = _runtime.Position;
        }

        public void Release()
        {
            UnsubscribeRuntimeEvents();
            _runtime = null;
            _visualSet = null;
            _isDying = false;
            gameObject.SetActive(false);
            _ownerPool?.Release(this);
        }

        private void ApplyVisuals()
        {
            EnsureComponents();
            EnsureVisualDatabase();

            _visualSet = null;
            if (_runtime?.Data != null && _visualDatabase != null)
            {
                _visualDatabase.TryGetEnemyVisual(_runtime.Data.Id, out _visualSet);
            }

            if (_visualSet != null)
            {
                Vector2 size = _visualSet.DisplaySize;
                _rectTransform.sizeDelta = size;

                Sprite sprite = _visualSet.GetMoveOrFallback();
                if (sprite != null)
                {
                    bodyImage.sprite = sprite;
                    bodyImage.color = Color.white;
                }

                if (_visualSet.Move.HasFrames && bodyAnimator != null)
                {
                    bodyAnimator.PlayIdle(_visualSet.Move);
                }

                return;
            }

            bodyImage.sprite = null;
            bodyImage.color = new Color(0.9f, 0.2f, 0.2f, 1f);
        }

        private void SubscribeRuntimeEvents()
        {
            if (_runtime == null)
            {
                return;
            }

            _runtime.HealthChanged -= HandleHealthChanged;
            _runtime.HealthChanged += HandleHealthChanged;
            _runtime.Died -= HandleDied;
            _runtime.Died += HandleDied;
        }

        private void UnsubscribeRuntimeEvents()
        {
            if (_runtime == null)
            {
                return;
            }

            _runtime.HealthChanged -= HandleHealthChanged;
            _runtime.Died -= HandleDied;
        }

        private void HandleHealthChanged(EnemyRuntime _, float current, float max)
        {
            if (_isDying || _visualSet == null || !_visualSet.Hit.HasFrames || bodyAnimator == null)
            {
                return;
            }

            if (current < max)
            {
                bodyAnimator.PlayOneShot(_visualSet.Hit, ResumeMove);
            }
        }

        private void HandleDied(EnemyRuntime _)
        {
            if (_isDying)
            {
                return;
            }

            _isDying = true;
            if (_visualSet != null && _visualSet.Death.HasFrames && bodyAnimator != null)
            {
                bodyAnimator.PlayOneShot(_visualSet.Death, Release);
                return;
            }

            Release();
        }

        private void ResumeMove()
        {
            if (_visualSet != null && _visualSet.Move.HasFrames && bodyAnimator != null)
            {
                bodyAnimator.PlayIdle(_visualSet.Move);
            }
        }

        private void UpdateLabel()
        {
            if (nameLabel == null || _runtime?.Data == null)
            {
                return;
            }

            nameLabel.text = _runtime.Data.DisplayName;
        }

        private void EnsureComponents()
        {
            if (_rectTransform == null)
            {
                _rectTransform = GetComponent<RectTransform>();
            }

            if (bodyImage == null)
            {
                bodyImage = GetComponent<Image>();
            }

            if (bodyAnimator == null && bodyImage != null)
            {
                bodyAnimator = bodyImage.GetComponent<UiSpriteAnimator>();
                if (bodyAnimator == null)
                {
                    bodyAnimator = bodyImage.gameObject.AddComponent<UiSpriteAnimator>();
                }

                bodyAnimator.SetImage(bodyImage);
            }
        }

        private void EnsureVisualDatabase()
        {
            if (_visualDatabase == null)
            {
                _visualDatabase = VisualDatabaseLocator.Load();
            }
        }

        private void Awake()
        {
            EnsureComponents();
        }
    }
}
