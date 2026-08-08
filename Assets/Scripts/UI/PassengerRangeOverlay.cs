using LastTrain.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.UI
{
    /// <summary>
    /// 선택된 승객의 공격 사거리를 SafeArea 로컬 원으로 표시한다.
    /// </summary>
    public sealed class PassengerRangeOverlay : MonoBehaviour
    {
        public const string RootName = "PassengerRangeOverlay";

        [SerializeField] private Color fillColor = new(0.35f, 0.95f, 0.75f, 0.55f);
        [SerializeField] private RectTransform circleRect;
        [SerializeField] private Image circleImage;

        public float VisibleRadius { get; private set; }

        public bool IsVisible => gameObject.activeSelf && VisibleRadius > 0f;

        public static float RadiusForEffectiveRange(float dataRange)
        {
            return BattleConstants.ToWorldRange(dataRange);
        }

        public static PassengerRangeOverlay Ensure(RectTransform parent)
        {
            if (parent == null)
            {
                return null;
            }

            Transform existing = parent.Find(RootName);
            PassengerRangeOverlay overlay = existing != null
                ? existing.GetComponent<PassengerRangeOverlay>()
                : null;
            if (overlay == null)
            {
                var go = new GameObject(RootName, typeof(RectTransform), typeof(PassengerRangeOverlay));
                var root = go.GetComponent<RectTransform>();
                root.SetParent(parent, false);
                root.anchorMin = new Vector2(0.5f, 0.5f);
                root.anchorMax = new Vector2(0.5f, 0.5f);
                root.pivot = new Vector2(0.5f, 0.5f);
                root.anchoredPosition = Vector2.zero;
                root.sizeDelta = Vector2.zero;
                root.SetAsLastSibling();
                overlay = go.GetComponent<PassengerRangeOverlay>();
            }

            overlay.EnsureCircle();
            overlay.Hide();
            return overlay;
        }

        private void Awake()
        {
            EnsureCircle();
            Hide();
        }

        public void Show(Vector2 combatLocalCenter, float radiusLocal)
        {
            EnsureCircle();
            VisibleRadius = Mathf.Max(0f, radiusLocal);
            if (VisibleRadius <= 0.01f || circleRect == null)
            {
                Hide();
                return;
            }

            float diameter = VisibleRadius * 2f;
            circleRect.anchoredPosition = combatLocalCenter;
            circleRect.sizeDelta = new Vector2(diameter, diameter);
            gameObject.SetActive(true);
            if (circleImage != null)
            {
                circleImage.enabled = true;
            }
        }

        public void Hide()
        {
            VisibleRadius = 0f;
            if (circleImage != null)
            {
                circleImage.enabled = false;
            }

            gameObject.SetActive(false);
        }

        private void EnsureCircle()
        {
            Transform child = transform.Find("RangeCircle");
            if (child == null)
            {
                var go = new GameObject("RangeCircle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                child = go.transform;
                child.SetParent(transform, false);
            }

            circleRect = child as RectTransform;
            circleRect.anchorMin = new Vector2(0.5f, 0.5f);
            circleRect.anchorMax = new Vector2(0.5f, 0.5f);
            circleRect.pivot = new Vector2(0.5f, 0.5f);
            circleRect.anchoredPosition = Vector2.zero;
            if (circleRect.sizeDelta.sqrMagnitude < 1f)
            {
                circleRect.sizeDelta = new Vector2(100f, 100f);
            }

            circleImage = child.GetComponent<Image>();
            circleImage.sprite = UiProceduralSprites.SoftCircle();
            circleImage.color = fillColor;
            circleImage.raycastTarget = false;
            circleImage.type = Image.Type.Simple;
            circleImage.preserveAspect = true;
        }
    }
}
