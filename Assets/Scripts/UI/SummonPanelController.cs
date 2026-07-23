using System;
using System.Collections;
using LastTrain.Ads;
using LastTrain.Audio;
using LastTrain.Core;
using LastTrain.Data;
using LastTrain.Grid;
using LastTrain.Passenger;
using LastTrain.Run;
using LastTrain.Save;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LastTrain.UI
{
    /// <summary>
    /// 소환·리롤·판매 UI. 개발 단위 9 최소 조작 패널.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class SummonPanelController : MonoBehaviour
    {
        private const float EphemeralStatusSeconds = 2.5f;

        [Header("References")]
        [SerializeField] private GridManager gridManager;
        [SerializeField] private GameDatabase gameDatabase;
        [SerializeField] private SummonEconomyConfig economyConfig;
        [SerializeField] private GameBattleBootstrap battleBootstrap;
        [SerializeField] private int randomSeed;

        [Header("HUD")]
        [SerializeField] private Text coinLabel;
        [SerializeField] private Text costLabel;
        [SerializeField] private Text statusLabel;
        [SerializeField] private Button summonButton;

        /// <summary>좌하단 코인 라벨. 획득 연출 기준점으로 사용한다.</summary>
        public Text CoinLabel => coinLabel;

        [Header("Offer Popup")]
        [SerializeField] private GameObject offerPanel;
        [SerializeField] private Button[] offerButtons = new Button[3];
        [SerializeField] private Text[] offerLabels = new Text[3];
        [SerializeField] private Button cancelOfferButton;
        [SerializeField] private Button freeRerollButton;
        [SerializeField] private Button adRerollButton;

        private SummonManager _summonManager;
        private RunState _runState;
        private GameSession _session;
        private readonly UiInputGuard _adInputGuard = new(0.2f);
        private Coroutine _statusClearRoutine;
        private string _ephemeralStatusToken;

        private void Start()
        {
            AppRoot appRoot = AppRoot.Instance;
            if (appRoot == null || !appRoot.GameSession.HasActiveRun)
            {
                Debug.LogWarning("[SummonPanelController] 활성 RunState가 없습니다.", this);
                return;
            }

            EnsureReferences();
            if (gameDatabase == null || economyConfig == null)
            {
                Debug.LogError(
                    "[SummonPanelController] gameDatabase 또는 economyConfig가 없습니다. " +
                    "Tools > 막차 생존 > 개발 단위 9 소환 UI 추가를 다시 실행하세요.",
                    this);
                return;
            }

            _session = appRoot.GameSession;
            _runState = appRoot.GameSession.RunState;
            if (gridManager == null)
            {
                gridManager = FindAnyObjectByType<GridManager>();
            }

            int seed;
            if (_runState != null && _runState.IsDailyRun && _runState.RandomSeed != 0)
            {
                seed = _runState.RandomSeed;
            }
            else
            {
                seed = randomSeed != 0
                    ? randomSeed
                    : (_runState != null && _runState.RandomSeed != 0
                        ? _runState.RandomSeed
                        : unchecked(Environment.TickCount));
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (_runState != null
                    && LastTrain.DebugTools.DebugCombatSettings.FixedSeed.HasValue)
                {
                    seed = LastTrain.DebugTools.DebugCombatSettings.FixedSeed.Value;
                }
#endif
            }
            var random = new RandomService(seed);
            var unlockedPassengers = MetaSaveSystem.FilterUnlockedPassengers(gameDatabase.Passengers);
            if (_runState != null && !string.IsNullOrWhiteSpace(_runState.LiveEventId))
            {
                unlockedPassengers = FilterLiveEventPassengers(unlockedPassengers, _runState);
            }

            var offerService = new PassengerOfferService(
                unlockedPassengers,
                random,
                economyConfig.OfferCount);

            _summonManager = new SummonManager(_runState, economyConfig, offerService);
            _summonManager.StatusMessage += HandleStatusMessage;
            AppRoot.Instance?.AnalyticsRunBinder?.BindSummon(_summonManager);

            WireButtons();

            if (_session != null)
            {
                _session.RunEnded += HandleRunEnded;
            }
            if (gridManager != null)
            {
                gridManager.PassengerSelected += HandlePassengerSelected;
                gridManager.PassengerDropped += HandlePassengerDropped;
            }

            _runState.Currency.CoinsChanged += HandleCoinsChanged;
            _runState.Summon.OffersChanged += RefreshOfferPanel;

            if (offerPanel != null)
            {
                offerPanel.SetActive(false);
            }

            RefreshHud();
            SetStatus("소환 준비 완료", ephemeral: true);
        }

        private void EnsureReferences()
        {
#if UNITY_EDITOR
            if (gameDatabase == null)
            {
                gameDatabase = AssetDatabase.LoadAssetAtPath<GameDatabase>(
                    "Assets/Data/GameDatabase.asset");
            }

            if (economyConfig == null)
            {
                economyConfig = AssetDatabase.LoadAssetAtPath<SummonEconomyConfig>(
                    "Assets/Data/SummonEconomyConfig.asset");
            }
#endif

            if (economyConfig == null)
            {
                economyConfig = Resources.Load<SummonEconomyConfig>("SummonEconomyConfig");
            }

            if (gameDatabase == null)
            {
                gameDatabase = Resources.Load<GameDatabase>("GameDatabase");
            }

            if (battleBootstrap == null)
            {
                battleBootstrap = FindAnyObjectByType<GameBattleBootstrap>();
            }
        }

        private void OnDestroy()
        {
            if (_session != null)
            {
                _session.RunEnded -= HandleRunEnded;
            }

            if (_summonManager != null)
            {
                _summonManager.StatusMessage -= HandleStatusMessage;
            }

            if (AppRoot.Instance?.AnalyticsRunBinder != null)
            {
                AppRoot.Instance.AnalyticsRunBinder.BindSummon(null);
            }

            if (gridManager != null)
            {
                gridManager.PassengerSelected -= HandlePassengerSelected;
                gridManager.PassengerDropped -= HandlePassengerDropped;
            }

            if (_runState?.Currency != null)
            {
                _runState.Currency.CoinsChanged -= HandleCoinsChanged;
            }

            if (_runState?.Summon != null)
            {
                _runState.Summon.OffersChanged -= RefreshOfferPanel;
            }

            UnwireButtons();
        }

        private void HandleRunEnded(RunResult _)
        {
            if (summonButton != null) summonButton.interactable = false;

            if (cancelOfferButton != null) cancelOfferButton.interactable = false;
            if (freeRerollButton != null) freeRerollButton.interactable = false;
            if (adRerollButton != null) adRerollButton.interactable = false;

            for (int i = 0; i < offerButtons.Length; i++)
            {
                if (offerButtons[i] != null) offerButtons[i].interactable = false;
            }

            if (offerPanel != null) offerPanel.SetActive(false);
        }

        private void WireButtons()
        {
            if (summonButton != null)
            {
                summonButton.onClick.AddListener(OnSummonClicked);
            }

            if (cancelOfferButton != null)
            {
                cancelOfferButton.onClick.AddListener(OnCancelOfferClicked);
            }

            if (freeRerollButton != null)
            {
                freeRerollButton.onClick.AddListener(OnFreeRerollClicked);
            }

            if (adRerollButton != null)
            {
                adRerollButton.onClick.AddListener(OnAdRerollClicked);
            }

            for (int i = 0; i < offerButtons.Length; i++)
            {
                int index = i;
                if (offerButtons[i] != null)
                {
                    offerButtons[i].onClick.AddListener(() => OnOfferClicked(index));
                }
            }
        }

        private void UnwireButtons()
        {
            if (summonButton != null)
            {
                summonButton.onClick.RemoveListener(OnSummonClicked);
            }

            if (cancelOfferButton != null)
            {
                cancelOfferButton.onClick.RemoveListener(OnCancelOfferClicked);
            }

            if (freeRerollButton != null)
            {
                freeRerollButton.onClick.RemoveListener(OnFreeRerollClicked);
            }

            if (adRerollButton != null)
            {
                adRerollButton.onClick.RemoveListener(OnAdRerollClicked);
            }

            for (int i = 0; i < offerButtons.Length; i++)
            {
                if (offerButtons[i] != null)
                {
                    offerButtons[i].onClick.RemoveAllListeners();
                }
            }
        }

        private void OnSummonClicked()
        {
            if (_summonManager == null)
            {
                return;
            }

            if (Tutorial.TutorialDirector.Instance != null
                && !Tutorial.TutorialDirector.Instance.Allows(Tutorial.TutorialInputMask.Summon))
            {
                GameAudio.PlaySfx(SfxId.UiError);
                return;
            }

            SummonRequestResult result = _summonManager.TryBeginSummon();
            if (result == SummonRequestResult.Success)
            {
                GameAudio.PlaySfx(SfxId.SummonOpen);
                Tutorial.TutorialDirector.Instance?.NotifySummonOpened();
                RefreshOfferPanel();
            }
            else if (result == SummonRequestResult.NotEnoughCoins
                     || result == SummonRequestResult.NoEmptySlot
                     || result == SummonRequestResult.OfferAlreadyOpen)
            {
                GameAudio.PlaySfx(SfxId.UiError);
                Ux.UxGuidanceService.ShowSummonResult(result);
            }

            RefreshHud();
        }

        private void OnOfferClicked(int index)
        {
            if (_summonManager == null)
            {
                return;
            }

            SelectOfferResult result = _summonManager.TrySelectOffer(index, out _);
            if (result == SelectOfferResult.Success)
            {
                GameAudio.PlaySfx(SfxId.SummonSelect);
                gridManager?.RefreshViews();
                SetStatus("승객을 배치했습니다.", ephemeral: true);
                Tutorial.TutorialDirector.Instance?.NotifyPassengerPlaced();
                battleBootstrap?.MissionBinder?.NotifySummoned();
                if (_summonManager != null && _runState != null)
                {
                    // 방금 배치된 승객 ID는 그리드에서 찾는다
                    for (int i = 0; i < RunState.GridSlotCount; i++)
                    {
                        PassengerRuntime p = _runState.GetPassengerAtSlot(i);
                        if (p?.Data != null)
                        {
                            battleBootstrap?.MissionBinder?.NotifyPassengerPlaced(p.Data.Id);
                        }
                    }
                }

                Ux.MergeHighlightService.Refresh(gridManager, _runState);
            }
            else if (result == SelectOfferResult.NoEmptySlot)
            {
                GameAudio.PlaySfx(SfxId.UiError);
                Ux.UxGuidanceService.Show("빈 슬롯이 없습니다.");
            }

            RefreshHud();
            RefreshOfferPanel();
        }

        private void OnCancelOfferClicked()
        {
            _summonManager?.CancelOffers();
            GameAudio.PlaySfx(SfxId.UiCancel);
            RefreshOfferPanel();
            SetStatus("소환을 취소했습니다.", ephemeral: true);
        }

        private void OnFreeRerollClicked()
        {
            if (_summonManager == null)
            {
                return;
            }

            if (_summonManager.TryRerollFree() == RerollResult.Success)
            {
                GameAudio.PlaySfx(SfxId.Switch);
                SetStatus($"무료 리롤 사용 (남은 {_summonManager.RemainingFreeRerolls}회)", ephemeral: true);
            }

            RefreshOfferPanel();
            RefreshHud();
        }

        private void OnAdRerollClicked()
        {
            if (_summonManager == null || !_adInputGuard.TryAcquire())
            {
                return;
            }

            AdCoordinator ads = AppRoot.Instance?.Ads;
            if (ads == null)
            {
                if (_summonManager.TryRerollWithAd() == RerollResult.Success)
                {
                    GameAudio.PlaySfx(SfxId.Switch);
                    SetStatus($"광고 리롤 사용 (남은 {_summonManager.RemainingAdRerolls}회)", ephemeral: true);
                }

                RefreshOfferPanel();
                RefreshHud();
                return;
            }

            if (!_summonManager.HasActiveOffers)
            {
                return;
            }

            if (!ads.CanOfferReroll(RewardedAdPlacement.PassengerReroll)
                || _summonManager.RemainingAdRerolls <= 0)
            {
                SetStatus("광고 리롤을 사용할 수 없습니다.", ephemeral: true);
                return;
            }

            UiInputGuard.SetInteractable(adRerollButton, false);
            ads.ShowRewarded(
                RewardedAdPlacement.PassengerReroll,
                () =>
                {
                    if (_summonManager.ApplyAdReroll(recordUsage: true) == RerollResult.Success)
                    {
                        GameAudio.PlaySfx(SfxId.Switch);
                        SetStatus($"광고 리롤 사용 (남은 {_summonManager.RemainingAdRerolls}회)", ephemeral: true);
                    }

                    RefreshOfferPanel();
                    RefreshHud();
                },
                result =>
                {
                    if (result != AdResult.Completed)
                    {
                        SetStatus($"광고 {(result == AdResult.Cancelled ? "취소" : "실패")} — 보상 없음", ephemeral: true);
                    }

                    RefreshOfferPanel();
                    RefreshHud();
                });
        }

        private void RefreshOfferPanel()
        {
            bool open = _summonManager != null && _summonManager.HasActiveOffers;
            if (offerPanel != null)
            {
                offerPanel.SetActive(open);
            }

            if (!open || _summonManager == null)
            {
                return;
            }

            var offers = _summonManager.CurrentOffers;
            for (int i = 0; i < offerButtons.Length; i++)
            {
                bool has = i < offers.Count && offers[i] != null;
                if (offerButtons[i] != null)
                {
                    offerButtons[i].gameObject.SetActive(has);
                }

                if (offerLabels[i] != null)
                {
                    offerLabels[i].text = has ? offers[i].DisplayName : string.Empty;
                }
            }

            if (freeRerollButton != null)
            {
                freeRerollButton.interactable = _summonManager.RemainingFreeRerolls > 0;
            }

            if (adRerollButton != null)
            {
                AdCoordinator ads = AppRoot.Instance?.Ads;
                adRerollButton.interactable = _summonManager.RemainingAdRerolls > 0
                    && (ads == null || ads.CanOfferReroll(RewardedAdPlacement.PassengerReroll));
            }
        }

        private void RefreshHud()
        {
            if (_runState == null)
            {
                return;
            }

            if (coinLabel != null)
            {
                coinLabel.text = $"코인 {_runState.Currency.CurrentCoins}";
            }

            if (costLabel != null && _summonManager != null)
            {
                costLabel.text = $"소환 {_summonManager.CurrentSummonCost}";
            }
        }

        private void HandleCoinsChanged(int _)
        {
            RefreshHud();
        }

        private void HandlePassengerSelected(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= RunState.GridSlotCount)
            {
                ClearStatus();
                return;
            }

            PassengerRuntime passenger = _runState?.GetPassengerAtSlot(slotIndex);
            if (passenger != null)
            {
                int price = PassengerSellService.GetSellPrice(passenger, _runState);
                SetStatus(
                    $"선택: {passenger.Data.DisplayName} {passenger.StarLevel}★ (판매 {price})",
                    ephemeral: true);
            }
        }

        private void HandlePassengerDropped(int from, int to, GridDropResult result)
        {
            RefreshHud();
        }

        private void HandleStatusMessage(string message)
        {
            SetStatus(message, ephemeral: true);
        }

        private static System.Collections.Generic.List<PassengerData> FilterLiveEventPassengers(
            System.Collections.Generic.List<PassengerData> source,
            RunState runState)
        {
            var filtered = new System.Collections.Generic.List<PassengerData>();
            if (source == null || runState == null)
            {
                return filtered;
            }

            for (int i = 0; i < source.Count; i++)
            {
                PassengerData passenger = source[i];
                if (passenger == null)
                {
                    continue;
                }

                if (runState.IsLiveEventPassengerAllowed(passenger.Id))
                {
                    filtered.Add(passenger);
                }
            }

            return filtered.Count > 0 ? filtered : source;
        }

        private void SetStatus(string message, bool ephemeral = false)
        {
            CancelStatusClear();
            if (statusLabel != null)
            {
                statusLabel.text = message ?? string.Empty;
            }

            if (!ephemeral || string.IsNullOrWhiteSpace(message) || !isActiveAndEnabled)
            {
                return;
            }

            _ephemeralStatusToken = message;
            _statusClearRoutine = StartCoroutine(ClearStatusAfterDelay(message, EphemeralStatusSeconds));
        }

        private void ClearStatus()
        {
            CancelStatusClear();
            if (statusLabel != null)
            {
                statusLabel.text = string.Empty;
            }
        }

        private void CancelStatusClear()
        {
            if (_statusClearRoutine != null)
            {
                StopCoroutine(_statusClearRoutine);
                _statusClearRoutine = null;
            }

            _ephemeralStatusToken = null;
        }

        private IEnumerator ClearStatusAfterDelay(string expected, float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            _statusClearRoutine = null;
            if (statusLabel != null
                && string.Equals(statusLabel.text, expected, StringComparison.Ordinal)
                && string.Equals(_ephemeralStatusToken, expected, StringComparison.Ordinal))
            {
                statusLabel.text = string.Empty;
            }

            _ephemeralStatusToken = null;
        }
    }
}
