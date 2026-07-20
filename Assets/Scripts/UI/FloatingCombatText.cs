using System.Collections;
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

        public void Play(string message, Color color, Vector2 anchoredStart)
        {
            if (label == null)
            {
                label = GetComponentInChildren<Text>();
            }

            _rect = transform as RectTransform;
            if (label != null)
            {
                label.text = message;
                label.color = color;
            }

            if (_rect != null)
            {
                _rect.anchoredPosition = anchoredStart;
            }

            StopAllCoroutines();
            StartCoroutine(Animate());
        }

        private IEnumerator Animate()
        {
            float elapsed = 0f;
            Vector2 start = _rect != null ? _rect.anchoredPosition : Vector2.zero;
            Color startColor = label != null ? label.color : Color.white;

            while (elapsed < lifetime)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / lifetime);
                if (_rect != null)
                {
                    _rect.anchoredPosition = start + Vector2.up * (riseDistance * t);
                }

                if (label != null)
                {
                    Color c = startColor;
                    c.a = 1f - t;
                    label.color = c;
                }

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
