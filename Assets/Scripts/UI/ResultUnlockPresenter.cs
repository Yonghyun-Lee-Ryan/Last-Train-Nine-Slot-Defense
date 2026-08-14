using System.Collections.Generic;
using UnityEngine;

namespace LastTrain.UI
{
    /// <summary>결과 화면에서 조각·해금을 순서대로 보여 준다.</summary>
    public sealed class ResultUnlockPresenter : MonoBehaviour
    {
        private AchievementToastController _toast;
        private IReadOnlyList<string> _lines;
        private int _index;

        public bool IsPlaying => _lines != null && _index < _lines.Count;
        public int RemainingCount => _lines == null ? 0 : Mathf.Max(0, _lines.Count - _index);

        public void Play(IReadOnlyList<string> lines)
        {
            CancelInvoke();
            _lines = lines;
            _index = 0;
            if (_lines == null || _lines.Count == 0)
            {
                return;
            }

            if (_toast == null)
            {
                _toast = GetComponent<AchievementToastController>();
                if (_toast == null)
                {
                    _toast = gameObject.AddComponent<AchievementToastController>();
                }
            }

            ShowNext();
        }

        private void ShowNext()
        {
            if (_lines == null || _index >= _lines.Count || _toast == null)
            {
                return;
            }

            _toast.ShowMessage(_lines[_index]);
            _index++;
            if (_index < _lines.Count && Application.isPlaying)
            {
                Invoke(nameof(ShowNext), 1.65f);
            }
        }

        private void OnDestroy()
        {
            CancelInvoke();
        }
    }
}
