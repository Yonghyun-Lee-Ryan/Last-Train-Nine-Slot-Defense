using System;
using System.Collections.Generic;
using LastTrain.Analytics;
using LastTrain.Audio;
using LastTrain.Core;
using LastTrain.Difficulty;
using LastTrain.Run;
using LastTrain.Data;
using LastTrain.Save;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.UI
{
    /// <summary>
    /// MainMenu Scene의 임시 컨트롤러.
    /// 게임 시작 버튼을 눌러 Game Scene으로 이동한다.
    /// 개발 단위 1 범위의 최소 구현이며, 이후 메뉴 UI로 대체된다.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        /// <summary>계정 레벨·승차권 조각·해금 진행률 표시용.</summary>
        public event Action<MetaProgressSnapshot> MetaProgressUpdated;

        [SerializeField] private Button startButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Text metaStatusLabel;

        private SettingsPanelController _settingsPanel;
        private PrivacyConsentDialogController _privacyDialog;
        private DifficultySelectionController _difficultySelection;
        private DifficultyUnlockPopupController _difficultyUnlockPopup;

        private void Awake()
        {
            if (startButton == null)
            {
                Debug.LogError("[MainMenuController] startButton이 연결되지 않았습니다.", this);
                return;
            }

            startButton.onClick.AddListener(OnStartClicked);

            EnsureContinueButton();
            EnsureMetaStatusLabel();
            RefreshMetaProgress();
        }

        private void Start()
        {
            EnsureMenuServices();
            EnsureDifficultyServices();
            _privacyDialog?.TryShowIfNeeded();
            _difficultyUnlockPopup?.TryShowPendingUnlocks();

            Canvas canvas = FindAnyObjectByType<Canvas>();
            Transform safeArea = canvas != null ? canvas.transform.Find("SafeArea") : null;
            ApplyMenuVisualTheme(safeArea);
            MainMenuUiLayout.Apply(safeArea);
            RebindMetaStatusLabel();
            RefreshMetaProgress();
        }

        private void RebindMetaStatusLabel()
        {
            GameObject metaGo = GameObject.Find("MetaStatusLabel");
            if (metaGo == null)
            {
                return;
            }

            Text resolved = MainMenuUiLayout.ResolveMetaStatusText(metaGo.transform);
            if (resolved != null)
            {
                metaStatusLabel = resolved;
            }
        }

        private static void ApplyMenuVisualTheme(Transform safeArea)
        {
            if (safeArea == null)
            {
                return;
            }

            VisualTheme theme = VisualThemeLocator.Load();
            if (theme == null)
            {
                return;
            }

            Transform background = safeArea.Find("MainMenuBackground");
            if (background == null)
            {
                var bgGo = new GameObject("MainMenuBackground", typeof(RectTransform), typeof(Image));
                background = bgGo.transform;
                background.SetParent(safeArea, false);
                background.SetAsFirstSibling();
                RectTransform bgRect = bgGo.GetComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.offsetMin = Vector2.zero;
                bgRect.offsetMax = Vector2.zero;
            }

            Image bgImage = background.GetComponent<Image>();
            if (bgImage != null && theme.MainMenuBackground != null)
            {
                bgImage.sprite = theme.MainMenuBackground;
                bgImage.type = Image.Type.Simple;
                bgImage.preserveAspect = false;
                bgImage.color = new Color(1f, 1f, 1f, 0.55f);
                bgImage.raycastTarget = false;
            }

            Transform titleArt = safeArea.Find("TitleArtwork");
            if (titleArt == null)
            {
                var artGo = new GameObject("TitleArtwork", typeof(RectTransform), typeof(Image));
                titleArt = artGo.transform;
                titleArt.SetParent(safeArea, false);
            }

            Image titleImage = titleArt.GetComponent<Image>();
            if (titleImage != null && theme.MainMenuTitle != null)
            {
                titleImage.sprite = theme.MainMenuTitle;
                titleImage.preserveAspect = true;
                titleImage.color = Color.white;
                titleImage.raycastTarget = false;
            }
        }

        private void EnsureMenuServices()
        {
            _settingsPanel = GetComponent<SettingsPanelController>();
            if (_settingsPanel == null)
            {
                _settingsPanel = gameObject.AddComponent<SettingsPanelController>();
            }

            _privacyDialog = GetComponent<PrivacyConsentDialogController>();
            if (_privacyDialog == null)
            {
                _privacyDialog = gameObject.AddComponent<PrivacyConsentDialogController>();
            }

            EnsureSettingsButton();
        }

        private void EnsureDifficultyServices()
        {
            _difficultySelection = GetComponent<DifficultySelectionController>();
            if (_difficultySelection == null)
            {
                _difficultySelection = gameObject.AddComponent<DifficultySelectionController>();
            }

            _difficultyUnlockPopup = GetComponent<DifficultyUnlockPopupController>();
            if (_difficultyUnlockPopup == null)
            {
                _difficultyUnlockPopup = gameObject.AddComponent<DifficultyUnlockPopupController>();
            }

            if (GetComponent<MainMenuLayoutController>() == null)
            {
                gameObject.AddComponent<MainMenuLayoutController>();
            }

            RefreshDifficultySelection();
        }

        private void RefreshDifficultySelection()
        {
            if (RunSaveSystem.TryLoadPreparing(out RunSaveData save) && save != null)
            {
                DifficultySelectionState.LockToContinueSave(save.difficultyId);
            }
            else
            {
                DifficultySelectionState.UnlockSelection();
            }

            _difficultySelection?.Refresh();
        }

        private void EnsureSettingsButton()
        {
            if (GameObject.Find("SettingsButton") != null)
            {
                return;
            }

            Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                return;
            }

            Transform safeArea = canvas.transform.Find("SafeArea");
            Transform parent = safeArea != null ? safeArea : canvas.transform;

            Button settingsButton = MenuOverlayUi.CreateButton(
                parent,
                "SettingsButton",
                "설정",
                new Vector2(-20f, -20f),
                new Vector2(168f, 68f),
                () => _settingsPanel?.Show());

            RectTransform rect = settingsButton.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-20f, -20f);
            ApplySettingsButtonTheme(settingsButton);
        }

        private static void ApplySettingsButtonTheme(Button button)
        {
            if (button == null)
            {
                return;
            }

            VisualTheme theme = VisualThemeLocator.Load();
            Image image = button.GetComponent<Image>();
            if (theme == null || image == null || theme.ButtonNormal == null)
            {
                return;
            }

            image.sprite = theme.ButtonNormal;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
        }

        private void EnsureMetaStatusLabel()
        {
            if (metaStatusLabel != null)
            {
                return;
            }

            Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                return;
            }

            Transform safeArea = canvas.transform.Find("SafeArea");
            Transform parent = safeArea != null ? safeArea : canvas.transform;

            GameObject go = new GameObject("MetaStatusLabel", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(900f, 96f);
            rect.anchoredPosition = Vector2.zero;

            metaStatusLabel = go.AddComponent<Text>();
            metaStatusLabel.alignment = TextAnchor.MiddleCenter;
            metaStatusLabel.fontSize = 26;
            metaStatusLabel.color = Color.white;
            metaStatusLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            metaStatusLabel.verticalOverflow = VerticalWrapMode.Overflow;

            Font font = GameFontProvider.Get();
            if (font != null)
            {
                metaStatusLabel.font = font;
            }
        }

        private void OnDestroy()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(OnStartClicked);
            }

            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(OnContinueClicked);
            }
        }

        public void RefreshMetaProgress()
        {
            MetaProgressSnapshot snapshot = MetaSaveSystem.GetSnapshot();
            MetaProgressUpdated?.Invoke(snapshot);

            if (metaStatusLabel != null)
            {
                GameDatabase db = GameDatabaseLocator.Load();
                int totalPassengers = db?.Passengers?.Count ?? 0;
                float unlock01 = snapshot.GetUnlockProgress01(totalPassengers);
                metaStatusLabel.text =
                    $"계정 Lv.{snapshot.AccountLevel}  |  승차권 조각 {snapshot.TicketFragments}\n" +
                    $"해금 {snapshot.UnlockedPassengerCount}/{Math.Max(totalPassengers, snapshot.UnlockedPassengerCount)}" +
                    $" ({unlock01 * 100f:0}%)";
            }

            if (snapshot.PendingNewDiscoveryIds != null && snapshot.PendingNewDiscoveryIds.Count > 0)
            {
                MetaSaveSystem.ClearPendingDiscoveries();
            }

            RefreshDifficultySelection();
        }

        private void EnsureContinueButton()
        {
            if (continueButton != null)
            {
                ApplyContinueButtonTheme();
                continueButton.onClick.RemoveListener(OnContinueClicked);
                continueButton.onClick.AddListener(OnContinueClicked);
                RefreshContinueButton();
                return;
            }

            // 기존 씬에 버튼이 이미 존재하면 그걸 사용
            var found = GameObject.Find("ContinueButton");
            if (found != null)
            {
                continueButton = found.GetComponent<Button>();
                if (continueButton != null)
                {
                    ApplyContinueButtonTheme();
                    continueButton.onClick.AddListener(OnContinueClicked);
                    RefreshContinueButton();
                    return;
                }
            }

            // 없으면 SafeArea 아래에 최소 UI를 런타임 생성
            Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                return;
            }

            Transform safeArea = canvas.transform.Find("SafeArea");
            Transform parent = safeArea != null ? safeArea : canvas.transform;

            GameObject go = new GameObject("ContinueButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(600, 160);
            rect.anchoredPosition = Vector2.zero;

            Image img = go.GetComponent<Image>();
            img.color = new Color(0.20f, 0.45f, 0.85f);

            continueButton = go.GetComponent<Button>();

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text text = textGo.AddComponent<Text>();
            text.text = "이어하기";
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 44;
            text.color = Color.white;

            Font font = GameFontProvider.Get();
            if (font != null)
            {
                text.font = font;
            }

            ApplyContinueButtonTheme();
            continueButton.onClick.AddListener(OnContinueClicked);
            RefreshContinueButton();
        }

        private void ApplyContinueButtonTheme()
        {
            if (continueButton == null)
            {
                return;
            }

            VisualTheme theme = VisualThemeLocator.Load();
            Image image = continueButton.GetComponent<Image>();
            if (theme == null || image == null || theme.ButtonNormal == null)
            {
                return;
            }

            image.sprite = theme.ButtonNormal;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            continueButton.transition = Selectable.Transition.SpriteSwap;

            SpriteState state = continueButton.spriteState;
            state.highlightedSprite = theme.ButtonNormal;
            state.pressedSprite = theme.ButtonPressed != null
                ? theme.ButtonPressed
                : theme.ButtonNormal;
            state.disabledSprite = theme.ButtonDisabled != null
                ? theme.ButtonDisabled
                : theme.ButtonNormal;
            continueButton.spriteState = state;
        }

        private void OnStartClicked()
        {
            startButton.interactable = false;
            GameAudio.PlaySfx(SfxId.UiConfirm);

            // 새 게임 시작 시 이어하기 저장은 제거한다.
            RunSaveSystem.DeleteRunSave();
            DifficultySelectionState.UnlockSelection();

            // 새 회차로 시작한다. 이전 씬의 RunState가 남아 있어도 덮어쓴다.
            AppRoot appRoot = AppRoot.Instance;
            if (appRoot != null)
            {
                var config = RunStartConfig.CreateDefault();
                config.DifficultyId = DifficultySelectionState.SelectedDifficultyId;
                appRoot.GameSession.StartNewRun(config);
            }

            SceneFlow.Load(SceneNames.Game);
        }

        private void OnContinueClicked()
        {
            if (continueButton != null)
            {
                continueButton.interactable = false;
            }

            if (!RunSaveSystem.TryLoadPreparing(out RunSaveData save) || save == null)
            {
                GameAudio.PlaySfx(SfxId.UiError);
                RefreshContinueButton();
                return;
            }

            GameAudio.PlaySfx(SfxId.UiConfirm);

            GameDatabase gameDatabase = GameDatabaseLocator.Load();
            if (gameDatabase == null)
            {
                Debug.LogError("[MainMenuController] GameDatabase를 로드하지 못했습니다.");
                if (continueButton != null)
                {
                    continueButton.interactable = true;
                }

                return;
            }

            AppRoot appRoot = AppRoot.Instance;
            if (appRoot == null)
            {
                return;
            }

            var config = RunSaveMapper.CreateStartConfigFromSave(save);
            appRoot.GameSession.StartNewRun(config);
            RunSaveMapper.ApplyToRunState(appRoot.GameSession.RunState, save, gameDatabase);

            // RunStarted는 저장 복원 전에 발생하므로, 복원 후 Context를 다시 맞춘다.
            appRoot.Analytics?.BindRun(appRoot.GameSession.RunState);
            appRoot.Analytics?.Track(AnalyticsEventNames.SaveRecovered, new Dictionary<string, object>
            {
                ["station_index"] = save.stationIndex,
                ["run_id"] = appRoot.GameSession.RunState?.RunId ?? string.Empty,
            });

            SceneFlow.Load(SceneNames.Game);
        }

        private void RefreshContinueButton()
        {
            if (continueButton == null)
            {
                return;
            }

            bool hasSave = RunSaveSystem.TryLoadPreparing(out _);
            continueButton.interactable = hasSave;
        }
    }
}
