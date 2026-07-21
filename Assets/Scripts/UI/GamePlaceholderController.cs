using LastTrain.Core;
using LastTrain.Run;
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
            // 실제 게임 종료 흐름이 구현되었으므로 개발용 강제 결과 버튼은 더 이상 사용하지 않는다.
            if (exitToResultButton != null)
            {
                Destroy(exitToResultButton.gameObject);
                exitToResultButton = null;
            }

            enabled = false;
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

            AppRoot appRoot = AppRoot.Instance;
            if (appRoot != null && appRoot.GameSession.HasActiveRun)
            {
                appRoot.GameSession.EndRun(RunEndReason.Abandoned, isVictory: false);
            }

            SceneFlow.Load(SceneNames.Result);
        }
    }
}
