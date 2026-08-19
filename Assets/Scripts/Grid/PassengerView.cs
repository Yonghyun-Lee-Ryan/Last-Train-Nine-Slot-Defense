using LastTrain.Data;
using LastTrain.Run;
using LastTrain.UI;
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
        [SerializeField] private Image starFrameImage;
        [SerializeField] private Text nameLabel;
        [SerializeField] private Text starLabel;
        [SerializeField] private UiSpriteAnimator portraitAnimator;

        private CanvasGroup _canvasGroup;
        private RectTransform _rectTransform;
        private GridManager _gridManager;
        private PassengerRuntime _passenger;
        private Transform _originalParent;
        private Vector2 _originalAnchoredPosition;
        private int _originalSiblingIndex;
        private Canvas _rootCanvas;
        private bool _isDragging;
        private VisualDatabase _visualDatabase;
        private PassengerVisualSet _visualSet;

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

            EnsureLabelLayout();

            if (nameLabel != null)
            {
                nameLabel.text = _passenger.Data.GetSlotLabel(_passenger.StarLevel);
            }

            if (starLabel != null)
            {
                starLabel.text = $"{_passenger.StarLevel}★";
            }

            ApplyVisuals();
        }

        private void EnsureLabelLayout()
        {
            // 초상화 크기·위치는 프리팹/기존 연출 그대로 유지한다.

            if (starLabel != null)
            {
                RectTransform starRect = starLabel.rectTransform;
                starRect.anchorMin = new Vector2(0.5f, 1f);
                starRect.anchorMax = new Vector2(0.5f, 1f);
                starRect.pivot = new Vector2(0.5f, 1f);
                starRect.anchoredPosition = new Vector2(0f, -2f);
                starRect.sizeDelta = new Vector2(200f, 36f);
                starLabel.fontSize = 26;
                starLabel.alignment = TextAnchor.MiddleCenter;
                starLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
                starLabel.verticalOverflow = VerticalWrapMode.Truncate;
                starLabel.raycastTarget = false;
            }

            if (nameLabel != null)
            {
                RectTransform nameRect = nameLabel.rectTransform;
                nameRect.anchorMin = new Vector2(0.08f, 0f);
                nameRect.anchorMax = new Vector2(0.92f, 0f);
                nameRect.pivot = new Vector2(0.5f, 0f);
                nameRect.anchoredPosition = new Vector2(0f, 8f);
                nameRect.sizeDelta = new Vector2(0f, 44f);
                nameLabel.fontSize = 18;
                nameLabel.alignment = TextAnchor.LowerCenter;
                nameLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
                nameLabel.verticalOverflow = VerticalWrapMode.Truncate;
                nameLabel.resizeTextForBestFit = true;
                nameLabel.resizeTextMinSize = 12;
                nameLabel.resizeTextMaxSize = 18;
                nameLabel.lineSpacing = 0.85f;
                nameLabel.raycastTarget = false;
            }

            EnsureSlotClip();
        }

        private void EnsureSlotClip()
        {
            // 칸(Content) 밖으로 삐져나온 텍스트/스프라이트를 잘라 낸다.
            if (GetComponent<RectMask2D>() == null)
            {
                gameObject.AddComponent<RectMask2D>();
            }
        }

        public void PlayAttackAnimation()
        {
            if (_visualSet != null && _visualSet.Attack.HasFrames && portraitAnimator != null)
            {
                portraitAnimator.PlayOneShot(_visualSet.Attack, ResumeIdle);
                return;
            }

            if (portraitImage != null)
            {
                portraitImage.transform.localScale = new Vector3(1.08f, 1.08f, 1f);
            }
        }

        public void PlaySkillAnimation()
        {
            if (_visualSet != null && _visualSet.Skill.HasFrames && portraitAnimator != null)
            {
                portraitAnimator.PlayOneShot(_visualSet.Skill, ResumeIdle);
                return;
            }

            if (portraitImage != null)
            {
                portraitImage.transform.localScale = new Vector3(1.18f, 1.18f, 1f);
                portraitImage.color = new Color(1f, 0.95f, 0.55f, 1f);
            }
        }

        public void PlayMergeAnimation()
        {
            if (_visualSet != null && _visualSet.Merge.HasFrames && portraitAnimator != null)
            {
                portraitAnimator.PlayOneShot(_visualSet.Merge, ResumeIdle);
                return;
            }

            if (portraitImage != null)
            {
                portraitImage.transform.localScale = new Vector3(1.15f, 1.15f, 1f);
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
            EnsureAnimator();
        }

        private void Update()
        {
            if (portraitImage != null && portraitImage.transform.localScale.x > 1.001f)
            {
                portraitImage.transform.localScale = Vector3.Lerp(
                    portraitImage.transform.localScale,
                    Vector3.one,
                    Time.deltaTime * 12f);
            }
        }

        private void EnsureAnimator()
        {
            if (portraitAnimator == null && portraitImage != null)
            {
                portraitAnimator = portraitImage.GetComponent<UiSpriteAnimator>();
                if (portraitAnimator == null)
                {
                    portraitAnimator = portraitImage.gameObject.AddComponent<UiSpriteAnimator>();
                }

                portraitAnimator.SetImage(portraitImage);
            }
        }

        private void ApplyVisuals()
        {
            if (portraitImage == null)
            {
                return;
            }

            EnsureVisualDatabase();
            _visualSet = null;
            if (_visualDatabase != null)
            {
                _visualDatabase.TryGetPassengerVisual(_passenger.Data.Id, out _visualSet);
            }

            Sprite portrait = _visualSet?.GetPortraitOrFallback();
            if (portrait != null)
            {
                portraitImage.sprite = portrait;
                portraitImage.color = Color.white;
                if (_visualSet != null && _visualSet.Idle.HasFrames && portraitAnimator != null)
                {
                    portraitAnimator.PlayIdle(_visualSet.Idle);
                }
            }
            else
            {
                portraitImage.sprite = null;
                portraitImage.color = GetPlaceholderColor(_passenger.Data.Id);
            }

            ApplyStarFrame();
        }

        private void ApplyStarFrame()
        {
            if (starFrameImage == null)
            {
                return;
            }

            VisualTheme theme = _visualDatabase?.Theme ?? VisualThemeLocator.Load();
            Sprite frame = theme?.GetStarFrame(_passenger.StarLevel);
            if (frame != null)
            {
                starFrameImage.sprite = frame;
                starFrameImage.color = VisualThemePalette.StarTint(_passenger.StarLevel);
                starFrameImage.enabled = true;
            }
            else
            {
                starFrameImage.enabled = false;
            }
        }

        private void ResumeIdle()
        {
            if (_visualSet != null && _visualSet.Idle.HasFrames && portraitAnimator != null)
            {
                portraitAnimator.PlayIdle(_visualSet.Idle);
            }
        }

        private void EnsureVisualDatabase()
        {
            if (_visualDatabase == null)
            {
                _visualDatabase = VisualDatabaseLocator.Load();
            }
        }

        private static Color GetPlaceholderColor(string passengerId)
        {
            int hash = passengerId?.GetHashCode() ?? 0;
            float hue = (hash & 0xFFFF) / (float)0xFFFF;
            return Color.HSVToRGB(hue, 0.45f, 0.9f);
        }
    }
}
