using LastTrain.Battle;
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
        [SerializeField] private float moveSpeed = 1200f;
        [SerializeField] private float hitRadius = 24f;

        private EnemyRuntime _target;
        private float _damage;
        private ProjectilePool _pool;
        private RectTransform _rectTransform;
        private bool _active;

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

        public void Launch(Vector2 origin, EnemyRuntime target, float damage)
        {
            _rectTransform = _rectTransform != null ? _rectTransform : GetComponent<RectTransform>();
            _target = target;
            _damage = damage;
            _active = true;
            _rectTransform.position = origin;
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
                // 타깃 사망 시 피해 없이 반환
                Release();
                return;
            }

            Vector2 current = _rectTransform.position;
            Vector2 targetPos = _target.Position;
            Vector2 next = Vector2.MoveTowards(current, targetPos, moveSpeed * Time.deltaTime);
            _rectTransform.position = next;

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
            gameObject.SetActive(false);

            if (_pool != null)
            {
                _pool.Release(this);
            }
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
