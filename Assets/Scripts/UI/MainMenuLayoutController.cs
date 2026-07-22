using UnityEngine;

namespace LastTrain.UI
{
    /// <summary>메인 메뉴 UI를 모든 컴포넌트 초기화 후 한 번 더 정렬한다.</summary>
    [DefaultExecutionOrder(200)]
    public sealed class MainMenuLayoutController : MonoBehaviour
    {
        private void Start()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            Transform safeArea = canvas != null ? canvas.transform.Find("SafeArea") : null;
            MainMenuUiLayout.Apply(safeArea);
        }
    }
}
