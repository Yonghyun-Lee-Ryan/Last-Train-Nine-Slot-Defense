using System;
using System.Text;
using LastTrain.Ability;
using LastTrain.Ads;
using LastTrain.Audio;
using LastTrain.Core;
using LastTrain.Data;
using LastTrain.Run;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LastTrain.UI
{
    /// <summary>역 완료 후 능력 카드 선택 UI. 데이터와 분리된 View.</summary>
    [DefaultExecutionOrder(60)]
    public class AbilityPanelController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameBattleBootstrap battleBootstrap;
        [SerializeField] private GameDatabase gameDatabase;
        [SerializeField] private int randomSeed;

        [Header("Panel")]
        [SerializeField] private GameObject root;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text statusLabel;
        [SerializeField] private Text ownedListLabel;
        [SerializeField] private Button[] offerButtons = new Button[3];
        [SerializeField] private Text[] offerLabels = new Text[3];
        [SerializeField] private Text[] offerDetailLabels = new Text[3];
        [SerializeField] private Button freeRerollButton;
        [SerializeField] private Button adRerollButton;

        private AbilityManager _abilityManager;
        private RunState _runState;
        private AdCoordinator _adsSubscription;

        public AbilityManager AbilityManager => _abilityManager;

        /// <summary>런타임 UI 빌더·에디터 SceneBuilder에서 참조를 주입한다.</summary>
        public void Configure(
            GameBattleBootstrap bootstrapRef,
            GameDatabase databaseRef,
            GameObject rootObject,
            Text title,
            Text status,
            Text owned,
            Button[] offers,
            Text[] labels,
            Text[] details,
            Button freeReroll,
            Button adReroll)
        {
            battleBootstrap = bootstrapRef;
            gameDatabase = databaseRef;
            root = rootObject;
            titleLabel = title;
            statusLabel = status;
            ownedListLabel = owned;
            offerButtons = offers ?? offerButtons;
            offerLabels = labels ?? offerLabels;
            offerDetailLabels = details ?? offerDetailLabels;
            freeRerollButton = freeReroll;
            adRerollButton = adReroll;
        }

        private void Start()
        {
            AppRoot appRoot = AppRoot.Instance;
            if (appRoot == null || !appRoot.GameSession.HasActiveRun)
            {
                Debug.LogWarning("[AbilityPanelController] 활성 RunState가 없습니다.", this);
                return;
            }

            EnsureReferences();
            if (gameDatabase == null)
            {
                Debug.LogError("[AbilityPanelController] gameDatabase가 없습니다.", this);
                return;
            }

            _runState = appRoot.GameSession.RunState;
            int seed = randomSeed != 0 ? randomSeed : unchecked(Environment.TickCount);
            var random = new RandomService(seed);
            var offerService = new AbilityOfferService(gameDatabase.Abilities, random, offerCount: 3);

            _abilityManager = new AbilityManager(
                _runState,
                offerService,
                _runState.BaseTrainMaxHp,
                onRewardFinished: HandleRewardFinished);

            _abilityManager.StatusMessage += HandleStatusMessage;
            AppRoot.Instance?.AnalyticsRunBinder?.BindAbility(_abilityManager);
            _runState.Abilities.OffersChanged += RefreshOfferPanel;
            _runState.Abilities.SelectionChanged += RefreshOwnedList;

            if (battleBootstrap != null)
            {
                battleBootstrap.RegisterAbilityPanel(this);
            }

            CenterButtonLabel(freeRerollButton);
            CenterButtonLabel(adRerollButton);
            HidePanel();
            RefreshOwnedList();
        }

        private void OnDestroy()
        {
            if (_abilityManager != null)
            {
                _abilityManager.StatusMessage -= HandleStatusMessage;
            }

            AppRoot.Instance?.AnalyticsRunBinder?.BindAbility(null);

            UnsubscribeAds();

            if (_runState?.Abilities != null)
            {
                _runState.Abilities.OffersChanged -= RefreshOfferPanel;
                _runState.Abilities.SelectionChanged -= RefreshOwnedList;
            }

            UnwireButtons();
        }

        public void OpenRewardSelection()
        {
            if (_abilityManager == null)
            {
                return;
            }

            AbilityOfferResult result = _abilityManager.TryBeginRewardSelection();
            if (result == AbilityOfferResult.Success)
            {
                GameAudio.PlaySfx(SfxId.Reward);
                ShowPanel();
                SubscribeAds();
                RefreshOfferPanel();
                SetStatus("능력 카드 3장 중 하나를 선택하세요.");
            }
            else if (result == AbilityOfferResult.NoEligibleAbilities)
            {
                HidePanel();
            }
        }

        private void EnsureReferences()
        {
            if (battleBootstrap == null)
            {
                battleBootstrap = FindAnyObjectByType<GameBattleBootstrap>();
            }

            if (gameDatabase == null && battleBootstrap != null)
            {
                gameDatabase = battleBootstrap.GameDatabase;
            }

#if UNITY_EDITOR
            if (gameDatabase == null)
            {
                gameDatabase = AssetDatabase.LoadAssetAtPath<GameDatabase>("Assets/Data/GameDatabase.asset");
            }
#endif

            if (root == null)
            {
                root = gameObject;
            }

            WireButtons();
        }

        private void WireButtons()
        {
            for (int i = 0; i < offerButtons.Length; i++)
            {
                int index = i;
                if (offerButtons[i] != null)
                {
                    offerButtons[i].onClick.RemoveAllListeners();
                    offerButtons[i].onClick.AddListener(() => OnOfferClicked(index));
                }
            }

            if (freeRerollButton != null)
            {
                freeRerollButton.onClick.RemoveAllListeners();
                freeRerollButton.onClick.AddListener(OnFreeRerollClicked);
            }

            if (adRerollButton != null)
            {
                adRerollButton.onClick.RemoveAllListeners();
                adRerollButton.onClick.AddListener(OnAdRerollClicked);
            }
        }

        private void UnwireButtons()
        {
            for (int i = 0; i < offerButtons.Length; i++)
            {
                if (offerButtons[i] != null)
                {
                    offerButtons[i].onClick.RemoveAllListeners();
                }
            }

            if (freeRerollButton != null)
            {
                freeRerollButton.onClick.RemoveAllListeners();
            }

            if (adRerollButton != null)
            {
                adRerollButton.onClick.RemoveAllListeners();
            }
        }

        private void OnOfferClicked(int index)
        {
            if (_abilityManager == null)
            {
                return;
            }

            if (_abilityManager.TrySelectOffer(index) == AbilitySelectResult.Success)
            {
                GameAudio.PlaySfx(SfxId.UiConfirm);
                HidePanel();
                RefreshOwnedList();
            }
        }

        private void OnFreeRerollClicked()
        {
            if (_abilityManager?.TryRerollFree() == AbilityRerollResult.Success)
            {
                GameAudio.PlaySfx(SfxId.Switch);
                SetStatus($"무료 리롤 사용 (남은 {_abilityManager.RemainingFreeRerolls}회)");
            }

            RefreshRerollButtons();
            RefreshOfferCards();
        }

        private void OnAdRerollClicked()
        {
            if (_abilityManager == null)
            {
                return;
            }

            AdCoordinator ads = AppRoot.Instance?.Ads;
            if (ads == null)
            {
                _abilityManager.TryRerollWithAd();
                RefreshOfferPanel();
                return;
            }

            if (!_abilityManager.HasActiveOffers
                || _abilityManager.RemainingAdRerolls <= 0
                || !ads.CanOfferReroll(RewardedAdPlacement.AbilityReroll))
            {
                return;
            }

            UiInputGuard.SetInteractable(adRerollButton, false);
            ads.ShowRewarded(
                RewardedAdPlacement.AbilityReroll,
                () =>
                {
                    if (_abilityManager.ApplyAdReroll(recordUsage: true) == AbilityRerollResult.Success)
                    {
                        GameAudio.PlaySfx(SfxId.Switch);
                        SetStatus($"광고 리롤 사용 (남은 {_abilityManager.RemainingAdRerolls}회)");
                    }

                    RefreshRerollButtons();
                    RefreshOfferCards();
                },
                result =>
                {
                    if (result != AdResult.Completed)
                    {
                        SetStatus($"광고 {(result == AdResult.Cancelled ? "취소" : "실패")} — 보상 없음");
                    }

                    RefreshRerollButtons();
                    RefreshOfferCards();
                });
        }

        private void HandleRewardFinished()
        {
            UnsubscribeAds();
            battleBootstrap?.StationManager?.ContinueAfterAbilityReward();
            HidePanel();
        }

        private void SubscribeAds()
        {
            UnsubscribeAds();
            _adsSubscription = AppRoot.Instance?.Ads;
            if (_adsSubscription != null)
            {
                _adsSubscription.RewardedShowFinished += HandleRewardedShowFinished;
            }
        }

        private void UnsubscribeAds()
        {
            if (_adsSubscription != null)
            {
                _adsSubscription.RewardedShowFinished -= HandleRewardedShowFinished;
                _adsSubscription = null;
            }
        }

        private void HandleRewardedShowFinished(AdResult _)
        {
            if (root == null || !root.activeInHierarchy)
            {
                return;
            }

            RefreshRerollButtons();
        }

        private void RefreshOfferPanel()
        {
            RefreshOfferCards();
            RefreshRerollButtons();
        }

        private void RefreshOfferCards()
        {
            if (_runState?.Abilities == null)
            {
                return;
            }

            var offers = _runState.Abilities.CurrentOffers;
            for (int i = 0; i < offerButtons.Length; i++)
            {
                bool hasOffer = i < offers.Count && offers[i] != null;
                if (offerButtons[i] != null)
                {
                    offerButtons[i].gameObject.SetActive(hasOffer);
                }

                if (!hasOffer)
                {
                    continue;
                }

                AbilityData ability = offers[i];
                if (offerLabels != null && i < offerLabels.Length && offerLabels[i] != null)
                {
                    offerLabels[i].text = $"{ability.DisplayName}\n[{ability.Rarity}]";
                }

                if (offerDetailLabels != null && i < offerDetailLabels.Length && offerDetailLabels[i] != null)
                {
                    offerDetailLabels[i].text = ability.Description;
                }
            }
        }

        private void RefreshRerollButtons()
        {
            if (freeRerollButton != null)
            {
                freeRerollButton.interactable = _abilityManager != null && _abilityManager.RemainingFreeRerolls > 0;
            }

            if (adRerollButton != null)
            {
                AdCoordinator ads = AppRoot.Instance?.Ads;
                adRerollButton.interactable = _abilityManager != null
                    && _abilityManager.RemainingAdRerolls > 0
                    && (ads == null || ads.CanOfferReroll(RewardedAdPlacement.AbilityReroll));
            }
        }

        private void RefreshOwnedList()
        {
            if (ownedListLabel == null || _runState?.Abilities == null)
            {
                return;
            }

            var selected = _runState.Abilities.Selected;
            if (selected.Count == 0)
            {
                ownedListLabel.text = "보유 능력: 없음";
                return;
            }

            var sb = new StringBuilder("보유 능력: ");
            for (int i = 0; i < selected.Count; i++)
            {
                AbilityData ability = selected[i];
                int stacks = _runState.Abilities.GetStackCount(ability.Id);
                if (i > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(ability.DisplayName);
                if (stacks > 1)
                {
                    sb.Append('x').Append(stacks);
                }
            }

            ownedListLabel.text = sb.ToString();
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

        private static void CenterButtonLabel(Button button)
        {
            Text label = button != null ? button.GetComponentInChildren<Text>() : null;
            if (label == null)
            {
                return;
            }

            RectTransform rect = label.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            label.alignment = TextAnchor.MiddleCenter;
        }

        private void ShowPanel()
        {
            if (root != null)
            {
                root.SetActive(true);
            }

            if (titleLabel != null)
            {
                titleLabel.text = "능력 카드 선택";
            }
        }

        private void HidePanel()
        {
            UnsubscribeAds();
            if (root != null)
            {
                root.SetActive(false);
            }
        }
    }
}
