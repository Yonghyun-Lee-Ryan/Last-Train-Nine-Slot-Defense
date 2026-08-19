using System;
using System.Collections.Generic;
using LastTrain.Analytics;
using LastTrain.Attendance;
using LastTrain.Audio;
using LastTrain.Core;
using LastTrain.Data;
using LastTrain.Difficulty;
using LastTrain.Endless;
using LastTrain.LiveOps;
using LastTrain.Mission;
using LastTrain.Run;
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
        private MissionPanelController _missionPanel;
        private LiveEventPanelController _liveEventPanel;
        private CodexPanelController _codexPanel;
        private AttendancePanelController _attendancePanel;
        private AchievementPanelController _achievementPanel;
        private PrivacyConsentDialogController _privacyDialog;
        private DifficultySelectionController _difficultySelection;
        private DifficultyUnlockPopupController _difficultyUnlockPopup;
        private Button _missionButton;
        private Button _dailyRunButton;
        private Button _quickRunButton;
        private Button _endlessRunButton;
        private Button _liveEventButton;
        private Button _codexButton;
        private Button _attendanceButton;
        private Button _endlessMilestoneButton;
        private Button _achievementButton;
        private Button _todayGoalButton;
        private Text _todayGoalLabel;
        private HomeGoalSnapshot _currentGoal;
        private Button _tabPlay;
        private Button _tabGrowth;
        private Button _tabSeason;

        private void Awake()
        {
            MainMenuHomeTabs.Active = MainMenuHomeSection.Play;
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
            EnsureMissionServices();
            EnsureCodexServices();
            EnsureAttendanceServices();
            EnsureEndlessMilestoneServices();
            EnsureAchievementServices();
            Transform safeArea = MainMenuUiLayout.FindOwnedSafeArea(this);
            ApplyMenuVisualTheme(safeArea);
            EnsureHomeIa(safeArea);
            MainMenuUiLayout.Apply(safeArea);
            RebindMetaStatusLabel();
            RefreshMetaProgress();
            RefreshTodayGoalCard();

            _privacyDialog?.TryShowIfNeeded();
            _difficultyUnlockPopup?.TryShowPendingUnlocks();
            _attendancePanel?.TryShowIfClaimable();
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

        private void EnsureMissionServices()
        {
            _missionPanel = GetComponent<MissionPanelController>();
            if (_missionPanel == null)
            {
                _missionPanel = gameObject.AddComponent<MissionPanelController>();
            }

            _liveEventPanel = GetComponent<LiveEventPanelController>();
            if (_liveEventPanel == null)
            {
                _liveEventPanel = gameObject.AddComponent<LiveEventPanelController>();
            }

            AppRoot.Instance?.RefreshLiveOpsOnMenu();
            EnsureMissionButtons();
            MetaSaveData meta = MetaSaveSystem.LoadOrCreate();
            MissionProgressService.EnsurePeriods(meta, GameDatabaseLocator.Load()?.Missions);
            MetaSaveSystem.Save(meta);
        }

        private void EnsureCodexServices()
        {
            _codexPanel = GetComponent<CodexPanelController>();
            if (_codexPanel == null)
            {
                _codexPanel = gameObject.AddComponent<CodexPanelController>();
            }

            EnsureCodexButton();
        }

        private void EnsureCodexButton()
        {
            Transform parent = ResolveMenuContentParent();
            if (parent == null)
            {
                return;
            }

            if (GameObject.Find("CodexButton") == null)
            {
                _codexButton = MenuOverlayUi.CreateButton(
                    parent,
                    "CodexButton",
                    "도감",
                    Vector2.zero,
                    new Vector2(UiButtonStyler.MenuActionMaxWidth, UiButtonStyler.MenuSecondaryHeight),
                    OnCodexClicked);
                ApplyContinueButtonThemeTo(_codexButton);
            }
            else
            {
                _codexButton = GameObject.Find("CodexButton").GetComponent<Button>();
                _codexButton.onClick.RemoveAllListeners();
                _codexButton.onClick.AddListener(OnCodexClicked);
            }

            RefreshCodexButton();
        }

        private void OnCodexClicked()
        {
            _codexPanel?.Show(GameDatabaseLocator.Load());
            RefreshCodexButton();
        }

        private void RefreshCodexButton()
        {
            if (_codexButton == null)
            {
                return;
            }

            MetaProgressSnapshot snapshot = MetaSaveSystem.GetSnapshot();
            int pending = snapshot.PendingNewDiscoveryIds?.Count ?? 0;
            Text label = _codexButton.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = pending > 0 ? $"도감 ({pending})" : "도감";
            }
        }

        private void EnsureAttendanceServices()
        {
            _attendancePanel = GetComponent<AttendancePanelController>();
            if (_attendancePanel == null)
            {
                _attendancePanel = gameObject.AddComponent<AttendancePanelController>();
            }

            EnsureAttendanceButton();
        }

        private void EnsureAttendanceButton()
        {
            Transform parent = ResolveMenuContentParent();
            if (parent == null)
            {
                return;
            }

            if (GameObject.Find("AttendanceButton") == null)
            {
                _attendanceButton = MenuOverlayUi.CreateButton(
                    parent,
                    "AttendanceButton",
                    "출석",
                    Vector2.zero,
                    new Vector2(UiButtonStyler.MenuActionMaxWidth, UiButtonStyler.MenuSecondaryHeight),
                    OnAttendanceClicked);
                ApplyContinueButtonThemeTo(_attendanceButton);
            }
            else
            {
                _attendanceButton = GameObject.Find("AttendanceButton").GetComponent<Button>();
                _attendanceButton.onClick.RemoveAllListeners();
                _attendanceButton.onClick.AddListener(OnAttendanceClicked);
            }

            RefreshAttendanceButton();
        }

        private void OnAttendanceClicked()
        {
            _attendancePanel?.Show();
            RefreshAttendanceButton();
        }

        public void RefreshAttendanceButton()
        {
            if (_attendanceButton == null)
            {
                return;
            }

            MetaSaveData meta = MetaSaveSystem.LoadOrCreate();
            bool canClaim = AttendanceService.CanClaimToday(meta);
            Text label = _attendanceButton.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = canClaim ? "출석 (받기)" : "출석";
            }
        }

        public void TryShowQueuedAttendance()
        {
            _attendancePanel?.TryShowIfClaimable();
        }

        private EndlessMilestonePanelController _endlessMilestonePanel;

        private void EnsureEndlessMilestoneServices()
        {
            _endlessMilestonePanel = GetComponent<EndlessMilestonePanelController>();
            if (_endlessMilestonePanel == null)
            {
                _endlessMilestonePanel = gameObject.AddComponent<EndlessMilestonePanelController>();
            }

            Transform parent = ResolveMenuContentParent();
            if (parent == null)
            {
                return;
            }

            if (GameObject.Find("EndlessMilestoneButton") == null)
            {
                _endlessMilestoneButton = MenuOverlayUi.CreateButton(
                    parent,
                    "EndlessMilestoneButton",
                    "무한 마일스톤",
                    Vector2.zero,
                    new Vector2(UiButtonStyler.MenuActionMaxWidth, UiButtonStyler.MenuSecondaryHeight),
                    () => _endlessMilestonePanel?.Show());
                ApplyContinueButtonThemeTo(_endlessMilestoneButton);
            }
            else
            {
                _endlessMilestoneButton = GameObject.Find("EndlessMilestoneButton").GetComponent<Button>();
                _endlessMilestoneButton.onClick.RemoveAllListeners();
                _endlessMilestoneButton.onClick.AddListener(() => _endlessMilestonePanel?.Show());
            }
        }

        private void EnsureAchievementServices()
        {
            _achievementPanel = GetComponent<AchievementPanelController>();
            if (_achievementPanel == null)
            {
                _achievementPanel = gameObject.AddComponent<AchievementPanelController>();
            }

            EnsureAchievementButton();
        }

        private void EnsureAchievementButton()
        {
            Transform parent = ResolveMenuContentParent();
            if (parent == null)
            {
                return;
            }

            if (GameObject.Find("AchievementButton") == null)
            {
                _achievementButton = MenuOverlayUi.CreateButton(
                    parent,
                    "AchievementButton",
                    "업적",
                    Vector2.zero,
                    new Vector2(UiButtonStyler.MenuActionMaxWidth, UiButtonStyler.MenuSecondaryHeight),
                    OnAchievementClicked);
                ApplyContinueButtonThemeTo(_achievementButton);
            }
            else
            {
                _achievementButton = GameObject.Find("AchievementButton").GetComponent<Button>();
                _achievementButton.onClick.RemoveAllListeners();
                _achievementButton.onClick.AddListener(OnAchievementClicked);
            }
        }

        private void OnAchievementClicked()
        {
            _achievementPanel?.Show();
        }

        private void EnsureMissionButtons()
        {
            Transform parent = ResolveMenuContentParent();
            if (parent == null)
            {
                return;
            }

            if (GameObject.Find("MissionButton") == null)
            {
                _missionButton = MenuOverlayUi.CreateButton(
                    parent,
                    "MissionButton",
                    "미션",
                    Vector2.zero,
                    new Vector2(UiButtonStyler.MenuActionMaxWidth, UiButtonStyler.MenuSecondaryHeight),
                    () => _missionPanel?.Show(GameDatabaseLocator.Load()));
                ApplyContinueButtonThemeTo(_missionButton);
            }
            else
            {
                _missionButton = GameObject.Find("MissionButton").GetComponent<Button>();
                _missionButton.onClick.RemoveAllListeners();
                _missionButton.onClick.AddListener(() => _missionPanel?.Show(GameDatabaseLocator.Load()));
            }

            if (GameObject.Find("DailyRunButton") == null)
            {
                _dailyRunButton = MenuOverlayUi.CreateButton(
                    parent,
                    "DailyRunButton",
                    "오늘의 막차",
                    Vector2.zero,
                    new Vector2(UiButtonStyler.MenuActionMaxWidth, UiButtonStyler.MenuSecondaryHeight),
                    OnDailyRunClicked);
                ApplyContinueButtonThemeTo(_dailyRunButton);
            }
            else
            {
                _dailyRunButton = GameObject.Find("DailyRunButton").GetComponent<Button>();
                _dailyRunButton.onClick.RemoveAllListeners();
                _dailyRunButton.onClick.AddListener(OnDailyRunClicked);
            }

            if (GameObject.Find("QuickRunButton") == null)
            {
                _quickRunButton = MenuOverlayUi.CreateButton(
                    parent,
                    "QuickRunButton",
                    "퀵 런",
                    Vector2.zero,
                    new Vector2(UiButtonStyler.MenuActionMaxWidth, UiButtonStyler.MenuSecondaryHeight),
                    OnQuickRunClicked);
                ApplyContinueButtonThemeTo(_quickRunButton);
            }
            else
            {
                _quickRunButton = GameObject.Find("QuickRunButton").GetComponent<Button>();
                _quickRunButton.onClick.RemoveAllListeners();
                _quickRunButton.onClick.AddListener(OnQuickRunClicked);
            }

            if (GameObject.Find("EndlessRunButton") == null)
            {
                _endlessRunButton = MenuOverlayUi.CreateButton(
                    parent,
                    "EndlessRunButton",
                    "무한 모드",
                    Vector2.zero,
                    new Vector2(UiButtonStyler.MenuActionMaxWidth, UiButtonStyler.MenuSecondaryHeight),
                    OnEndlessRunClicked);
                ApplyContinueButtonThemeTo(_endlessRunButton);
            }
            else
            {
                _endlessRunButton = GameObject.Find("EndlessRunButton").GetComponent<Button>();
                _endlessRunButton.onClick.RemoveAllListeners();
                _endlessRunButton.onClick.AddListener(OnEndlessRunClicked);
            }

            if (GameObject.Find("LiveEventButton") == null)
            {
                _liveEventButton = MenuOverlayUi.CreateButton(
                    parent,
                    "LiveEventButton",
                    "시즌 이벤트",
                    Vector2.zero,
                    new Vector2(UiButtonStyler.MenuActionMaxWidth, UiButtonStyler.MenuSecondaryHeight),
                    () => _liveEventPanel?.Show());
                ApplyContinueButtonThemeTo(_liveEventButton);
            }
            else
            {
                _liveEventButton = GameObject.Find("LiveEventButton").GetComponent<Button>();
                _liveEventButton.onClick.RemoveAllListeners();
                _liveEventButton.onClick.AddListener(() => _liveEventPanel?.Show());
            }

            RefreshEndlessButton();
            RefreshDailyRunButton();
            RefreshLiveEventButton();
            RefreshTodayGoalCard();
            RelayoutMainMenu();
        }

        private Transform ResolveMenuContentParent()
        {
            Transform safeArea = MainMenuUiLayout.FindOwnedSafeArea(this);
            if (safeArea == null)
            {
                return null;
            }

            RectTransform content = MainMenuUiLayout.EnsureContentRoot(safeArea);
            return content != null ? content : safeArea;
        }

        private void EnsureHomeIa(Transform safeArea)
        {
            if (safeArea == null)
            {
                return;
            }

            Transform content = MainMenuUiLayout.EnsureContentRoot(safeArea);
            Transform parent = content != null ? content : safeArea;
            EnsureTodayGoalCard(parent);
            EnsureHomeTabBar(parent);
            EnsureGrowthPlaceholder(parent);
        }

        private void EnsureTodayGoalCard(Transform parent)
        {
            Transform existing = parent != null ? parent.Find("TodayGoalCard") : null;
            if (existing == null)
            {
                GameObject found = GameObject.Find("TodayGoalCard");
                existing = found != null ? found.transform : null;
            }

            if (existing == null)
            {
                var go = new GameObject("TodayGoalCard", typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(parent, false);
                _todayGoalButton = go.GetComponent<Button>();
                _todayGoalButton.onClick.AddListener(OnTodayGoalClicked);
                existing = go.transform;
            }
            else
            {
                _todayGoalButton = existing.GetComponent<Button>();
                if (_todayGoalButton == null)
                {
                    _todayGoalButton = existing.gameObject.AddComponent<Button>();
                }

                _todayGoalButton.onClick.RemoveListener(OnTodayGoalClicked);
                _todayGoalButton.onClick.AddListener(OnTodayGoalClicked);
            }

            _todayGoalLabel = MainMenuUiLayout.ResolveMetaStatusText(existing);
        }

        private void EnsureHomeTabBar(Transform parent)
        {
            Transform bar = parent != null ? parent.Find("HomeTabBar") : null;
            if (bar == null)
            {
                GameObject found = GameObject.Find("HomeTabBar");
                bar = found != null ? found.transform : null;
            }

            if (bar == null)
            {
                var go = new GameObject("HomeTabBar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                bar = go.transform;
                bar.SetParent(parent, false);
            }

            _tabPlay = EnsureTabButton(bar, "TabPlay", "플레이", MainMenuHomeSection.Play);
            _tabGrowth = EnsureTabButton(bar, "TabGrowth", "성장", MainMenuHomeSection.Growth);
            _tabSeason = EnsureTabButton(bar, "TabSeason", "시즌", MainMenuHomeSection.Season);
            HighlightActiveTab();
        }

        private void EnsureGrowthPlaceholder(Transform parent)
        {
            Transform existing = parent != null ? parent.Find("GrowthPlaceholder") : null;
            if (existing != null)
            {
                return;
            }

            var go = new GameObject("GrowthPlaceholder", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Text label = go.AddComponent<Text>();
            label.text = "성장\n도감·출석·업적에서 메타 보상을 확인하세요.";
            label.alignment = TextAnchor.MiddleCenter;
            label.fontSize = 24;
            label.color = Color.white;
            Font font = GameFontProvider.Get();
            if (font != null)
            {
                label.font = font;
            }

            go.SetActive(false);
        }

        private Button EnsureTabButton(Transform bar, string name, string label, MainMenuHomeSection section)
        {
            Transform child = bar.Find(name);
            Button button;
            if (child == null)
            {
                button = MenuOverlayUi.CreateLayoutButton(
                    bar,
                    name,
                    label,
                    56f,
                    () => SelectHomeSection(section),
                    28);
            }
            else
            {
                button = child.GetComponent<Button>();
                if (button == null)
                {
                    button = child.gameObject.AddComponent<Button>();
                }

                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => SelectHomeSection(section));
                Text text = child.GetComponentInChildren<Text>();
                if (text != null)
                {
                    text.text = label;
                }
            }

            return button;
        }

        private void SelectHomeSection(MainMenuHomeSection section)
        {
            if (MainMenuHomeTabs.Active == section)
            {
                return;
            }

            GameAudio.PlaySfx(SfxId.UiToggle);
            MainMenuHomeTabs.Active = section;
            HighlightActiveTab();
            RelayoutMainMenu();
        }

        private void HighlightActiveTab()
        {
            SetTabHighlight(_tabPlay, MainMenuHomeTabs.Active == MainMenuHomeSection.Play);
            SetTabHighlight(_tabGrowth, MainMenuHomeTabs.Active == MainMenuHomeSection.Growth);
            SetTabHighlight(_tabSeason, MainMenuHomeTabs.Active == MainMenuHomeSection.Season);
        }

        private static void SetTabHighlight(Button button, bool active)
        {
            if (button == null)
            {
                return;
            }

            Text label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.fontSize = active ? 28 : 24;
                label.color = active ? Color.white : new Color(0.75f, 0.8f, 0.88f, 1f);
            }

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = active ? Color.white : new Color(0.85f, 0.85f, 0.85f, 1f);
            }
        }

        private void RefreshTodayGoalCard()
        {
            EnsureHomeIa(FindSafeArea());
            GameDatabase db = GameDatabaseLocator.Load();
            MetaSaveData meta = MetaSaveSystem.LoadOrCreate();
            if (db?.Missions != null)
            {
                MissionProgressService.EnsurePeriods(meta, db.Missions);
            }

            var missionViews = MissionProgressService.BuildViews(meta, db?.Missions);
            LiveEventService live = AppRoot.Instance?.LiveEvents;
            string seasonName = live != null && live.HasActiveEvent && live.ActiveEvent != null
                ? live.ActiveEvent.DisplayName
                : null;
            bool hasContinue = RunSaveSystem.TryLoadPreparing(out _);
            _currentGoal = HomeGoalResolver.Resolve(
                missionViews,
                db?.Difficulties,
                meta,
                seasonName,
                hasContinue);

            if (_todayGoalLabel == null && GameObject.Find("TodayGoalCard") != null)
            {
                _todayGoalLabel = MainMenuUiLayout.ResolveMetaStatusText(
                    GameObject.Find("TodayGoalCard").transform);
            }

            if (_todayGoalLabel != null && _currentGoal != null)
            {
                _todayGoalLabel.text =
                    $"{_currentGoal.Title}\n{_currentGoal.Body}\n[{_currentGoal.CtaLabel}]";
            }
        }

        private void OnTodayGoalClicked()
        {
            if (_currentGoal == null)
            {
                RefreshTodayGoalCard();
            }

            if (_currentGoal == null)
            {
                return;
            }

            GameAudio.PlaySfx(SfxId.UiConfirm);
            switch (_currentGoal.Kind)
            {
                case HomeGoalKind.MissionClaim:
                case HomeGoalKind.MissionProgress:
                    SelectHomeSection(MainMenuHomeSection.Season);
                    _missionPanel?.Show(GameDatabaseLocator.Load());
                    break;
                case HomeGoalKind.SeasonEvent:
                    SelectHomeSection(MainMenuHomeSection.Season);
                    _liveEventPanel?.Show();
                    break;
                case HomeGoalKind.DifficultyUnlock:
                    SelectHomeSection(MainMenuHomeSection.Play);
                    break;
                case HomeGoalKind.ContinueRun:
                    SelectHomeSection(MainMenuHomeSection.Play);
                    OnContinueClicked();
                    break;
                case HomeGoalKind.StartRun:
                    SelectHomeSection(MainMenuHomeSection.Play);
                    break;
            }
        }

        private Transform FindSafeArea()
        {
            return MainMenuUiLayout.FindOwnedSafeArea(this);
        }

        private void RefreshLiveEventButton()
        {
            LiveEventService live = AppRoot.Instance?.LiveEvents;
            MainMenuHomeTabs.LiveEventAvailable = live != null && live.HasActiveEvent;
            if (_liveEventButton != null
                && MainMenuHomeTabs.LiveEventAvailable
                && live.ActiveEvent != null)
            {
                Text label = _liveEventButton.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = live.ActiveEvent.DisplayName;
                }
            }

            RelayoutMainMenu();
        }

        private void RelayoutMainMenu()
        {
            MainMenuUiLayout.Apply(MainMenuUiLayout.FindOwnedSafeArea(this));
        }

        private void RefreshEndlessButton()
        {
            if (_endlessRunButton == null)
            {
                return;
            }

            MetaSaveData meta = MetaSaveSystem.LoadOrCreate();
            bool unlocked = EndlessProgressService.IsUnlocked(meta);
            _endlessRunButton.interactable = unlocked;
            Text label = _endlessRunButton.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = unlocked
                    ? (meta.endlessBestScore > 0
                        ? $"무한 모드  (로컬 최고 {meta.endlessBestScore})"
                        : "무한 모드")
                    : "무한 모드  (노선 클리어 후 해금)";
            }
        }

        private void RefreshDailyRunButton()
        {
            if (_dailyRunButton == null)
            {
                return;
            }

            GameDatabase database = GameDatabaseLocator.Load();
            DailyRuleData rule = DailyRunService.ResolveToday(database != null ? database.DailyRules : null);
            Text label = _dailyRunButton.GetComponentInChildren<Text>();
            if (label == null)
            {
                return;
            }

            label.text = rule != null && !string.IsNullOrWhiteSpace(rule.DisplayName)
                ? $"오늘의 막차\n{rule.DisplayName}"
                : "오늘의 막차";
        }

        private static void ApplyContinueButtonThemeTo(Button button)
        {
            UiButtonStyler.ApplyStandardTheme(button);
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

            Transform parent = MainMenuUiLayout.FindOwnedSafeArea(this);
            if (parent == null)
            {
                return;
            }

            Button settingsButton = MenuOverlayUi.CreateButton(
                parent,
                "SettingsButton",
                "설정",
                new Vector2(-48f, -48f),
                new Vector2(168f, 72f),
                () => _settingsPanel?.Show());

            RectTransform rect = settingsButton.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-48f, -48f);
            ApplySettingsButtonTheme(settingsButton);
        }

        private static void ApplySettingsButtonTheme(Button button)
        {
            UiButtonStyler.ApplyStandardTheme(button);
        }

        private void EnsureMetaStatusLabel()
        {
            if (metaStatusLabel != null)
            {
                return;
            }

            Transform parent = ResolveMenuContentParent();
            if (parent == null)
            {
                return;
            }

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
                RefreshCodexButton();
            }

            RefreshDifficultySelection();
            RefreshEndlessButton();
            RefreshDailyRunButton();
            RefreshLiveEventButton();
            RefreshTodayGoalCard();
            RefreshAttendanceButton();
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
            Transform parent = ResolveMenuContentParent();
            if (parent == null)
            {
                return;
            }

            GameObject go = new GameObject("ContinueButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(UiButtonStyler.MenuActionMaxWidth, UiButtonStyler.MenuPrimaryHeight);
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
            UiButtonStyler.ApplyStandardTheme(continueButton);
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

        private void OnDailyRunClicked()
        {
            if (_dailyRunButton != null)
            {
                _dailyRunButton.interactable = false;
            }

            GameAudio.PlaySfx(SfxId.UiConfirm);

            // 오늘의 막차는 이어하기·시드 변경을 막는다.
            RunSaveSystem.DeleteRunSave();
            DifficultySelectionState.UnlockSelection();

            AppRoot appRoot = AppRoot.Instance;
            if (appRoot != null)
            {
                int seed = DailyRunService.ComputeSeedForToday();
                RunStartConfig config = RunStartConfig.CreateDailyRun(seed);
                GameDatabase database = GameDatabaseLocator.Load();
                IReadOnlyList<DailyRuleData> catalog = database != null ? database.DailyRules : null;
                DailyRuleData rule = DailyRunService.ResolveRule(catalog, seed);
                DailyRunService.BindRule(config, rule, seed, catalog != null ? catalog.Count : 0);
                config.DifficultyId = DifficultySelectionState.SelectedDifficultyId;
                appRoot.GameSession.StartNewRun(config);
            }

            SceneFlow.Load(SceneNames.Game);
        }

        private void OnQuickRunClicked()
        {
            if (_quickRunButton != null)
            {
                _quickRunButton.interactable = false;
            }

            GameAudio.PlaySfx(SfxId.UiConfirm);
            RunSaveSystem.DeleteRunSave();
            DifficultySelectionState.UnlockSelection();

            AppRoot appRoot = AppRoot.Instance;
            if (appRoot != null)
            {
                RunStartConfig config = RunStartConfig.CreateQuickRun(DifficultySelectionState.SelectedDifficultyId);
                appRoot.GameSession.StartNewRun(config);
            }

            SceneFlow.Load(SceneNames.Game);
        }

        private void OnEndlessRunClicked()
        {
            MetaSaveData meta = MetaSaveSystem.LoadOrCreate();
            if (!EndlessProgressService.IsUnlocked(meta))
            {
                GameAudio.PlaySfx(SfxId.UiError);
                RefreshEndlessButton();
                return;
            }

            if (_endlessRunButton != null)
            {
                _endlessRunButton.interactable = false;
            }

            GameAudio.PlaySfx(SfxId.UiConfirm);
            RunSaveSystem.DeleteRunSave();
            DifficultySelectionState.UnlockSelection();

            AppRoot appRoot = AppRoot.Instance;
            if (appRoot != null)
            {
                DifficultyModifierData[] depthMods = null;
                EndlessRouteData endless = GameDatabaseLocator.Load()?.EndlessRoute;
                if (endless?.DepthModifiers != null && endless.DepthModifiers.Count > 0)
                {
                    depthMods = new DifficultyModifierData[endless.DepthModifiers.Count];
                    for (int i = 0; i < depthMods.Length; i++)
                    {
                        depthMods[i] = endless.DepthModifiers[i];
                    }
                }

                RunStartConfig config = RunStartConfig.CreateEndlessRun(
                    DifficultySelectionState.SelectedDifficultyId,
                    depthMods);
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

            if (!RunSaveSystem.TryLoadPreparing(out RunSaveData save) || save == null || save.isDailyRun)
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
            // 오늘의 막차 세이브는 이어하기 대상이 아니다.
            bool hasSave = RunSaveSystem.TryLoadPreparing(out RunSaveData save)
                           && save != null
                           && !save.isDailyRun;
            MainMenuHomeTabs.ContinueAvailable = hasSave;
            if (continueButton == null)
            {
                return;
            }

            continueButton.interactable = hasSave;
        }
    }
}
