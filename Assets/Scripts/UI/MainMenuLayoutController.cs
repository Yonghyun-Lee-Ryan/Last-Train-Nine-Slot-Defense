using System.Collections;
using UnityEngine;

namespace LastTrain.UI
{
    /// <summary>메인 메뉴 UI를 모든 컴포넌트 초기화 후·레이아웃 확정 후 정렬한다.</summary>
    [DefaultExecutionOrder(200)]
    public sealed class MainMenuLayoutController : MonoBehaviour
    {
        private Coroutine _applyRoutine;

        private void Start()
        {
            ApplyNow();
            QueueApply();
        }

        private void OnEnable()
        {
            QueueApply();
        }

        private void QueueApply()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (_applyRoutine != null)
            {
                StopCoroutine(_applyRoutine);
            }

            _applyRoutine = StartCoroutine(ApplyAfterLayoutSettled());
        }

        private IEnumerator ApplyAfterLayoutSettled()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            ApplyNow();
            yield return new WaitForEndOfFrame();
            ApplyNow();
            _applyRoutine = null;
        }

        private void ApplyNow()
        {
            Transform safeArea = MainMenuUiLayout.FindOwnedSafeArea(this);
            MainMenuUiLayout.Apply(safeArea);
        }
    }
}
