using System.Collections.Generic;
using LastTrain.Enemy;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LastTrain.Battle
{
    /// <summary>발사체 Object Pool. IProjectileLauncher를 구현한다.</summary>
    public sealed class ProjectilePool : MonoBehaviour, IProjectileLauncher
    {
        private const string DefaultPrefabPath = "Assets/Prefabs/Projectiles/BasicProjectile.prefab";
        private const string ResourcesPrefabPath = "Combat/BasicProjectile";

        [SerializeField] private ProjectileController prefab;
        [SerializeField] private RectTransform poolRoot;
        [SerializeField] private int prewarmCount = 12;
        [SerializeField] private float moveSpeed = BattleConstants.ProjectileSpeed;
        [SerializeField] private float hitRadius = 24f;

        private readonly Queue<ProjectileController> _available = new();
        private readonly HashSet<ProjectileController> _inUse = new();
        private RectTransform _combatSpace;

        public void Initialize(RectTransform combatSpace = null)
        {
            _combatSpace = combatSpace;
            EnsurePrefab();
            EnsurePoolRoot();
            if (prefab == null)
            {
                throw new System.InvalidOperationException(
                    "[ProjectilePool] prefab이 없어 전투를 시작할 수 없습니다. Game 씬 ProjectilePool에 BasicProjectile을 연결하세요.");
            }

            moveSpeed = BattleConstants.ProjectileSpeed;
            hitRadius = 24f;
            Prewarm();
        }

        public void Launch(Vector2 origin, EnemyRuntime target, float damage, string passengerId = null)
        {
            if (prefab == null || target == null || !target.IsAlive)
            {
                return;
            }

            ProjectileController projectile = Get();
            projectile.Configure(this, moveSpeed, hitRadius, _combatSpace);
            projectile.Launch(origin, target, damage, passengerId);
        }

        internal void Release(ProjectileController projectile)
        {
            if (projectile == null)
            {
                return;
            }

            if (!_inUse.Remove(projectile))
            {
                return;
            }

            _available.Enqueue(projectile);
        }

        private ProjectileController Get()
        {
            ProjectileController projectile;
            if (_available.Count > 0)
            {
                projectile = _available.Dequeue();
            }
            else
            {
                projectile = CreateInstance();
            }

            _inUse.Add(projectile);
            return projectile;
        }

        private void Prewarm()
        {
            while (_available.Count + _inUse.Count < prewarmCount)
            {
                ProjectileController created = CreateInstance();
                created.gameObject.SetActive(false);
                _available.Enqueue(created);
            }
        }

        private ProjectileController CreateInstance()
        {
            ProjectileController instance = Instantiate(prefab, poolRoot);
            instance.Configure(this, moveSpeed, hitRadius, _combatSpace);
            return instance;
        }

        private void EnsurePrefab()
        {
            if (prefab != null)
            {
                return;
            }

#if UNITY_EDITOR
            prefab = AssetDatabase.LoadAssetAtPath<ProjectileController>(DefaultPrefabPath);
#endif

            if (prefab == null)
            {
                GameObject loaded = Resources.Load<GameObject>(ResourcesPrefabPath);
                if (loaded != null)
                {
                    prefab = loaded.GetComponent<ProjectileController>();
                }
            }

            if (prefab == null)
            {
                Debug.LogError(
                    $"[ProjectilePool] prefab이 설정되지 않았습니다. Inspector에 연결하거나 Resources/{ResourcesPrefabPath}를 확인하세요.",
                    this);
            }
        }

        private void EnsurePoolRoot()
        {
            if (poolRoot != null)
            {
                return;
            }

            poolRoot = transform as RectTransform;
        }
    }
}
