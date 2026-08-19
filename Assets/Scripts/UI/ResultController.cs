using System;
using LastTrain.Ads;
using LastTrain.Core;
using LastTrain.Data;
using LastTrain.Difficulty;
using LastTrain.Integrations;
using LastTrain.Release;
using LastTrain.Run;
using LastTrain.Save;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.UI
{
    /// <summary>회차 결과 화면. 승/패 원인, 보상·해금 연출, 광고 2배를 표시한다.</summary>
    public class ResultController : MonoBehaviour
    {
        [Header("Result UI")]
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text messageLabel;
        [SerializeField] private Text statsLabel;

        [SerializeField] private Button retryButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button doubleRewardAdButton;

        private readonly UiInputGuard _adGuard = new(0.25f);
        private ResultUnlockPresenter _unlockPresenter;

        private void Awake()
        {
            EnsureDoubleRewardButton();
            ResultUiLayout.EnsureButtonGroup(retryButton, doubleRewardAdButton, mainMenuButton);
            ResultUiLayout.ApplyContent(titleLabel, messageLabel, statsLabel);
            RefreshResultUi();

            if (retryButton != null)
            {
                retryButton.onClick.AddListener(OnRetryClicked);
            }
            else
            {
                Debug.LogError("[ResultController] retryButton이 연결되지 않았습니다.", this);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            }
            else
            {
                Debug.LogError("[ResultController] mainMenuButton이 연결되지 않았습니다.", this);
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
                RunResult last = appRoot.GameSession.LastResult;
                RunStartConfig config;
                if (last != null && string.Equals(last.LineId, RouteIds.Quick, StringComparison.Ordinal))
                {
                    config = RunStartConfig.CreateQuickRun(last.DifficultyId);
                }
                else if (last != null && last.IsEndlessRun)
                {
                    config = RunStartConfig.CreateEndlessRun(last.DifficultyId);
                }
                else
                {
                    config = RunStartConfig.CreateDefault();
                    config.DifficultyId = last?.DifficultyId ?? DifficultySelectionState.SelectedDifficultyId;
                }

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
                        + RunResultFormatter.BuildMetaRewardText(
                            MetaSaveSystem.LastApplyResult,
                            GameDatabaseLocator.Load())
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

            GameDatabase database = GameDatabaseLocator.Load();
            if (titleLabel != null)
            {
                titleLabel.text = RunResultFormatter.GetTitle(result);
            }

            if (messageLabel != null)
            {
                messageLabel.text = RunResultFormatter.GetCauseLine(result);
            }

            if (statsLabel != null)
            {
                statsLabel.text = RunResultFormatter.BuildStatsText(result)
                    + RunResultFormatter.BuildMetaRewardText(MetaSaveSystem.LastApplyResult, database);
            }

            PlayUnlockPresentation(database);

            if (result.IsVictory)
            {
                MetaSaveData meta = MetaSaveSystem.LoadOrCreate();
                InAppReviewPromptService.TryPrompt(meta);
            }

            AppRoot.Instance?.Integrations?.Interstitials?.TryShowAfterRunEnded();
        }

        private void PlayUnlockPresentation(GameDatabase database)
        {
            var lines = RunResultFormatter.CollectRevealLines(MetaSaveSystem.LastApplyResult, database);
            if (lines == null || lines.Count == 0)
            {
                return;
            }

            if (_unlockPresenter == null)
            {
                _unlockPresenter = GetComponent<ResultUnlockPresenter>();
                if (_unlockPresenter == null)
                {
                    _unlockPresenter = gameObject.AddComponent<ResultUnlockPresenter>();
                }
            }

            _unlockPresenter.Play(lines);
        }
    }
}
