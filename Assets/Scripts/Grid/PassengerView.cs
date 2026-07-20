using LastTrain.Run;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LastTrain.Grid
{
    /// <summary>
    /// 승객 표시 전용 View. 드래그 입력을 GridManager에 위임한다.
    /// RunState를 직접 수정하지 않는다.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class PassengerView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [SerializeField] private Image portraitImage;
        [SerializeField] private Text nameLabel;
        [SerializeField] private Text starLabel;

        private CanvasGroup _canvasGroup;
        private RectTransform _rectTransform;
        private GridManager _gridManager;
        private PassengerRuntime _passenger;
        private Transform _originalParent;
        private Vector2 _originalAnchoredPosition;
        private int _originalSiblingIndex;
        private Canvas _rootCanvas;
        private bool _isDragging;

        public PassengerRuntime Passenger => _passenger;
        public int SlotIndex => _passenger?.GridSlotIndex ?? -1;

        public void Bind(GridManager gridManager, PassengerRuntime passenger)
        {
            _gridManager = gridManager;
            _passenger = passenger;
            RefreshDisplay();
        }

        public void RefreshDisplay()
        {
            if (_passenger == null || _passenger.Data == null)
            {
                if (nameLabel != null)
                {
                    nameLabel.text = string.Empty;
                }

                if (starLabel != null)
                {
                    starLabel.text = string.Empty;
                }

                return;
            }

            if (nameLabel != null)
            {
                nameLabel.text = _passenger.Data.GetDisplayNameAtStar(_passenger.StarLevel);
            }

            if (starLabel != null)
            {
                starLabel.text = $"{_passenger.StarLevel}★";
            }

            if (portraitImage != null)
            {
                portraitImage.color = GetPlaceholderColor(_passenger.Data.Id);
            }
        }

        public void SnapToSlot(GridSlot slot)
        {
            if (slot == null)
            {
                return;
            }

            RectTransform anchor = slot.ContentAnchor;
            _rectTransform.SetParent(anchor, false);
            _rectTransform.anchorMin = Vector2.zero;
            _rectTransform.anchorMax = Vector2.one;
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;
            _rectTransform.localScale = Vector3.one;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_gridManager == null || _passenger == null || !_gridManager.CanDrag)
            {
                return;
            }

            _isDragging = true;
            _originalParent = _rectTransform.parent;
            _originalAnchoredPosition = _rectTransform.anchoredPosition;
            _originalSiblingIndex = _rectTransform.GetSiblingIndex();

            _rootCanvas = _gridManager.RootCanvas;
            if (_rootCanvas != null)
            {
                _rectTransform.SetParent(_rootCanvas.transform, true);
            }

            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.alpha = 0.85f;

            _gridManager.HandleDragStarted(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging || _rootCanvas == null)
            {
                return;
            }

            RectTransform canvasRect = _rootCanvas.transform as RectTransform;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPoint))
            {
                _rectTransform.localPosition = localPoint;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging)
            {
                return;
            }

            _isDragging = false;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.alpha = 1f;

            _gridManager.HandleDragEnded(this, eventData.position, eventData.pressEventCamera);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_isDragging || _gridManager == null || _passenger == null)
            {
                return;
            }

            _gridManager.HandlePassengerClicked(this);
        }

        public void RevertToOriginalTransform()
        {
            if (_originalParent != null)
            {
                _rectTransform.SetParent(_originalParent, false);
                _rectTransform.SetSiblingIndex(_originalSiblingIndex);
                _rectTransform.anchoredPosition = _originalAnchoredPosition;
            }
        }

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _rectTransform = transform as RectTransform;
        }

        private static Color GetPlaceholderColor(string passengerId)
        {
            int hash = passengerId?.GetHashCode() ?? 0;
            float hue = (hash & 0xFFFF) / (float)0xFFFF;
            return Color.HSVToRGB(hue, 0.45f, 0.9f);
        }
    }
}
