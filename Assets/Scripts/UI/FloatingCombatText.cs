using System.Collections;
using LastTrain.Feedback;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.UI
{
    /// <summary>피해·코인 획득 등 간단한 떠오르는 텍스트.</summary>
    public sealed class FloatingCombatText : MonoBehaviour
    {
        [SerializeField] private Text label;
        [SerializeField] private float lifetime = 0.9f;
        [SerializeField] private float riseDistance = 80f;

        private RectTransform _rect;
        private FloatingTextPool _pool;
        private bool _useWorldSpace;

        public void Play(string message, Color color, Vector2 anchoredStart)
        {
            Play(message, color, anchoredStart, null, useWorldSpace: false);
        }

        public void Play(string message, Color color, Vector2 anchoredStart, FloatingTextPool pool)
        {
            Play(message, color, anchoredStart, pool, useWorldSpace: false);
        }

        public void PlayAtWorld(string message, Color color, Vector2 worldPosition, FloatingTextPool pool)
        {
            Play(message, color, worldPosition, pool, useWorldSpace: true);
        }

        private void Play(
            string message,
            Color color,
            Vector2 start,
            FloatingTextPool pool,
            bool useWorldSpace)
        {
            _pool = pool;
            _useWorldSpace = useWorldSpace;
            if (label == null)
            {
                label = GetComponentInChildren<Text>();
            }

            _rect = transform as RectTransform;
            if (_rect != null)
            {
                _rect.anchorMin = new Vector2(0.5f, 0.5f);
                _rect.anchorMax = new Vector2(0.5f, 0.5f);
                _rect.pivot = new Vector2(0.5f, 0.5f);
                if (_useWorldSpace)
                {
                    _rect.position = new Vector3(start.x, start.y, _rect.position.z);
                }
                else
                {
                    _rect.anchoredPosition = start;
                }
            }

            if (label != null)
            {
                label.text = message;
                label.color = color;
                label.raycastTarget = false;
            }

            StopAllCoroutines();
            StartCoroutine(Animate());
        }

        private IEnumerator Animate()
        {
            float elapsed = 0f;
            Vector3 worldStart = _rect != null ? _rect.position : Vector3.zero;
            Vector2 anchoredStart = _rect != null ? _rect.anchoredPosition : Vector2.zero;
            Color startColor = label != null ? label.color : Color.white;

            while (elapsed < lifetime)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / lifetime);
                if (_rect != null)
                {
                    if (_useWorldSpace)
                    {
                        _rect.position = worldStart + Vector3.up * (riseDistance * t);
                    }
                    else
                    {
                        _rect.anchoredPosition = anchoredStart + Vector2.up * (riseDistance * t);
                    }
                }

                if (label != null)
                {
                    Color c = startColor;
                    c.a = 1f - t;
                    label.color = c;
                }

                yield return null;
            }

            if (_pool != null)
            {
                _pool.Release(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
