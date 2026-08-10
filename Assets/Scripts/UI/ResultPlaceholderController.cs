using LastTrain.Ads;
using LastTrain.Core;
using LastTrain.Data;
using LastTrain.Difficulty;
using LastTrain.Run;
using LastTrain.Save;
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
        [Header("Result UI")]
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text messageLabel;
        [SerializeField] private Text statsLabel;

        [SerializeField] private Button retryButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button doubleRewardAdButton;

        private readonly UiInputGuard _adGuard = new(0.25f);

        private void Awake()
        {
            EnsureDoubleRewardButton();
            ResultUiLayout.EnsureButtonGroup(retryButton, doubleRewardAdButton, mainMenuButton);
            RefreshResultUi();

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

            if (doubleRewardAdButton != null)
            {
                doubleRewardAdButton.onClick.AddListener(OnDoubleRewardClicked);
                RefreshDoubleRewardButton();
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

            if (doubleRewardAdButton != null)
            {
                doubleRewardAdButton.onClick.RemoveListener(OnDoubleRewardClicked);
            }
        }

        private void OnRetryClicked()
        {
            SetButtonsInteractable(false);

            AppRoot appRoot = AppRoot.Instance;
            if (appRoot != null)
            {
                var config = RunStartConfig.CreateDefault();
                config.DifficultyId = appRoot.GameSession.LastResult?.DifficultyId
                    ?? DifficultySelectionState.SelectedDifficultyId;
                appRoot.GameSession.StartNewRun(config);
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

        private void OnDoubleRewardClicked()
        {
            if (!_adGuard.TryAcquire())
            {
                return;
            }

            AdCoordinator ads = AppRoot.Instance?.Ads;
            if (ads == null || !ads.IsReady(RewardedAdPlacement.DoubleResultReward))
            {
                return;
            }

            UiInputGuard.SetInteractable(doubleRewardAdButton, false);
            ads.ShowDoubleResultReward(result =>
            {
                if (result == AdResult.Completed && statsLabel != null)
                {
                    statsLabel.text = RunResultFormatter.BuildStatsText(AppRoot.Instance.GameSession.LastResult)
                        + RunResultFormatter.BuildMetaRewardText(MetaSaveSystem.LastApplyResult)
                        + "\n\n[광고] 승차권 조각 2배 보너스 지급!";
                }

                RefreshDoubleRewardButton();
            });
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

            RefreshDoubleRewardButton();
        }

        private void RefreshDoubleRewardButton()
        {
            if (doubleRewardAdButton == null)
            {
                return;
            }

            AdCoordinator ads = AppRoot.Instance?.Ads;
            bool ready = ads != null && ads.IsReady(RewardedAdPlacement.DoubleResultReward);
            // 광고 SDK 미배선(NoOp) 시 비활성 버튼만 두지 않고 숨긴다.
            doubleRewardAdButton.gameObject.SetActive(ready);
            doubleRewardAdButton.interactable = ready;
        }

        private void EnsureDoubleRewardButton()
        {
            if (doubleRewardAdButton != null)
            {
                return;
            }

            if (retryButton == null)
            {
                return;
            }

            Button template = retryButton;
            GameObject go = Instantiate(template.gameObject, template.transform.parent);
            go.name = "DoubleRewardAdButton";
            doubleRewardAdButton = go.GetComponent<Button>();
            doubleRewardAdButton.onClick.RemoveAllListeners();

            Text label = go.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = "광고로 보상 2배";
            }

            UiButtonStyler.ApplyStandardTheme(doubleRewardAdButton);
            UiButtonStyler.EnsureAdIcon(doubleRewardAdButton);
            UiButtonStyler.OffsetButtonLabel(doubleRewardAdButton);
        }

        private void RefreshResultUi()
        {
            AppRoot appRoot = AppRoot.Instance;
            if (appRoot == null || appRoot.GameSession == null)
            {
                return;
            }

            RunResult result = appRoot.GameSession.LastResult;
            if (result == null)
            {
                return;
            }

            if (titleLabel != null)
            {
                titleLabel.text = RunResultFormatter.GetTitle(result);
            }

            if (messageLabel != null)
            {
                messageLabel.text = RunResultFormatter.GetOverlayMessage(result);
            }

            if (statsLabel != null)
            {
                statsLabel.text = RunResultFormatter.BuildStatsText(result)
                    + RunResultFormatter.BuildMetaRewardText(MetaSaveSystem.LastApplyResult);
            }
        }
    }
}
