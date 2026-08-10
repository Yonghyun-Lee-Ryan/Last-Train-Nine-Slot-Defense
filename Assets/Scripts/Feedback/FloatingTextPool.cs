using System.Collections.Generic;
using LastTrain.Core;
using LastTrain.Release;
using LastTrain.UI;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.Feedback
{
    /// <summary>FloatingCombatText Object Pool.</summary>
    public sealed class FloatingTextPool : MonoBehaviour
    {
        [SerializeField] private FloatingCombatText prefab;
        [SerializeField] private RectTransform poolRoot;
        [SerializeField] private int prewarmCount = 12;
        [SerializeField] [Range(0.1f, 1f)] private float lowFxPlayChance = 0.4f;

        private readonly Queue<FloatingCombatText> _available = new();
        private readonly HashSet<FloatingCombatText> _inUse = new();

        public void Initialize(FloatingCombatText textPrefab, RectTransform root)
        {
            if (textPrefab != null)
            {
                prefab = textPrefab;
            }

            if (root != null)
            {
                poolRoot = root;
            }

            EnsureRoot();
            EnsurePrefab();
            Prewarm();
        }

        public void Spawn(string message, Color color, Vector2 anchoredPosition)
        {
            Spawn(message, color, anchoredPosition, FloatingTextKind.Damage);
        }

        public void Spawn(string message, Color color, Vector2 anchoredPosition, FloatingTextKind kind)
        {
            if (!CanSpawn(kind))
            {
                return;
            }

            if (prefab == null || poolRoot == null)
            {
                return;
            }

            FloatingCombatText instance = Get();
            instance.Play(message, color, anchoredPosition, this);
        }

        public void SpawnWorld(string message, Color color, Vector2 worldPosition, Camera camera, Canvas canvas)
        {
            SpawnWorld(message, color, worldPosition, camera, canvas, FloatingTextKind.Damage);
        }

        public void SpawnWorld(
            string message,
            Color color,
            Vector2 worldPosition,
            Camera camera,
            Canvas canvas,
            FloatingTextKind kind)
        {
            _ = camera;
            _ = canvas;

            if (!CanSpawn(kind))
            {
                return;
            }

            if (prefab == null || poolRoot == null)
            {
                return;
            }

            FloatingCombatText instance = Get();
            instance.PlayAtWorld(message, color, worldPosition, this);
        }

        private bool CanSpawn(FloatingTextKind kind)
        {
            GameSettingsService settings = AppRoot.Instance?.GameSettings;
            if (settings != null)
            {
                switch (kind)
                {
                    case FloatingTextKind.Damage when !settings.DamageNumbersEnabled:
                    case FloatingTextKind.Coin when !settings.CoinNumbersEnabled:
                        return false;
                }

                if (settings.LowFxMode && Random.value > lowFxPlayChance)
                {
                    return false;
                }
            }

            return true;
        }

        internal void Release(FloatingCombatText text)
        {
            if (text == null)
            {
                return;
            }

            if (!_inUse.Remove(text))
            {
                return;
            }

            text.gameObject.SetActive(false);
            _available.Enqueue(text);
        }

        private FloatingCombatText Get()
        {
            FloatingCombatText instance = _available.Count > 0
                ? _available.Dequeue()
                : CreateInstance();

            _inUse.Add(instance);
            instance.gameObject.SetActive(true);
            return instance;
        }

        private void Prewarm()
        {
            while (_available.Count + _inUse.Count < prewarmCount)
            {
                FloatingCombatText created = CreateInstance();
                created.gameObject.SetActive(false);
                _available.Enqueue(created);
            }
        }

        private FloatingCombatText CreateInstance()
        {
            FloatingCombatText instance = Instantiate(prefab, poolRoot);
            return instance;
        }

        private void EnsureRoot()
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

            var go = new GameObject(
                "FloatingCombatTextTemplate",
                typeof(RectTransform),
                typeof(Text),
                typeof(FloatingCombatText));
            go.transform.SetParent(poolRoot, false);
            go.SetActive(false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(200f, 48f);
            Text label = go.GetComponent<Text>();
            label.alignment = TextAnchor.MiddleCenter;
            label.fontSize = 28;
            label.raycastTarget = false;
            if (label.font == null)
            {
                label.font = LastTrain.UI.GameFontProvider.Get();
            }

            prefab = go.GetComponent<FloatingCombatText>();
        }
    }
}
