using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.Grid
{
    /// <summary>
    /// 3×3 Grid의 단일 슬롯. 드롭 대상·하이라이트 표시를 담당한다.
    /// 승객 상태는 RunState에서 관리하며, 슬롯은 View 앵커 역할만 한다.
    /// </summary>
    public class GridSlot : MonoBehaviour
    {
        [SerializeField] private int slotIndex;
        [SerializeField] private RectTransform contentAnchor;
        [SerializeField] private Image highlightImage;

        [Header("Highlight Colors")]
        [SerializeField] private Color normalHighlightColor = new Color(1f, 1f, 1f, 0f);
        [SerializeField] private Color dragTargetColor = new Color(0.3f, 0.8f, 1f, 0.35f);
        [SerializeField] private Color mergeHighlightColor = new Color(0.25f, 0.95f, 0.4f, 0.55f);

        public int SlotIndex => slotIndex;

        public RectTransform ContentAnchor => contentAnchor != null ? contentAnchor : transform as RectTransform;

        public void Configure(int index)
        {
            slotIndex = index;
        }

        public void SetHighlightActive(bool active)
        {
            if (highlightImage == null)
            {
                return;
            }

            highlightImage.color = active ? dragTargetColor : normalHighlightColor;
            highlightImage.enabled = active || normalHighlightColor.a > 0.01f;
        }

        /// <summary>튜토리얼/UX용 합성 가능 슬롯 초록 강조.</summary>
        public void SetMergeHighlight(bool active)
        {
            if (highlightImage == null)
            {
                return;
            }

            if (active)
            {
                highlightImage.color = mergeHighlightColor;
                highlightImage.enabled = true;
                return;
            }

            SetHighlightActive(false);
        }

        public bool ContainsScreenPoint(Vector2 screenPosition, Camera eventCamera)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(ContentAnchor, screenPosition, eventCamera);
        }

        private void Reset()
        {
            contentAnchor = transform as RectTransform;

            if (highlightImage == null)
            {
                highlightImage = GetComponent<Image>();
            }
        }

        private void Awake()
        {
            if (contentAnchor == null)
            {
                contentAnchor = transform as RectTransform;
            }

            SetHighlightActive(false);
        }
    }
}
