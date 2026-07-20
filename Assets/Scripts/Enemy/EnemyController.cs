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

        private RectTransform _rectTransform;
        private EnemyRuntime _runtime;
        private EnemyPool _ownerPool;

        public EnemyRuntime Runtime => _runtime;

        public void Bind(EnemyPool ownerPool, EnemyRuntime runtime)
        {
            _ownerPool = ownerPool;
            _runtime = runtime;
            SyncTransform();
            UpdateLabel();
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
            _runtime = null;
            gameObject.SetActive(false);
            _ownerPool?.Release(this);
        }

        private void UpdateLabel()
        {
            if (nameLabel == null || _runtime?.Data == null)
            {
                return;
            }

            nameLabel.text = _runtime.Data.DisplayName;
        }

        private void Awake()
        {
            if (_rectTransform == null)
            {
                _rectTransform = GetComponent<RectTransform>();
            }

            if (bodyImage == null)
            {
                bodyImage = GetComponent<Image>();
            }
        }
    }
}
