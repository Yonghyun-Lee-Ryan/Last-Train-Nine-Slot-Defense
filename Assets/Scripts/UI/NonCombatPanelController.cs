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
        private Text _status;

        private RunState _runState;
        private ShopService _shop;
        private EventService _events;
        private StationManager _stationManager;

        private RunPhase _lastRenderedPhase = RunPhase.None;
        private string _lastRenderedKey = string.Empty;

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
            else if (_lastRenderedPhase == RunPhase.ShopOpen || _lastRenderedPhase == RunPhase.EventOpen)
            {
                Hide();
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
                gameDatabase = battleBootstrap.GameDatabase;
            }
        }

        private void ShowShop()
        {
            if (_shop == null)
            {
                return;
            }

            string key = BuildShopRenderKey();
            bool needsRebuild = _lastRenderedPhase != RunPhase.ShopOpen || _lastRenderedKey != key;

            _root.SetActive(true);
            _title.text = "상점";
            _body.text = "상품을 구매하거나 나갈 수 있습니다.";
            _leaveButton.gameObject.SetActive(true);
            _status.text = $"보유 코인: {_runState.Currency.CurrentCoins}";

            if (!needsRebuild)
            {
                return;
            }

            for (int i = 0; i < _choiceButtons.Length; i++)
            {
                bool visible = i < _runState.Shop.Offers.Count;
                _choiceButtons[i].gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                ShopOffer offer = _runState.Shop.Offers[i];
                string label = offer.purchased ? "[구매함] " : string.Empty;
                label += $"{DescribeShopItem(offer)} - {offer.price}코인";
                _choiceLabels[i].text = label;
                int index = i;
                _choiceButtons[i].onClick.RemoveAllListeners();
                _choiceButtons[i].onClick.AddListener(() => OnShopBuy(index));
                _choiceButtons[i].interactable = !offer.purchased;
            }

            _lastRenderedPhase = RunPhase.ShopOpen;
            _lastRenderedKey = key;
        }

        private void ShowEvent()
        {
            if (_events == null)
            {
                return;
            }

            EventData eventData = _events.GetCurrentEvent();
            string key = eventData != null
                ? $"{eventData.Id}|{_runState.Events.SelectedChoiceIndex}"
                : $"missing|{_runState.Events.EventId}";
            bool needsRebuild = _lastRenderedPhase != RunPhase.EventOpen || _lastRenderedKey != key;

            _root.SetActive(true);
            _leaveButton.gameObject.SetActive(false);

            if (eventData == null)
            {
                _title.text = "이벤트";
                _body.text = "이벤트 데이터를 찾을 수 없습니다. Unit 27 빌더 실행 후 GameDatabase 등록을 확인하세요.";
                _status.text = "선택지를 표시할 수 없습니다.";
                for (int i = 0; i < _choiceButtons.Length; i++)
                {
                    _choiceButtons[i].gameObject.SetActive(false);
                }

                _lastRenderedPhase = RunPhase.EventOpen;
                _lastRenderedKey = key;
                return;
            }

            _title.text = eventData.DisplayName;
            _body.text = eventData.Description;
            _status.text = "선택지를 고르세요.";

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
                hash = unchecked(hash * 31 + (offers[i].purchased ? 1 : 0) + offers[i].price);
            }

            return hash.ToString();
        }

        private static string DescribeShopItem(ShopOffer offer)
        {
            return offer.itemType switch
            {
                ShopItemType.RandomPassengerStar1 => "무작위 1성 승객",
                ShopItemType.SpecificPassenger => "특정 승객",
                ShopItemType.TrainHeal => "객차 회복",
                ShopItemType.RandomAbility => "능력 카드",
                ShopItemType.Relic => "유물",
                ShopItemType.FreeSummonToken => "무료 소환권",
                ShopItemType.DuplicatePassenger => "승객 복제",
                ShopItemType.SummonCostReduction => "소환 비용 감소",
                _ => offer.itemType.ToString(),
            };
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
            boxRect.sizeDelta = new Vector2(960f, 1100f);
            Image boxImage = box.GetComponent<Image>();
            if (theme?.Panel != null)
            {
                boxImage.sprite = theme.Panel;
                boxImage.type = Image.Type.Sliced;
                boxImage.color = Color.white;
            }
            else
            {
                boxImage.color = new Color(0.1f, 0.14f, 0.2f, 0.96f);
            }

            _title = CreateText(box.transform, "Title", "상점", 36, new Vector2(0f, 460f));
            _body = CreateText(box.transform, "Body", string.Empty, 24, new Vector2(0f, 360f), new Vector2(860f, 100f));
            _status = CreateText(box.transform, "Status", string.Empty, 22, new Vector2(0f, -460f));

            float[] xs = { -300f, -100f, 100f, 300f };
            for (int i = 0; i < 4; i++)
            {
                _choiceButtons[i] = CreateButton(box.transform, $"Choice{i}", "선택", new Vector2(xs[i], 40f), theme, useCardFrame: true);
                _choiceLabels[i] = _choiceButtons[i].GetComponentInChildren<Text>();
            }

            _leaveButton = CreateButton(box.transform, "LeaveButton", "나가기", new Vector2(0f, -340f), theme, useCardFrame: false);
            _leaveButton.GetComponent<RectTransform>().sizeDelta = new Vector2(280f, 88f);
            _leaveButton.onClick.AddListener(OnLeaveShop);
        }

        private static Text CreateText(Transform parent, string name, string value, int size, Vector2 pos, Vector2? box = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchoredPosition = pos;
            rect.sizeDelta = box ?? new Vector2(800f, 50f);
            Text text = go.GetComponent<Text>();
            text.font = GameFontProvider.Get() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = value;
            text.raycastTarget = false;
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

            Text text = CreateText(go.transform, "Label", label, 20, Vector2.zero, new Vector2(180f, 200f));
            text.resizeTextForBestFit = true;
            return go.GetComponent<Button>();
        }
    }
}
