using LastTrain.Core;
using LastTrain.Release;
using UnityEngine;

namespace LastTrain.Ux
{
    /// <summary>
    /// 화면 흔들림. Overlay Canvas 루트는 CanvasScaler가 매 프레임 위치를 덮어쓰므로
    /// SafeArea(또는 지정 RectTransform)의 localPosition을 LateUpdate에서 흔든다.
    /// </summary>
    public static class CameraShakeService
    {
        private static float _remaining;
        private static float _magnitude;
        private static Vector3 _origin;
        private static bool _hasOrigin;
        private static Transform _target;
        private static ScreenShakeDriver _driver;

        /// <summary>명시적으로 흔들 대상을 지정한다(보통 SafeArea).</summary>
        public static void SetTarget(Transform target)
        {
            RestoreOrigin();
            _remaining = 0f;
            _magnitude = 0f;
            _target = target;
            EnsureDriver();
        }

        public static void Shake(float duration = 0.15f, float magnitude = 16f)
        {
            GameSettingsService settings = AppRoot.Instance?.GameSettings;
            if (settings != null && !settings.ScreenShakeEnabled)
            {
                return;
            }

            if (settings != null && settings.LowFxMode)
            {
                duration *= 0.55f;
                magnitude *= 0.55f;
            }

            EnsureTarget();
            if (_target == null)
            {
                return;
            }

            EnsureDriver();

            if (!_hasOrigin)
            {
                _origin = _target.localPosition;
                _hasOrigin = true;
            }

            _remaining = Mathf.Max(_remaining, duration);
            _magnitude = Mathf.Max(_magnitude, magnitude);
        }

        /// <summary>ScreenShakeDriver LateUpdate에서 호출한다.</summary>
        public static void Tick(float unscaledDeltaTime)
        {
            if (_target == null || _remaining <= 0f)
            {
                RestoreOrigin();
                return;
            }

            _remaining -= unscaledDeltaTime;
            float x = (Random.value * 2f - 1f) * _magnitude;
            float y = (Random.value * 2f - 1f) * _magnitude;
            _target.localPosition = _origin + new Vector3(x, y, 0f);
            if (_remaining <= 0f)
            {
                RestoreOrigin();
            }
        }

        private static void RestoreOrigin()
        {
            if (_hasOrigin && _target != null)
            {
                _target.localPosition = _origin;
            }

            _hasOrigin = false;
            _magnitude = 0f;
        }

        private static void EnsureDriver()
        {
            if (_target == null)
            {
                return;
            }

            if (_driver != null && _driver.transform == _target)
            {
                return;
            }

            _driver = _target.GetComponent<ScreenShakeDriver>();
            if (_driver == null)
            {
                _driver = _target.gameObject.AddComponent<ScreenShakeDriver>();
            }
        }

        private static void EnsureTarget()
        {
            if (_target != null)
            {
                return;
            }

            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude);
            Transform bestSafeArea = null;
            Canvas bestCanvas = null;
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas == null || !canvas.isActiveAndEnabled)
                {
                    continue;
                }

                Canvas root = canvas.rootCanvas;
                if (bestCanvas != null && root.sortingOrder < bestCanvas.sortingOrder)
                {
                    continue;
                }

                bestCanvas = root;
                Transform safe = root.transform.Find("SafeArea");
                bestSafeArea = safe != null ? safe : root.transform;
            }

            if (bestSafeArea != null)
            {
                _target = bestSafeArea;
                return;
            }

            Camera cam = Camera.main;
            if (cam != null)
            {
                _target = cam.transform;
            }
        }
    }

    /// <summary>흔들림 Tick 전용. CanvasScaler 이후에 적용되도록 LateUpdate에서 동작한다.</summary>
    [DefaultExecutionOrder(1000)]
    public sealed class ScreenShakeDriver : MonoBehaviour
    {
        private void LateUpdate()
        {
            CameraShakeService.Tick(Time.unscaledDeltaTime);
        }
    }
}
