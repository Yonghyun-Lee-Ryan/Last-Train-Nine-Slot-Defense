using LastTrain.Core;
using LastTrain.Run;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.UI
{
    /// <summary>
    /// Result Scene의 임시 컨트롤러 (개발 단위 1 범위).
    /// 다시 시작(Game)과 메인 메뉴(MainMenu) 이동을 제공한다.
    /// 실제 결과 통계 표시는 개발 단위 15에서 구현된다.
    /// </summary>
    public class ResultPlaceholderController : MonoBehaviour
    {
        [SerializeField] private Button retryButton;
        [SerializeField] private Button mainMenuButton;

        private void Awake()
        {
            if (retryButton != null)
            {
                retryButton.onClick.AddListener(OnRetryClicked);
            }
            else
            {
                Debug.LogError("[ResultPlaceholderController] retryButton이 연결되지 않았습니다.", this);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            }
            else
            {
                Debug.LogError("[ResultPlaceholderController] mainMenuButton이 연결되지 않았습니다.", this);
            }
        }

        private void OnDestroy()
        {
            if (retryButton != null)
            {
                retryButton.onClick.RemoveListener(OnRetryClicked);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
            }
        }

        private void OnRetryClicked()
        {
            SetButtonsInteractable(false);

            AppRoot appRoot = AppRoot.Instance;
            if (appRoot != null)
            {
                appRoot.GameSession.StartNewRun();
            }

            SceneFlow.Load(SceneNames.Game);
        }

        private void OnMainMenuClicked()
        {
            SetButtonsInteractable(false);

            AppRoot appRoot = AppRoot.Instance;
            if (appRoot != null && appRoot.GameSession.HasActiveRun)
            {
                appRoot.GameSession.EndRun(RunEndReason.Abandoned, isVictory: false);
            }

            SceneFlow.Load(SceneNames.MainMenu);
        }

        private void SetButtonsInteractable(bool value)
        {
            if (retryButton != null)
            {
                retryButton.interactable = value;
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.interactable = value;
            }
        }
    }
}
