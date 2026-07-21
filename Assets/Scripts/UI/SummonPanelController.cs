using System;
using LastTrain.Core;
using LastTrain.Data;
using LastTrain.Grid;
using LastTrain.Passenger;
using LastTrain.Run;
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
        [Header("References")]
        [SerializeField] private GridManager gridManager;
        [SerializeField] private GameDatabase gameDatabase;
        [SerializeField] private SummonEconomyConfig economyConfig;
        [SerializeField] private int randomSeed;

        [Header("HUD")]
        [SerializeField] private Text coinLabel;
        [SerializeField] private Text costLabel;
        [SerializeField] private Text statusLabel;
        [SerializeField] private Button summonButton;

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

            int seed = randomSeed != 0 ? randomSeed : unchecked(Environment.TickCount);
            var random = new RandomService(seed);
            var offerService = new PassengerOfferService(
                gameDatabase.Passengers,
                random,
                economyConfig.OfferCount);

            _summonManager = new SummonManager(_runState, economyConfig, offerService);
            _summonManager.StatusMessage += HandleStatusMessage;

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
            SetStatus("소환 준비 완료");
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

            SummonRequestResult result = _summonManager.TryBeginSummon();
            if (result == SummonRequestResult.Success)
            {
                RefreshOfferPanel();
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
                gridManager?.RefreshViews();
                SetStatus("승객을 배치했습니다.");
            }

            RefreshHud();
            RefreshOfferPanel();
        }

        private void OnCancelOfferClicked()
        {
            _summonManager?.CancelOffers();
            RefreshOfferPanel();
            SetStatus("소환을 취소했습니다.");
        }

        private void OnFreeRerollClicked()
        {
            if (_summonManager == null)
            {
                return;
            }

            if (_summonManager.TryRerollFree() == RerollResult.Success)
            {
                SetStatus($"무료 리롤 사용 (남은 {_summonManager.RemainingFreeRerolls}회)");
            }

            RefreshOfferPanel();
            RefreshHud();
        }

        private void OnAdRerollClicked()
        {
            if (_summonManager == null)
            {
                return;
            }

            if (_summonManager.TryRerollWithAd() == RerollResult.Success)
            {
                SetStatus($"광고 리롤 사용 (남은 {_summonManager.RemainingAdRerolls}회)");
            }

            RefreshOfferPanel();
            RefreshHud();
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
                adRerollButton.interactable = _summonManager.RemainingAdRerolls > 0;
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
                SetStatus("선택 해제");
                return;
            }

            PassengerRuntime passenger = _runState?.GetPassengerAtSlot(slotIndex);
            if (passenger != null)
            {
                int price = PassengerSellService.GetSellPrice(passenger, _runState);
                SetStatus($"선택: {passenger.Data.DisplayName} {passenger.StarLevel}★ (판매 {price})");
            }
        }

        private void HandlePassengerDropped(int from, int to, GridDropResult result)
        {
            RefreshHud();
        }

        private void HandleStatusMessage(string message)
        {
            SetStatus(message);
        }

        private void SetStatus(string message)
        {
            if (statusLabel != null)
            {
                statusLabel.text = message ?? string.Empty;
            }
        }
    }
}
