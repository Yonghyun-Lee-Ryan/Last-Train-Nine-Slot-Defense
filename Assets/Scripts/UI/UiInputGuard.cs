using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.UI
{
    /// <summary>버튼 중복 입력 방지.</summary>
    public sealed class UiInputGuard
    {
        private float _lockedUntil;
        private readonly float _lockDuration;

        public UiInputGuard(float lockDurationSeconds = 0.35f)
        {
            _lockDuration = Mathf.Max(0.05f, lockDurationSeconds);
        }

        public bool IsLocked => Time.unscaledTime < _lockedUntil;

        public bool TryAcquire()
        {
            if (IsLocked)
            {
                return false;
            }

            _lockedUntil = Time.unscaledTime + _lockDuration;
            return true;
        }

        public void Reset()
        {
            _lockedUntil = 0f;
        }

        public static void SetInteractable(Button button, bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }
    }
}
