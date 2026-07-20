using UnityEngine;

namespace LastTrain.UI
{
    /// <summary>
    /// RectTransform을 기기의 Safe Area(노치·펀치홀·둥근 모서리 제외 영역)에 맞춘다.
    /// Canvas 하위의 Safe Area 패널 RectTransform에 부착한다.
    /// 해상도나 방향이 바뀌면 자동으로 다시 적용한다.
    ///
    /// anchor 계산은 SafeAreaCalculator(순수 함수)에 위임한다.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        [Tooltip("가로 방향 Safe Area 여백을 적용할지 여부")]
        [SerializeField] private bool applyHorizontal = true;

        [Tooltip("세로 방향 Safe Area 여백을 적용할지 여부")]
        [SerializeField] private bool applyVertical = true;

        private RectTransform _rectTransform;
        private Rect _lastSafeArea = Rect.zero;
        private Vector2Int _lastScreenSize = Vector2Int.zero;
        private ScreenOrientation _lastOrientation = ScreenOrientation.AutoRotation;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            ApplySafeArea();
        }

        private void Update()
        {
            if (HasScreenChanged())
            {
                ApplySafeArea();
            }
        }

        private bool HasScreenChanged()
        {
            return _lastSafeArea != Screen.safeArea
                   || _lastScreenSize.x != Screen.width
                   || _lastScreenSize.y != Screen.height
                   || _lastOrientation != Screen.orientation;
        }

        private void ApplySafeArea()
        {
            if (_rectTransform == null)
            {
                return;
            }

            _lastSafeArea = Screen.safeArea;
            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            _lastOrientation = Screen.orientation;

            bool valid = SafeAreaCalculator.TryCalculateAnchors(
                Screen.safeArea,
                Screen.width,
                Screen.height,
                applyHorizontal,
                applyVertical,
                out Vector2 anchorMin,
                out Vector2 anchorMax);

            if (!valid)
            {
                return;
            }

            _rectTransform.anchorMin = anchorMin;
            _rectTransform.anchorMax = anchorMax;
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;
        }
    }
}
