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
        [SerializeField] private Color lockedHighlightColor = new Color(0.95f, 0.42f, 0.16f, 0.62f);

        private bool _locked;
        private Text _lockLabel;

        public int SlotIndex => slotIndex;

        public RectTransform ContentAnchor => contentAnchor != null ? contentAnchor : transform as RectTransform;

        public void Configure(int index)
        {
            slotIndex = index;
        }

        public void SetHighlightActive(bool active)
        {
            if (_locked)
            {
                ApplyLockedVisual();
                return;
            }

            if (highlightImage == null)
            {
                return;
            }

            highlightImage.color = active ? dragTargetColor : normalHighlightColor;
            highlightImage.enabled = active || normalHighlightColor.a > 0.01f;
        }

        /// <summary>오늘의 막차 잠금 좌석. 합성 하이라이트보다 우선한다.</summary>
        public void SetLocked(bool locked)
        {
            if (_locked == locked)
            {
                if (locked)
                {
                    ApplyLockedVisual();
                }

                return;
            }

            _locked = locked;
            if (locked)
            {
                ApplyLockedVisual();
                return;
            }

            SetLockLabelVisible(false);
            SetHighlightActive(false);
        }

        private void ApplyLockedVisual()
        {
            if (highlightImage != null)
            {
                highlightImage.color = lockedHighlightColor;
                highlightImage.enabled = true;
            }

            SetLockLabelVisible(true);
        }

        private void SetLockLabelVisible(bool visible)
        {
            if (!visible && _lockLabel == null && transform.Find("LockLabel") == null)
            {
                return;
            }

            EnsureLockLabel();
            if (_lockLabel != null)
            {
                _lockLabel.enabled = visible;
                _lockLabel.gameObject.SetActive(visible);
            }
        }

        private void EnsureLockLabel()
        {
            if (_lockLabel != null)
            {
                return;
            }

            Transform existing = transform.Find("LockLabel");
            Text text = existing != null ? existing.GetComponent<Text>() : null;
            if (text == null)
            {
                var go = new GameObject("LockLabel", typeof(RectTransform), typeof(Text));
                go.transform.SetParent(transform, false);
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                text = go.GetComponent<Text>();
            }

            text.text = "잠김";
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 32;
            text.color = Color.white;
            text.raycastTarget = false;
            text.font = LastTrain.UI.GameFontProvider.Get();
            _lockLabel = text;
        }

        /// <summary>튜토리얼/UX용 합성 가능 슬롯 초록 강조.</summary>
        public void SetMergeHighlight(bool active)
        {
            if (_locked)
            {
                ApplyLockedVisual();
                return;
            }

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

            if (ContentAnchor != null && ContentAnchor.GetComponent<RectMask2D>() == null)
            {
                ContentAnchor.gameObject.AddComponent<RectMask2D>();
            }

            SetHighlightActive(false);
        }
    }
}
