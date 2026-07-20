using LastTrain.Core;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.UI
{
    /// <summary>
    /// Game Scene의 임시 컨트롤러 (개발 단위 1 범위).
    /// 실제 전투 시스템은 이후 개발 단위에서 구현되며,
    /// 지금은 Scene 전환 흐름 검증을 위한 임시 종료 버튼만 제공한다.
    /// </summary>
    public class GamePlaceholderController : MonoBehaviour
    {
        [SerializeField] private Button exitToResultButton;

        private void Awake()
        {
            if (exitToResultButton == null)
            {
                Debug.LogError("[GamePlaceholderController] exitToResultButton이 연결되지 않았습니다.", this);
                return;
            }

            exitToResultButton.onClick.AddListener(OnExitClicked);
        }

        private void OnDestroy()
        {
            if (exitToResultButton != null)
            {
                exitToResultButton.onClick.RemoveListener(OnExitClicked);
            }
        }

        private void OnExitClicked()
        {
            exitToResultButton.interactable = false;
            SceneFlow.Load(SceneNames.Result);
        }
    }
}
