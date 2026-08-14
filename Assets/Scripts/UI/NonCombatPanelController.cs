using LastTrain.Ads;
using LastTrain.Battle;
using LastTrain.Core;
using LastTrain.Data;
using LastTrain.Audio;
using LastTrain.Event;
using LastTrain.Run;
using LastTrain.Shop;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.UI
{
    /// <summary>
    /// 상점/이벤트 역 UI. GameBattleBootstrap보다 먼저 Start될 수 있으므로
    /// 서비스·RunState는 Update에서 지연 바인딩한다.
    /// </summary>
    [DefaultExecutionOrder(65)]
    public sealed class NonCombatPanelController : MonoBehaviour
    {
        [SerializeField] private GameBattleBootstrap battleBootstrap;
        [SerializeField] private GameDatabase gameDatabase;

        private GameObject _root;
        private Text _title;
        private Text _body;
        private Button[] _choiceButtons = new Button[4];
        private Text[] _choiceLabels = new Text[4];
        private Button _leaveButton;
        private Button _shopRefreshAdButton;
        private Text _status;
        private readonly UiInputGuard _adInputGuard = new(0.25f);

        private RunState _runState;
        private ShopService _shop;
        private EventService _events;
        private StationManager _stationManager;

        private RunPhase _lastRenderedPhase = RunPhase.None;
        private string _lastRenderedKey = string.Empty;

        public bool IsPanelActive => _root != null && _root.activeSelf;

        public void EnsureBuiltHiddenForTests()
        {
            BuildUiIfNeeded();
        }

        private void Start()
        {
            TryBind();
            BuildUiIfNeeded();
            Hide();
        }

        private void Update()
        {
            TryBind();
            if (_runState == null)
            {
                return;
            }

            BuildUiIfNeeded();
            if (_root == null)
            {
                return;
            }

            RunPhase phase = _runState.Battle.CurrentPhase;
            if (phase == RunPhase.ShopOpen)
            {
                if (_lastRenderedPhase != RunPhase.ShopOpen)
                {
                    GameAudio.PlaySfx(SfxId.UiOpen);
                }

                ShowShop();
            }
            else if (phase == RunPhase.EventOpen)
            {
                if (_lastRenderedPhase != RunPhase.EventOpen)
                {
                    GameAudio.PlaySfx(SfxId.UiOpen);
                }

                ShowEvent();
            }
            else
            {
                if (_root != null && _root.activeSelf)
                {
                    Hide();
                }

                _lastRenderedPhase = phase;
                _lastRenderedKey = string.Empty;
            }
        }

        private void TryBind()
        {
            if (battleBootstrap == null)
            {
                battleBootstrap = FindAnyObjectByType<GameBattleBootstrap>();
            }

            if (_runState == null)
            {
                AppRoot appRoot = AppRoot.Instance;
                if (appRoot != null && appRoot.GameSession != null && appRoot.GameSession.HasActiveRun)
                {
                    _runState = appRoot.GameSession.RunState;
                }
            }

            if (battleBootstrap == null)
            {
                return;
            }

            if (_stationManager == null)
            {
                _stationManager = battleBootstrap.StationManager;
            }

            if ((_shop == null || _events == null) && battleBootstrap.NonCombatServices != null)
            {
                _shop = battleBootstrap.NonCombatServices.Shop;
                _events = battleBootstrap.NonCombatServices.Events;
            }

            if (gameDatabase == null)
            {
                gameDatabase = battleBootstrap != null ? battleBootstrap.GameDatabase : null;
            }

            if (gameDatabase == null)
            {
                gameDatabase = GameDatabaseLocator.Load();
            }
        }

        private void ShowShop()
        {
            if (_shop == null)
            {
                Hide();
                return;
            }

            BuildUiIfNeeded();
            if (_root == null)
            {
                return;
            }

            _root.SetActive(true);
            EnsureKoreanFont();
            Text leaveLabel = _leaveButton.GetComponentInChildren<Text>();
            if (leaveLabel != null)
            {
                leaveLabel.text = "나가기";
            }

            _leaveButton.onClick.RemoveAllListeners();
            _leaveButton.onClick.AddListener(OnLeaveShop);
            _title.text = "상점";
            _body.text = "상품을 구매하거나 나갈 수 있습니다.";
            _leaveButton.gameObject.SetActive(true);
            RefreshShopAdButton();
            _status.text = $"보유 코인: {_runState.Currency.CurrentCoins}";

            var offers = _runState.Shop.Offers;
            if (offers == null || offers.Count == 0)
            {
                _status.text = "상품을 불러오지 못했습니다. 나가기 후 다시 시도하세요.";
                for (int i = 0; i < _choiceButtons.Length; i++)
                {
                    if (_choiceButtons[i] != null)
                    {
                        _choiceButtons[i].gameObject.SetActive(false);
                    }
                }

                _lastRenderedPhase = RunPhase.ShopOpen;
                _lastRenderedKey = "empty";
                return;
            }

            string key = BuildShopRenderKey();
            bool needsRebuild = _lastRenderedPhase != RunPhase.ShopOpen || _lastRenderedKey != key;

            for (int i = 0; i < _choiceButtons.Length; i++)
            {
                bool visible = i < offers.Count;
                _choiceButtons[i].gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                ShopOffer offer = offers[i];
                string label = offer.purchased ? "[구매함]\n" : string.Empty;
                label += $"{DescribeShopItem(offer)}\n{offer.price}코인";
                if (_choiceLabels[i] != null)
                {
                    _choiceLabels[i].text = label;
                    _choiceLabels[i].color = new Color(0.95f, 0.98f, 1f, 1f);
                    ApplyFontTo(_choiceLabels[i]);
                }

                if (needsRebuild)
                {
                    int index = i;
                    _choiceButtons[i].onClick.RemoveAllListeners();
                    _choiceButtons[i].onClick.AddListener(() => OnShopBuy(index));
                }

                _choiceButtons[i].interactable = !offer.purchased;
            }

            _lastRenderedPhase = RunPhase.ShopOpen;
            _lastRenderedKey = key;
        }

        private void RefreshShopAdButton()
        {
            if (_shopRefreshAdButton == null)
            {
                return;
            }

            AdCoordinator ads = AppRoot.Instance?.Ads;
            bool ready = ads != null && ads.IsReady(RewardedAdPlacement.ShopRefresh);
            _shopRefreshAdButton.gameObject.SetActive(ready);
            _shopRefreshAdButton.interactable = ready;
            if (!ready)
            {
                return;
            }

            Text label = _shopRefreshAdButton.GetComponentInChildren<Text>();
            if (label != null)
            {
                int remaining = ads.Limits.GetRemaining(RewardedAdPlacement.ShopRefresh);
                label.text = $"광고 새로고침\n({remaining})";
                ApplyFontTo(label);
            }
        }

        private void OnShopRefreshAdClicked()
        {
            if (_shop == null || !_adInputGuard.TryAcquire())
            {
                return;
            }

            AdCoordinator ads = AppRoot.Instance?.Ads;
            if (ads == null || !ads.IsReady(RewardedAdPlacement.ShopRefresh))
            {
                RefreshShopAdButton();
                return;
            }

            UiInputGuard.SetInteractable(_shopRefreshAdButton, false);
            ads.ShowRewarded(
                RewardedAdPlacement.ShopRefresh,
                () =>
                {
                    if (_shop.TryRefreshOffersFromAd())
                    {
                        GameAudio.PlaySfx(SfxId.Switch);
                        _lastRenderedKey = string.Empty;
                        _status.text = "광고 보상: 상점 상품을 새로 뽑았습니다.";
                    }
                },
                result =>
                {
                    if (result != AdResult.Completed && _status != null)
                    {
                        _status.text = $"광고 {(result == AdResult.Cancelled ? "취소" : "실패")} — 새로고침 없음";
                    }

                    ShowShop();
                });
        }

        private void ShowEvent()
        {
            if (_events == null)
            {
                Hide();
                return;
            }

            EventData eventData = _events.GetCurrentEvent();
            string key = eventData != null
                ? $"{eventData.Id}|{_runState.Events.SelectedChoiceIndex}"
                : $"missing|{_runState.Events.EventId}";
            bool needsRebuild = _lastRenderedPhase != RunPhase.EventOpen || _lastRenderedKey != key;

            _root.SetActive(true);
            EnsureKoreanFont();
            _leaveButton.gameObject.SetActive(false);
            if (_shopRefreshAdButton != null)
            {
                _shopRefreshAdButton.gameObject.SetActive(false);
            }

            if (eventData == null)
            {
                _title.text = "이벤트";
                _body.text = "이벤트 정보를 불러오지 못했습니다. 건너뛰기를 눌러 계속하세요.";
                _status.text = "이벤트를 진행할 수 없습니다.";
                for (int i = 0; i < _choiceButtons.Length; i++)
                {
                    _choiceButtons[i].gameObject.SetActive(false);
                }

                _leaveButton.gameObject.SetActive(true);
                Text leaveLabel = _leaveButton.GetComponentInChildren<Text>();
                if (leaveLabel != null)
                {
                    leaveLabel.text = "건너뛰기";
                }

                _leaveButton.onClick.RemoveAllListeners();
                _leaveButton.onClick.AddListener(OnSkipEvent);
                _lastRenderedPhase = RunPhase.EventOpen;
                _lastRenderedKey = key;
                return;
            }

            _title.text = string.IsNullOrWhiteSpace(eventData.DisplayName) ? "이벤트" : eventData.DisplayName;
            _body.text = string.IsNullOrWhiteSpace(eventData.Description)
                ? "선택지를 고르세요."
                : eventData.Description;
            _status.text = "선택지를 고르세요.";
            _leaveButton.gameObject.SetActive(false);
            _leaveButton.onClick.RemoveAllListeners();
            _leaveButton.onClick.AddListener(OnLeaveShop);

            if (!needsRebuild)
            {
                return;
            }

            EventChoiceData[] choices = eventData.Choices;
            for (int i = 0; i < _choiceButtons.Length; i++)
            {
                bool visible = i < choices.Length && _events.IsChoiceVisible(choices[i]);
                _choiceButtons[i].gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                _choiceLabels[i].text = choices[i].text;
                if (string.IsNullOrWhiteSpace(_choiceLabels[i].text))
                {
                    _choiceLabels[i].text = $"선택 {i + 1}";
                }

                ApplyFontTo(_choiceLabels[i]);
                int index = i;
                _choiceButtons[i].onClick.RemoveAllListeners();
                _choiceButtons[i].onClick.AddListener(() => OnEventChoice(index));
                _choiceButtons[i].interactable = true;
            }

            _lastRenderedPhase = RunPhase.EventOpen;
            _lastRenderedKey = key;
        }

        private string BuildShopRenderKey()
        {
            var offers = _runState.Shop.Offers;
            int hash = offers.Count * 397 ^ _runState.Currency.CurrentCoins;
            for (int i = 0; i < offers.Count; i++)
            {
                ShopOffer offer = offers[i];
                hash = unchecked(
                    hash * 31
                    + (offer.purchased ? 1 : 0)
                    + offer.price
                    + (int)offer.itemType
                    + (offer.payloadId?.GetHashCode() ?? 0)
                    + offer.payloadValue);
            }

            return hash.ToString();
        }

        private string DescribeShopItem(ShopOffer offer)
        {
            if (offer == null)
            {
                return "상품";
            }

            switch (offer.itemType)
            {
                case ShopItemType.RandomPassengerStar1:
                    return "무작위 1성 승객";
                case ShopItemType.SpecificPassenger:
                    return ResolvePassengerName(offer.payloadId);
                case ShopItemType.TrainHeal:
                    return $"객차 회복 +{offer.payloadValue}";
                case ShopItemType.RandomAbility:
                    return "능력 카드";
                case ShopItemType.Relic:
                    return ResolveRelicName(offer.payloadId);
                case ShopItemType.FreeSummonToken:
                    return $"무료 소환권 x{Mathf.Max(1, offer.payloadValue)}";
                case ShopItemType.DuplicatePassenger:
                    return $"{ResolvePassengerName(offer.payloadId)} 복제";
                case ShopItemType.SummonCostReduction:
                    return $"소환 비용 -{Mathf.Max(1, offer.payloadValue)}";
                default:
                    return offer.itemType.ToString();
            }
        }

        private string ResolvePassengerName(string passengerId)
        {
            if (string.IsNullOrWhiteSpace(passengerId) || gameDatabase?.Passengers == null)
            {
                return "특정 승객";
            }

            for (int i = 0; i < gameDatabase.Passengers.Count; i++)
            {
                PassengerData data = gameDatabase.Passengers[i];
                if (data != null && data.Id == passengerId)
                {
                    return string.IsNullOrWhiteSpace(data.DisplayName) ? passengerId : data.DisplayName;
                }
            }

            return passengerId;
        }

        private string ResolveRelicName(string relicId)
        {
            if (string.IsNullOrWhiteSpace(relicId) || gameDatabase?.Relics == null)
            {
                return "유물";
            }

            for (int i = 0; i < gameDatabase.Relics.Count; i++)
            {
                RelicData data = gameDatabase.Relics[i];
                if (data != null && data.Id == relicId)
                {
                    return string.IsNullOrWhiteSpace(data.DisplayName) ? relicId : data.DisplayName;
                }
            }

            return relicId;
        }

        private void OnShopBuy(int index)
        {
            ShopPurchaseResult result = _shop.TryPurchase(index);
            _status.text = result switch
            {
                ShopPurchaseResult.Success => "구매 완료!",
                ShopPurchaseResult.AlreadyPurchased => "이미 구매한 상품입니다.",
                ShopPurchaseResult.NotEnoughCoins => "코인이 부족합니다.",
                _ => "구매할 수 없습니다.",
            };
            if (result == ShopPurchaseResult.Success)
            {
                GameAudio.PlaySfx(SfxId.ShopBuy);
                battleBootstrap?.MissionBinder?.NotifyShopPurchased();
                RefreshGridViews();
            }
            else
            {
                GameAudio.PlaySfx(SfxId.UiError);
            }
            _lastRenderedKey = string.Empty;
            ShowShop();
        }

        private void OnLeaveShop()
        {
            GameAudio.PlaySfx(SfxId.UiCancel);
            _shop?.LeaveShop();
            _stationManager?.TryActivateStation();
            Hide();
            _lastRenderedPhase = RunPhase.None;
            _lastRenderedKey = string.Empty;
        }

        private void OnSkipEvent()
        {
            GameAudio.PlaySfx(SfxId.UiCancel);
            if (_events != null && _events.TrySkipEvent())
            {
                _stationManager?.TryActivateStation();
            }

            Hide();
            _lastRenderedPhase = RunPhase.None;
            _lastRenderedKey = string.Empty;
            Text leaveLabel = _leaveButton != null ? _leaveButton.GetComponentInChildren<Text>() : null;
            if (leaveLabel != null)
            {
                leaveLabel.text = "나가기";
            }

            if (_leaveButton != null)
            {
                _leaveButton.onClick.RemoveAllListeners();
                _leaveButton.onClick.AddListener(OnLeaveShop);
            }
        }

        private void OnEventChoice(int index)
        {
            EventChoiceResult result = _events.TrySelectChoice(index);
            _status.text = result == EventChoiceResult.Success ? "선택 완료!" : "선택할 수 없습니다.";
            if (result == EventChoiceResult.Success)
            {
                GameAudio.PlaySfx(SfxId.UiConfirm);
                GameAudio.PlaySfx(SfxId.Reward);
                RefreshGridViews();
                _stationManager?.TryActivateStation();
                Hide();
                _lastRenderedPhase = RunPhase.None;
                _lastRenderedKey = string.Empty;
            }
            else
            {
                GameAudio.PlaySfx(SfxId.UiError);
            }
        }

        private static void RefreshGridViews()
        {
            FindAnyObjectByType<Grid.GridManager>()?.RefreshViews();
        }

        private void Hide()
        {
            if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        private void BuildUiIfNeeded()
        {
            if (_root != null)
            {
                return;
            }

            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                return;
            }

            VisualTheme theme = VisualThemeLocator.Load();

            _root = new GameObject("NonCombatPanel", typeof(RectTransform), typeof(Image));
            var rect = _root.GetComponent<RectTransform>();
            rect.SetParent(canvas.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.SetAsLastSibling();
            Image rootImage = _root.GetComponent<Image>();
            if (theme?.PopupDim != null)
            {
                rootImage.sprite = theme.PopupDim;
                rootImage.type = Image.Type.Sliced;
                rootImage.color = Color.white;
            }
            else
            {
                rootImage.color = new Color(0f, 0f, 0f, 0.82f);
            }

            rootImage.raycastTarget = true;

            GameObject box = new GameObject("Box", typeof(RectTransform), typeof(Image));
            RectTransform boxRect = box.GetComponent<RectTransform>();
            boxRect.SetParent(_root.transform, false);
            boxRect.anchorMin = new Vector2(0.5f, 0.5f);
            boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.sizeDelta = new Vector2(920f, 1180f);
            Image boxImage = box.GetComponent<Image>();
            boxImage.sprite = null;
            boxImage.color = MenuOverlayUi.OverlayFill;

            _title = CreateText(box.transform, "Title", "상점", 36, new Vector2(0f, 500f));
            _body = CreateText(box.transform, "Body", string.Empty, 24, new Vector2(0f, 400f), new Vector2(860f, 100f));
            _status = CreateText(box.transform, "Status", string.Empty, 22, new Vector2(0f, -500f));

            Vector2[] cardPositions =
            {
                new Vector2(-210f, 160f),
                new Vector2(210f, 160f),
                new Vector2(-210f, -90f),
                new Vector2(210f, -90f),
            };
            for (int i = 0; i < 4; i++)
            {
                _choiceButtons[i] = CreateButton(
                    box.transform,
                    $"Choice{i}",
                    string.Empty,
                    cardPositions[i],
                    theme,
                    useCardFrame: true);
                _choiceButtons[i].GetComponent<RectTransform>().sizeDelta = new Vector2(380f, 220f);
                _choiceButtons[i].gameObject.SetActive(false);
                _choiceLabels[i] = _choiceButtons[i].GetComponentInChildren<Text>();
                if (_choiceLabels[i] != null)
                {
                    _choiceLabels[i].rectTransform.sizeDelta = new Vector2(340f, 200f);
                    _choiceLabels[i].fontSize = 24;
                    _choiceLabels[i].resizeTextForBestFit = false;
                    _choiceLabels[i].horizontalOverflow = HorizontalWrapMode.Wrap;
                    _choiceLabels[i].verticalOverflow = VerticalWrapMode.Overflow;
                    _choiceLabels[i].color = new Color(0.95f, 0.98f, 1f, 1f);
                }
            }

            _leaveButton = CreateButton(box.transform, "LeaveButton", "나가기", new Vector2(-160f, -380f), theme, useCardFrame: false);
            _leaveButton.GetComponent<RectTransform>().sizeDelta = new Vector2(280f, 88f);
            _leaveButton.onClick.AddListener(OnLeaveShop);

            _shopRefreshAdButton = CreateButton(
                box.transform,
                "ShopRefreshAdButton",
                "광고 새로고침",
                new Vector2(160f, -380f),
                theme,
                useCardFrame: false);
            _shopRefreshAdButton.GetComponent<RectTransform>().sizeDelta = new Vector2(280f, 88f);
            _shopRefreshAdButton.onClick.AddListener(OnShopRefreshAdClicked);
            _shopRefreshAdButton.gameObject.SetActive(false);

            EnsureKoreanFont();
            _root.SetActive(false);
        }

        private void EnsureKoreanFont()
        {
            if (_root != null)
            {
                GameFontProvider.ApplyTo(_root);
            }

            ApplyFontTo(_title);
            ApplyFontTo(_body);
            ApplyFontTo(_status);
            if (_choiceLabels != null)
            {
                for (int i = 0; i < _choiceLabels.Length; i++)
                {
                    ApplyFontTo(_choiceLabels[i]);
                }
            }

            if (_leaveButton != null)
            {
                ApplyFontTo(_leaveButton.GetComponentInChildren<Text>());
            }

            if (_shopRefreshAdButton != null)
            {
                ApplyFontTo(_shopRefreshAdButton.GetComponentInChildren<Text>());
            }
        }

        private static void ApplyFontTo(Text text)
        {
            if (text == null)
            {
                return;
            }

            Font font = GameFontProvider.Get();
            if (font != null)
            {
                text.font = font;
            }

            // BestFit + 기본 영문 폰트 조합에서 한글이 ???로 깨지는 경우가 있다.
            text.resizeTextForBestFit = false;
        }

        private static Text CreateText(Transform parent, string name, string value, int size, Vector2 pos, Vector2? box = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchoredPosition = pos;
            rect.sizeDelta = box ?? new Vector2(800f, 50f);
            Text text = go.GetComponent<Text>();
            text.font = GameFontProvider.Get();
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = value;
            text.raycastTarget = false;
            text.resizeTextForBestFit = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            string label,
            Vector2 pos,
            VisualTheme theme,
            bool useCardFrame)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(200f, 220f);
            Image image = go.GetComponent<Image>();
            if (useCardFrame && theme?.CardFrame != null)
            {
                image.sprite = theme.CardFrame;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }
            else if (!useCardFrame)
            {
                UiButtonStyler.ApplyStandardTheme(go.GetComponent<Button>());
            }
            else
            {
                image.color = new Color(0.2f, 0.25f, 0.35f, 0.95f);
            }

            Text text = CreateText(go.transform, "Label", label, 22, Vector2.zero, new Vector2(180f, 200f));
            text.resizeTextForBestFit = false;
            return go.GetComponent<Button>();
        }
    }
}
