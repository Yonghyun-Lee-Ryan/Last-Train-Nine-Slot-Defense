using System.Collections.Generic;
using LastTrain.Data;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LastTrain.UI
{
    /// <summary>전투 VFX Object Pool.</summary>
    public sealed class UiVfxPool : MonoBehaviour
    {
        private const string DefaultDatabasePath = "Assets/Data/Visual/VisualDatabase.asset";

        [SerializeField] private VisualDatabase visualDatabase;
        [SerializeField] private RectTransform poolRoot;
        [SerializeField] private UiVfxController prefab;
        [SerializeField] private int prewarmCount = 16;

        private readonly Queue<UiVfxController> _available = new();
        private readonly HashSet<UiVfxController> _inUse = new();

        public VisualDatabase Database => visualDatabase;

        public void Initialize()
        {
            EnsureDatabase();
            EnsurePoolRoot();
            EnsurePrefab();
            Prewarm();
        }

        public void Play(string vfxId, Vector2 worldPosition)
        {
            if (visualDatabase == null || !visualDatabase.TryGetVfx(vfxId, out VfxVisualSet visual))
            {
                return;
            }

            UiVfxController instance = Get();
            instance.Play(visual, worldPosition);
        }

        internal void Release(UiVfxController controller)
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

        private UiVfxController Get()
        {
            UiVfxController controller = _available.Count > 0
                ? _available.Dequeue()
                : CreateInstance();

            _inUse.Add(controller);
            return controller;
        }

        private void Prewarm()
        {
            while (_available.Count + _inUse.Count < prewarmCount)
            {
                UiVfxController created = CreateInstance();
                created.gameObject.SetActive(false);
                _available.Enqueue(created);
            }
        }

        private UiVfxController CreateInstance()
        {
            UiVfxController instance = Instantiate(prefab, poolRoot);
            instance.Configure(this);
            return instance;
        }

        private void EnsureDatabase()
        {
            if (visualDatabase != null)
            {
                return;
            }

            visualDatabase = VisualDatabaseLocator.Load();
#if UNITY_EDITOR
            if (visualDatabase == null)
            {
                visualDatabase = AssetDatabase.LoadAssetAtPath<VisualDatabase>(DefaultDatabasePath);
            }
#endif
        }

        private void EnsurePoolRoot()
        {
            if (poolRoot == null)
            {
                poolRoot = transform as RectTransform;
            }
        }

        private void EnsurePrefab()
        {
            if (prefab != null)
            {
                return;
            }

            var go = new GameObject("UiVfxTemplate", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UiVfxController));
            go.transform.SetParent(poolRoot, false);
            go.SetActive(false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            prefab = go.GetComponent<UiVfxController>();
            prefab.Configure(this);
        }
    }
}
