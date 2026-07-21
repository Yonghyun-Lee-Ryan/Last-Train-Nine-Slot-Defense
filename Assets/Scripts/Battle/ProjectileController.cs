using LastTrain.Battle;
using LastTrain.Data;
using LastTrain.Enemy;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.Battle
{
    /// <summary>
    /// 기본 발사체. 타깃을 추적하며 도달 시 피해를 적용한다.
    /// 타깃이 비행 중 사망하면 피해 없이 풀로 반환한다.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class ProjectileController : MonoBehaviour
    {
        [SerializeField] private Image image;
        [SerializeField] private float moveSpeed = BattleConstants.ProjectileSpeed;
        [SerializeField] private float hitRadius = 24f;

        private EnemyRuntime _target;
        private float _damage;
        private ProjectilePool _pool;
        private RectTransform _rectTransform;
        private bool _active;
        private ProjectileVisualSet _visualSet;
        private bool _rotateTowardTarget = true;

        public bool IsActive => _active;

        public void Configure(ProjectilePool pool, float speed, float radius)
        {
            _pool = pool;
            if (speed > 0f)
            {
                moveSpeed = speed;
            }

            if (radius > 0f)
            {
                hitRadius = radius;
            }
        }

        public void Launch(Vector2 origin, EnemyRuntime target, float damage, string passengerId = null)
        {
            _rectTransform = _rectTransform != null ? _rectTransform : GetComponent<RectTransform>();
            _target = target;
            _damage = damage;
            _active = true;
            _rectTransform.position = origin;
            ApplyVisual(passengerId);
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!_active)
            {
                return;
            }

            if (_target == null || !_target.IsAlive)
            {
                Release();
                return;
            }

            Vector2 current = _rectTransform.position;
            Vector2 targetPos = _target.Position;
            Vector2 next = Vector2.MoveTowards(current, targetPos, moveSpeed * Time.deltaTime);
            _rectTransform.position = next;

            if (_rotateTowardTarget)
            {
                Vector2 delta = targetPos - current;
                if (delta.sqrMagnitude > 0.001f)
                {
                    float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
                    _rectTransform.rotation = Quaternion.Euler(0f, 0f, angle);
                }
            }

            if (Vector2.Distance(next, targetPos) <= hitRadius)
            {
                DamageService.ApplyDamage(_target, _damage);
                Release();
            }
        }

        public void Release()
        {
            _active = false;
            _target = null;
            _damage = 0f;
            _visualSet = null;
            gameObject.SetActive(false);

            if (_pool != null)
            {
                _pool.Release(this);
            }
        }

        private void ApplyVisual(string passengerId)
        {
            if (image == null)
            {
                image = GetComponent<Image>();
            }

            VisualDatabase database = VisualDatabaseLocator.Load();
            string projectileId = string.IsNullOrWhiteSpace(passengerId)
                ? "projectile_default"
                : $"projectile_{passengerId.Replace("passenger_", string.Empty)}";

            _visualSet = null;
            if (database != null && database.TryGetProjectileVisual(projectileId, out ProjectileVisualSet visual))
            {
                _visualSet = visual;
            }

            if (_visualSet != null && _visualSet.Sprite != null)
            {
                image.sprite = _visualSet.Sprite;
                image.color = _visualSet.Tint;
                _rectTransform.sizeDelta = new Vector2(_visualSet.Size, _visualSet.Size);
                _rotateTowardTarget = _visualSet.RotateTowardTarget;
                return;
            }

            image.sprite = null;
            image.color = new Color(1f, 0.85f, 0.2f, 1f);
            _rectTransform.sizeDelta = new Vector2(20f, 20f);
            _rotateTowardTarget = true;
        }

        private void Reset()
        {
            image = GetComponent<Image>();
            _rectTransform = GetComponent<RectTransform>();
        }

        private void Awake()
        {
            if (_rectTransform == null)
            {
                _rectTransform = GetComponent<RectTransform>();
            }

            if (image == null)
            {
                image = GetComponent<Image>();
            }
        }
    }
}
