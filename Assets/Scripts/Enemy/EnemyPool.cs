using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LastTrain.Enemy
{
    /// <summary>적 View Object Pool.</summary>
    public sealed class EnemyPool : MonoBehaviour
    {
        private const string DefaultPrefabPath = "Assets/Prefabs/Enemies/BasicEnemy.prefab";
        private const string ResourcesPrefabPath = "Combat/BasicEnemy";

        [SerializeField] private EnemyController prefab;
        [SerializeField] private RectTransform poolRoot;
        [SerializeField] private int prewarmCount = 8;

        private readonly Queue<EnemyController> _available = new();
        private readonly HashSet<EnemyController> _inUse = new();
        private RectTransform _combatSpace;

        public void Initialize(RectTransform combatSpace = null)
        {
            _combatSpace = combatSpace;
            EnsurePrefab();
            EnsurePoolRoot();
            if (prefab == null)
            {
                throw new System.InvalidOperationException(
                    "[EnemyPool] prefab이 없어 전투를 시작할 수 없습니다. Game 씬 EnemyPool에 BasicEnemy를 연결하세요.");
            }

            Prewarm();
        }

        public EnemyController Spawn(EnemyRuntime runtime)
        {
            if (runtime == null || prefab == null)
            {
                return null;
            }

            EnemyController controller = Get();
            controller.SetCombatSpace(_combatSpace);
            controller.Bind(this, runtime);
            return controller;
        }

        internal void Release(EnemyController controller)
        {
            if (controller == null)
            {
                return;
            }

            if (!_inUse.Remove(controller))
            {
                return;
            }

            _available.Enqueue(controller);
        }

        public void ReleaseAll(IEnumerable<EnemyController> controllers)
        {
            if (controllers == null)
            {
                return;
            }

            foreach (EnemyController controller in controllers)
            {
                if (controller != null)
                {
                    controller.Release();
                }
            }
        }

        private EnemyController Get()
        {
            EnemyController controller = _available.Count > 0 ? _available.Dequeue() : CreateInstance();
            _inUse.Add(controller);
            return controller;
        }

        private void Prewarm()
        {
            while (_available.Count + _inUse.Count < prewarmCount)
            {
                EnemyController created = CreateInstance();
                created.gameObject.SetActive(false);
                _available.Enqueue(created);
            }
        }

        private EnemyController CreateInstance()
        {
            return Instantiate(prefab, poolRoot);
        }

        private void EnsurePrefab()
        {
            if (prefab != null)
            {
                return;
            }

#if UNITY_EDITOR
            prefab = AssetDatabase.LoadAssetAtPath<EnemyController>(DefaultPrefabPath);
#endif

            if (prefab == null)
            {
                GameObject loaded = Resources.Load<GameObject>(ResourcesPrefabPath);
                if (loaded != null)
                {
                    prefab = loaded.GetComponent<EnemyController>();
                }
            }

            if (prefab == null)
            {
                Debug.LogError(
                    $"[EnemyPool] prefab이 설정되지 않았습니다. Inspector에 연결하거나 Resources/{ResourcesPrefabPath}를 확인하세요.",
                    this);
            }
        }

        private void EnsurePoolRoot()
        {
            if (poolRoot == null)
            {
                poolRoot = transform as RectTransform;
            }
        }
    }
}
