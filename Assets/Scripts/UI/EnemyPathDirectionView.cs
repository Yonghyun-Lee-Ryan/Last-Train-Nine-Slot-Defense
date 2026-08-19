using LastTrain.Battle;
using LastTrain.Run;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.UI
{
    /// <summary>
    /// 준비 단계에서만 적 지그재그 경로 방향 화살표를 표시한다.
    /// </summary>
    public sealed class EnemyPathDirectionView : MonoBehaviour
    {
        public const string RootName = "EnemyPathDirectionView";

        [SerializeField] private float arrowSpacing = 200f;
        [SerializeField] private Vector2 arrowSize = new(48f, 32f);
        [SerializeField] private Color arrowColor = new(1f, 0.9f, 0.35f, 0.72f);

        private RectTransform _root;
        private bool _built;

        public int ArrowCount => _root != null ? _root.childCount : 0;

        public bool IsShowing => isActiveAndEnabled && gameObject.activeSelf;

        public static bool ShouldShow(RunPhase phase)
        {
            return phase == RunPhase.Preparing;
        }

        public static EnemyPathDirectionView Ensure(RectTransform parent)
        {
            if (parent == null)
            {
                return null;
            }

            Transform existing = parent.Find(RootName);
            EnemyPathDirectionView view = existing != null
                ? existing.GetComponent<EnemyPathDirectionView>()
                : null;
            if (view == null)
            {
                var go = new GameObject(RootName, typeof(RectTransform), typeof(EnemyPathDirectionView));
                var rect = go.GetComponent<RectTransform>();
                rect.SetParent(parent, false);
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = Vector2.zero;
                rect.SetSiblingIndex(Mathf.Min(2, parent.childCount - 1));
                view = go.GetComponent<EnemyPathDirectionView>();
            }

            view.Rebuild();
            return view;
        }

        private void Awake()
        {
            _root = transform as RectTransform;
            if (!_built)
            {
                Rebuild();
            }
        }

        public void SetVisible(bool visible)
        {
            if (visible && !_built)
            {
                Rebuild();
            }

            gameObject.SetActive(visible);
        }

        public void Rebuild()
        {
            _root ??= transform as RectTransform;
            if (_root == null)
            {
                return;
            }

            for (int i = _root.childCount - 1; i >= 0; i--)
            {
                DestroyChild(_root.GetChild(i).gameObject);
            }

            Vector2[] points = BattleConstants.GetEnemyPathPoints();
            Sprite sprite = UiProceduralSprites.Chevron();
            float spacing = Mathf.Max(90f, arrowSpacing);

            for (int i = 0; i < points.Length - 1; i++)
            {
                Vector2 from = points[i];
                Vector2 to = points[i + 1];
                Vector2 delta = to - from;
                float length = delta.magnitude;
                if (length < 1f)
                {
                    continue;
                }

                Vector2 dir = delta / length;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                int count = Mathf.Max(1, Mathf.FloorToInt(length / spacing));
                for (int a = 0; a < count; a++)
                {
                    float t = (a + 1f) / (count + 1f);
                    Vector2 pos = Vector2.Lerp(from, to, t);
                    CreateArrow(pos, angle, sprite, a + i * 10);
                }
            }

            _built = true;
        }

        private void CreateArrow(Vector2 anchoredPosition, float zAngle, Sprite sprite, int index)
        {
            var go = new GameObject($"PathArrow_{index}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(_root, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = arrowSize;
            rect.localRotation = Quaternion.Euler(0f, 0f, zAngle);

            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = arrowColor;
            image.raycastTarget = false;
            image.preserveAspect = true;
        }

        private static void DestroyChild(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(go);
            }
            else
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
