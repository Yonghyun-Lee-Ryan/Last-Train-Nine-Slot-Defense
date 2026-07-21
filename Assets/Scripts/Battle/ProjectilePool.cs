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

        [SerializeField] private ProjectileController prefab;
        [SerializeField] private RectTransform poolRoot;
        [SerializeField] private int prewarmCount = 12;
        [SerializeField] private float moveSpeed = BattleConstants.ProjectileSpeed;
        [SerializeField] private float hitRadius = 24f;

        private readonly Queue<ProjectileController> _available = new();
        private readonly HashSet<ProjectileController> _inUse = new();

        public void Initialize()
        {
            EnsurePrefab();
            EnsurePoolRoot();
            Prewarm();
        }

        public void Launch(Vector2 origin, EnemyRuntime target, float damage, string passengerId = null)
        {
            if (target == null || !target.IsAlive)
            {
                return;
            }

            ProjectileController projectile = Get();
            projectile.Launch(origin, target, damage, passengerId);
        }

        internal void Release(ProjectileController projectile)
        {
            if (projectile == null)
            {
                return;
            }

            _inUse.Remove(projectile);
            if (!_available.Contains(projectile))
            {
                _available.Enqueue(projectile);
            }
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
            instance.Configure(this, moveSpeed, hitRadius);
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
                Debug.LogError(
                    $"[ProjectilePool] prefab이 설정되지 않았습니다. Inspector에 연결하거나 {DefaultPrefabPath}를 확인하세요.",
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
