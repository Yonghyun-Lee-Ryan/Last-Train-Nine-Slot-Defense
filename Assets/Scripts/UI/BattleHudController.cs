using System;
using System.Collections;
using LastTrain.Audio;
using LastTrain.Battle;
using LastTrain.Core;
using LastTrain.Data;
using LastTrain.Feedback;
using LastTrain.Grid;
using LastTrain.Passenger;
using LastTrain.Release;
using LastTrain.Run;
using LastTrain.Save;
using UnityEngine;
using UnityEngine.UI;

namespace LastTrain.UI
{
    /// <summary>
    /// 전투 HUD. RunState/이벤트를 구독해 표시하며, 상태 변경은 StationManager·서비스에 위임한다.
    /// </summary>
    [DefaultExecutionOrder(110)]
    public sealed class BattleHudController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GridManager gridManager;
        [SerializeField] private GameBattleBootstrap battleBootstrap;
        [SerializeField] private PassengerDetailPopup detailPopup;
        [SerializeField] private PassengerRangeOverlay rangeOverlay;
        [SerializeField] private FloatingCombatText floatingTextPrefab;
        [SerializeField] private RectTransform floatingTextRoot;

        [Header("Status")]
        [SerializeField] private Slider trainHpSlider;
        [SerializeField] private Text trainHpLabel;
        [SerializeField] private Text coinLabel;
        [SerializeField] private Text stationLabel;
        [SerializeField] private Text waveLabel;
        [SerializeField] private Text phaseLabel;
        [SerializeField] private Text statusLabel;

        [Header("Boss")]
        [SerializeField] private GameObject bossHpRoot;
        [SerializeField] private Slider bossHpSlider;
        [SerializeField] private Text bossHpLabel;
        [SerializeField] private Text bossNameLabel;

        [Header("Actions")]
        [SerializeField] private Button readyButton;
        [SerializeField] private Button speedButton;
        [SerializeField] private Button undoMergeButton;
        [SerializeField] private Button pauseButton;
        [SerializeField] private GameObject pauseOverlay;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button mainMenuFromPauseButton;
        [SerializeField] private Button settingsFromPauseButton;

        private SettingsPanelController _settingsPanel;

        private RunState _runState;
        private GameSession _session;
        private BattleManager _battleManager;
        private bool _runEndHandled;
        private readonly UiInputGuard _inputGuard = new();
        private float _speedScale = 1f;
        private bool _paused;
        private int _lastCoins = -1;
        private int _lastHp = -1;
        private int _totalStations = 5;
        private SummonPanelController _summonPanel;
        private EnemyPathDirectionView _pathDirectionView;
        private WaveThreatTickerView _threatTicker;
        private Coroutine _restoreSpeedCoroutine;

        private void OnEnable()
        {
            BattleSpeedRuntime.TimeScaleRestoreRequested += HandleTimeScaleRestoreRequested;
        }

        private void OnDisable()
        {
            BattleSpeedRuntime.TimeScaleRestoreRequested -= HandleTimeScaleRestoreRequested;
            if (_restoreSpeedCoroutine != null)
            {
                StopCoroutine(_restoreSpeedCoroutine);
                _restoreSpeedCoroutine = null;
            }
        }

        private void HandleTimeScaleRestoreRequested()
        {
            if (_paused || _runEndHandled || _runState == null)
            {
                return;
            }

            ApplyBattleSpeedFromSettings();
        }

        private void Start()
        {
            AppRoot appRoot = AppRoot.Instance;
            if (appRoot == null || !appRoot.GameSession.HasActiveRun)
            {
                Debug.LogWarning("[BattleHudController] 활성 RunState가 없습니다.", this);
                return;
            }

            _session = appRoot.GameSession;
            _runState = _session.RunState;
            _runEndHandled = false;

            if (gridManager == null)
            {
                gridManager = FindAnyObjectByType<GridManager>();
            }

            if (battleBootstrap == null)
            {
                battleBootstrap = FindAnyObjectByType<GameBattleBootstrap>();
            }

            if (battleBootstrap != null && battleBootstrap.GameDatabase != null)
            {
                string lineId = _runState?.LineId;
                if (!string.IsNullOrWhiteSpace(lineId))
                {
                    _totalStations = battleBootstrap.GameDatabase.GetRouteStationCount(lineId);
                }
                else
                {
                    _totalStations = battleBootstrap.GameDatabase.Stations?.Count ?? 5;
                }
            }

            _battleManager = FindAnyObjectByType<BattleManager>();

            EnsureCombatUxOverlays();
            Subscribe();
            EnsurePauseOverlayButtons();
            WireButtons();
            ApplyBattleSpeedFromSettings();
            EnsureUndoMergeButton();
            ApplyHudTheme();
            HideTopCoinDisplay();
            BindCameraShakeTarget();
            detailPopup?.Initialize(_runState, OnPassengerSold, OnDetailPopupClosed);
            rangeOverlay?.Hide();
            if (pauseOverlay != null)
            {
                pauseOverlay.SetActive(false);
            }

            SetBossVisible(false);
            _lastCoins = _runState.Currency.CurrentCoins;
            _lastHp = _runState.Train.CurrentHp;
            RefreshAll();
            ShowCurrentStationBriefing();
            if (string.IsNullOrWhiteSpace(statusLabel?.text))
            {
                SetStatus("준비 완료를 눌러 전투를 시작하세요.");
            }
        }

        private void Update()
        {
            RefreshAutoStartCountdown();
        }

        private void OnApplicationPause(bool pause)
        {
            if (!pause)
            {
                RestoreBattleSpeedAfterExternalOverlay();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                RestoreBattleSpeedAfterExternalOverlay();
            }
        }

        private void RestoreBattleSpeedAfterExternalOverlay()
        {
            if (_paused || _runEndHandled || _runState == null)
            {
                return;
            }

            if (_restoreSpeedCoroutine != null)
            {
                StopCoroutine(_restoreSpeedCoroutine);
            }

            _restoreSpeedCoroutine = StartCoroutine(RestoreBattleSpeedAfterExternalOverlayRoutine());
        }

        private IEnumerator RestoreBattleSpeedAfterExternalOverlayRoutine()
        {
            // AdMob/OS 오버레이가 timeScale을 0·1로 바꾼 뒤 한 프레임 늦게 복구한다.
            yield return null;
            yield return null;

            _restoreSpeedCoroutine = null;
            if (!_paused && !_runEndHandled && _runState != null)
            {
                ApplyBattleSpeedFromSettings();
            }
        }

        private void ApplyHudTheme()
        {
            VisualTheme theme = VisualThemeLocator.Load();
            if (theme == null)
            {
                return;
            }

            UiButtonStyler.ApplyStandardTheme(readyButton);
            UiButtonStyler.ApplyStandardTheme(speedButton);
            UiButtonStyler.ApplyStandardTheme(undoMergeButton);
            UiButtonStyler.ApplyStandardTheme(pauseButton);
            UiButtonStyler.ApplyStandardTheme(resumeButton);

            // 우상단 코인은 숨김 — 좌하단 SummonPanel만 사용.
            AttachLabelIcon(stationLabel, theme.IconStation);
            AttachLabelIcon(waveLabel, theme.IconWave);
            AttachButtonIcon(readyButton, theme.IconReady);
            AttachButtonIcon(speedButton, theme.IconSpeed);
            AttachButtonIcon(pauseButton, theme.IconPause);

            if (trainHpSlider != null)
            {
                ApplySliderSprites(trainHpSlider, theme.HpBarBackground, theme.HpBarFill);
            }

            if (bossHpSlider != null)
            {
                ApplySliderSprites(bossHpSlider, theme.HpBarBackground, theme.BossHpBarFill);
            }
        }

        private static void ApplySliderSprites(Slider slider, Sprite background, Sprite fill)
        {
            if (slider == null)
            {
                return;
            }

            Image bg = slider.GetComponent<Image>();
            if (bg != null && background != null)
            {
                bg.sprite = background;
                bg.type = Image.Type.Sliced;
                bg.color = Color.white;
            }

            if (slider.fillRect != null)
            {
                Image fillImage = slider.fillRect.GetComponent<Image>();
                if (fillImage != null && fill != null)
                {
                    fillImage.sprite = fill;
                    fillImage.type = Image.Type.Sliced;
                    fillImage.color = Color.white;
                }

                Transform fillArea = slider.fillRect.parent;
                if (fillArea != null && fillArea.GetComponent<RectMask2D>() == null)
                {
                    fillArea.gameObject.AddComponent<RectMask2D>();
                }
            }
        }

        private static void AttachLabelIcon(Text label, Sprite sprite)
        {
            if (label == null || sprite == null)
            {
                return;
            }

            Transform existing = label.transform.Find("ThemeIcon");
            RectTransform rect;
            Image image;
            if (existing != null)
            {
                rect = existing as RectTransform;
                image = existing.GetComponent<Image>();
            }
            else
            {
                var go = new GameObject("ThemeIcon", typeof(RectTransform), typeof(Image));
                rect = go.GetComponent<RectTransform>();
                rect.SetParent(label.transform, false);
                image = go.GetComponent<Image>();
            }

            if (rect == null || image == null)
            {
                return;
            }

            // 라벨 왼쪽 바깥에 배치해 가운데 정렬 텍스트를 가리지 않는다.
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-8f, 0f);
            rect.sizeDelta = new Vector2(36f, 36f);
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = Color.white;
        }

        private static void AttachButtonIcon(Button button, Sprite sprite)
        {
            if (button == null || sprite == null || button.transform.Find("ThemeIcon") != null)
            {
                return;
            }

            var go = new GameObject("ThemeIcon", typeof(RectTransform), typeof(Image));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(button.transform, false);
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(18f, 0f);
            rect.sizeDelta = new Vector2(40f, 40f);
            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = Color.white;
            UiButtonStyler.OffsetButtonLabel(button, 64f);
        }

        private void ShowCurrentStationBriefing()
        {
            StationManager stationManager = battleBootstrap != null ? battleBootstrap.StationManager : null;
            if (stationManager?.CurrentStation == null)
            {
                return;
            }

            HandleStationStarted(stationManager.CurrentStation);
        }

        private void OnDestroy()
        {
            Unsubscribe();
            UnwireButtons();
            if (_paused)
            {
                Time.timeScale = 1f;
            }

            if (_session != null)
            {
                _session.RunEnded -= HandleRunEnded;
            }
        }

        private void Subscribe()
        {
            if (_runState == null)
            {
                return;
            }

            if (_session != null)
            {
                _session.RunEnded += HandleRunEnded;
            }

            _runState.Currency.CoinsChanged += HandleCoinsChanged;
            _runState.Train.HpChanged += HandleHpChanged;
            _runState.Battle.PhaseChanged += HandlePhaseChanged;
            _runState.Station.StationIndexChanged += HandleStationChanged;
            _runState.Station.WaveIndexChanged += HandleWaveChanged;

            if (battleBootstrap?.StationManager != null)
            {
                battleBootstrap.StationManager.StationStarted += HandleStationStarted;
            }

            if (gridManager != null)
            {
                gridManager.PassengerSelected += HandlePassengerSelected;
                gridManager.PassengerDragStarted += HandlePassengerDragStarted;
                gridManager.MergeCompleted += HandleMergeCompleted;
            }

            if (_battleManager != null)
            {
                _battleManager.BossSpawned += HandleBossSpawned;
                _battleManager.BossDespawned += HandleBossDespawned;
                _battleManager.BossHealthChanged += HandleBossHealthChanged;
                _battleManager.BossPhaseChanged += HandleBossPhaseChanged;
            }
        }

        private void Unsubscribe()
        {
            if (_session != null)
            {
                _session.RunEnded -= HandleRunEnded;
            }

            if (_runState?.Currency != null)
            {
                _runState.Currency.CoinsChanged -= HandleCoinsChanged;
            }

            if (_runState?.Train != null)
            {
                _runState.Train.HpChanged -= HandleHpChanged;
            }

            if (_runState?.Battle != null)
            {
                _runState.Battle.PhaseChanged -= HandlePhaseChanged;
            }

            if (_runState?.Station != null)
            {
                _runState.Station.StationIndexChanged -= HandleStationChanged;
                _runState.Station.WaveIndexChanged -= HandleWaveChanged;
            }

            if (battleBootstrap?.StationManager != null)
            {
                battleBootstrap.StationManager.StationStarted -= HandleStationStarted;
            }

            if (gridManager != null)
            {
                gridManager.PassengerSelected -= HandlePassengerSelected;
                gridManager.PassengerDragStarted -= HandlePassengerDragStarted;
                gridManager.MergeCompleted -= HandleMergeCompleted;
            }

            if (_battleManager != null)
            {
                _battleManager.BossSpawned -= HandleBossSpawned;
                _battleManager.BossDespawned -= HandleBossDespawned;
                _battleManager.BossHealthChanged -= HandleBossHealthChanged;
                _battleManager.BossPhaseChanged -= HandleBossPhaseChanged;
            }
        }

        private void WireButtons()
        {
            if (readyButton != null)
            {
                readyButton.onClick.AddListener(OnReadyClicked);
            }

            if (speedButton != null)
            {
                speedButton.onClick.AddListener(OnSpeedClicked);
            }

            if (undoMergeButton != null)
            {
                undoMergeButton.onClick.AddListener(OnUndoMergeClicked);
            }

            if (pauseButton != null)
            {
                pauseButton.onClick.AddListener(OnPauseClicked);
            }

            if (resumeButton != null)
            {
                resumeButton.onClick.AddListener(OnResumeClicked);
            }

            if (mainMenuFromPauseButton != null)
            {
                mainMenuFromPauseButton.onClick.AddListener(OnMainMenuFromPauseClicked);
            }

            if (settingsFromPauseButton != null)
            {
                settingsFromPauseButton.onClick.AddListener(OnSettingsFromPauseClicked);
            }
        }

        private void UnwireButtons()
        {
            if (readyButton != null)
            {
                readyButton.onClick.RemoveListener(OnReadyClicked);
            }

            if (speedButton != null)
            {
                speedButton.onClick.RemoveListener(OnSpeedClicked);
            }

            if (undoMergeButton != null)
            {
                undoMergeButton.onClick.RemoveListener(OnUndoMergeClicked);
            }

            if (pauseButton != null)
            {
                pauseButton.onClick.RemoveListener(OnPauseClicked);
            }

            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveListener(OnResumeClicked);
            }

            if (mainMenuFromPauseButton != null)
            {
                mainMenuFromPauseButton.onClick.RemoveListener(OnMainMenuFromPauseClicked);
            }

            if (settingsFromPauseButton != null)
            {
                settingsFromPauseButton.onClick.RemoveListener(OnSettingsFromPauseClicked);
            }
        }

        private void OnReadyClicked()
        {
            if (!_inputGuard.TryAcquire() || _paused)
            {
                return;
            }

            if (Tutorial.TutorialDirector.Instance != null
                && !Tutorial.TutorialDirector.Instance.Allows(Tutorial.TutorialInputMask.Ready))
            {
                GameAudio.PlaySfx(SfxId.UiError);
                SetStatus("튜토리얼 안내를 먼저 확인하세요.");
                return;
            }

            StationManager stationManager = battleBootstrap != null ? battleBootstrap.StationManager : null;
            if (stationManager == null)
            {
                SetStatus("StationManager가 없습니다.");
                return;
            }

            if (stationManager.IsWaitingForAbilityReward
                || stationManager.IsWaitingForNonCombatInteraction
                || (_runState.Abilities != null && _runState.Abilities.IsSelectingReward))
            {
                GameAudio.PlaySfx(SfxId.UiError);
                SetStatus(stationManager.IsWaitingForNonCombatInteraction
                    ? "상점/이벤트를 먼저 마무리하세요."
                    : "능력 카드를 먼저 선택하세요.");
                return;
            }

            if (_runState.Battle.CurrentPhase != RunPhase.Preparing
                && _runState.Battle.CurrentPhase != RunPhase.RewardSelecting
                && _runState.Battle.CurrentPhase != RunPhase.StationCompleted
                && _runState.Battle.CurrentPhase != RunPhase.WaveCompleted)
            {
                GameAudio.PlaySfx(SfxId.UiError);
                SetStatus("지금은 준비할 수 없습니다.");
                return;
            }

            if (_runState.Battle.CurrentPhase == RunPhase.Preparing
                || _runState.Battle.CurrentPhase == RunPhase.RewardSelecting
                || _runState.Battle.CurrentPhase == RunPhase.StationCompleted)
            {
                bool started = stationManager.TryActivateStation();
                GameAudio.PlaySfx(started ? SfxId.UiConfirm : SfxId.UiError);
                SetStatus(started
                    ? stationManager.UsesWaveManager
                        ? "전투 시작!"
                        : _runState.Battle.CurrentPhase == RunPhase.ShopOpen
                            ? "상점이 열렸습니다."
                            : _runState.Battle.CurrentPhase == RunPhase.EventOpen
                                ? "이벤트가 시작되었습니다."
                                : "역 진행 완료!"
                    : "시작할 웨이브가 없습니다.");
            }

            RefreshAll();
        }

        private void OnSpeedClicked()
        {
            if (!_inputGuard.TryAcquire() || _paused)
            {
                return;
            }

            GameSettingsService settings = AppRoot.Instance?.GameSettings;
            int current = settings != null
                ? settings.BattleSpeed
                : Mathf.RoundToInt(_speedScale);
            int next = BattleSpeedPreset.Cycle(current);
            settings?.SetBattleSpeed(next);
            ApplyBattleSpeed(next);
            GameAudio.PlaySfx(SfxId.Switch);
        }

        private void ApplyBattleSpeedFromSettings()
        {
            int preset = AppRoot.Instance?.GameSettings?.BattleSpeed ?? 1;
            ApplyBattleSpeed(preset);
        }

        private void ApplyBattleSpeed(int preset)
        {
            _speedScale = BattleSpeedPreset.ToTimeScale(preset);
            if (!_paused)
            {
                Time.timeScale = _speedScale;
            }

            if (speedButton != null)
            {
                Text label = speedButton.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = $"{BattleSpeedPreset.Clamp(preset)}x";
                }
            }
        }

        private void EnsureUndoMergeButton()
        {
            if (undoMergeButton == null && speedButton != null && speedButton.transform.parent != null)
            {
                GameObject go = Instantiate(speedButton.gameObject, speedButton.transform.parent);
                go.name = "UndoMergeButton";
                undoMergeButton = go.GetComponent<Button>();
                undoMergeButton.onClick.RemoveAllListeners();
                undoMergeButton.onClick.AddListener(OnUndoMergeClicked);
                Text label = go.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = "되돌리기";
                }

                RectTransform rt = go.GetComponent<RectTransform>();
                RectTransform readyRt = readyButton != null ? readyButton.GetComponent<RectTransform>() : null;
                RectTransform speedRt = speedButton.GetComponent<RectTransform>();
                if (rt != null)
                {
                    Vector2 source = readyRt != null ? readyRt.anchoredPosition : speedRt.anchoredPosition;
                    rt.anchoredPosition = BattleHudLayout.UndoMergeAnchoredPosition(source);
                }
            }

            RefreshUndoMergeButton();
        }

        private void OnUndoMergeClicked()
        {
            if (!_inputGuard.TryAcquire() || _paused)
            {
                return;
            }

            if (Tutorial.TutorialDirector.Instance != null
                && Tutorial.TutorialDirector.Instance.IsTutorialActive
                && !Tutorial.TutorialDirector.Instance.Allows(Tutorial.TutorialInputMask.MergeUndo))
            {
                GameAudio.PlaySfx(SfxId.UiError);
                SetStatus("튜토리얼 중에는 합성을 되돌릴 수 없습니다.");
                return;
            }

            if (!MergeUndoService.TryUndo(_runState))
            {
                GameAudio.PlaySfx(SfxId.UiError);
                SetStatus("되돌릴 합성이 없습니다.");
                RefreshUndoMergeButton();
                return;
            }

            GameAudio.PlaySfx(SfxId.UiConfirm);
            gridManager?.RefreshViews();
            // Undo 직후에는 드래그 종료 경로처럼 MergeHighlightService가 갱신되지 않아서,
            // 합성 가능 쌍(초록 테두리)을 강제로 다시 계산해 표시한다.
            LastTrain.Ux.MergeHighlightService.Refresh(gridManager, _runState);
            SetStatus("합성을 되돌렸습니다.");
            RefreshAll();
        }

        private void RefreshUndoMergeButton()
        {
            if (undoMergeButton == null)
            {
                return;
            }

            bool preparing = _runState?.Battle != null
                             && _runState.Battle.CurrentPhase == RunPhase.Preparing;
            bool canUndo = preparing && MergeUndoService.CanUndo(_runState) && !_paused && !_runEndHandled;
            undoMergeButton.gameObject.SetActive(canUndo);
            undoMergeButton.interactable = canUndo;
        }

        private void HandleMergeCompleted(MergeResult _)
        {
            RefreshUndoMergeButton();
        }

        private void OnPauseClicked()
        {
            if (!_inputGuard.TryAcquire())
            {
                return;
            }

            _paused = true;
            Time.timeScale = 0f;
            battleBootstrap?.SetPaused(true);
            GameAudio.PlaySfx(SfxId.Pause);

            // Unit 16: Preparing 상태에서만 이어하기 저장 생성
            RunSaveSystem.TrySavePreparing(_session);
            EnsurePauseOverlayButtons();

            if (pauseOverlay != null)
            {
                pauseOverlay.transform.SetAsLastSibling();
                pauseOverlay.SetActive(true);
            }
        }

        private void OnResumeClicked()
        {
            if (!_inputGuard.TryAcquire())
            {
                return;
            }

            _paused = false;
            ApplyBattleSpeedFromSettings();
            battleBootstrap?.SetPaused(false);
            GameAudio.PlaySfx(SfxId.Resume);
            if (pauseOverlay != null)
            {
                pauseOverlay.SetActive(false);
            }
        }

        private void OnSettingsFromPauseClicked()
        {
            if (!_inputGuard.TryAcquire())
            {
                return;
            }

            if (_settingsPanel == null)
            {
                _settingsPanel = GetComponent<SettingsPanelController>();
                if (_settingsPanel == null)
                {
                    _settingsPanel = gameObject.AddComponent<SettingsPanelController>();
                }
            }

            GameAudio.PlaySfx(SfxId.UiOpen);
            _settingsPanel.Show();
        }

        private void OnMainMenuFromPauseClicked()
        {
            if (!_inputGuard.TryAcquire())
            {
                return;
            }

            _paused = false;
            Time.timeScale = 1f;
            battleBootstrap?.SetPaused(false);

            // 이어하기 저장을 남기고 Result로 가지 않도록 ClearRun 사용
            RunSaveSystem.TrySavePreparing(_session);
            _session?.ClearRun();
            Time.timeScale = 1f;
            AudioListener.pause = false;
            SceneFlow.Load(SceneNames.MainMenu);
        }

        private const float PauseButtonWidth = 560f;
        private const float PauseButtonHeight = 100f;

        private void EnsurePauseOverlayButtons()
        {
            if (pauseOverlay == null)
            {
                return;
            }

            Image overlayImage = pauseOverlay.GetComponent<Image>();
            if (overlayImage != null)
            {
                overlayImage.color = new Color(0f, 0f, 0f, 0.78f);
                overlayImage.raycastTarget = true;
            }

            if (resumeButton == null)
            {
                resumeButton = pauseOverlay.transform.Find("ResumeButton")?.GetComponent<Button>();
            }

            if (mainMenuFromPauseButton == null)
            {
                Transform existing = pauseOverlay.transform.Find("MainMenuButton");
                if (existing != null)
                {
                    mainMenuFromPauseButton = existing.GetComponent<Button>();
                }
            }

            if (mainMenuFromPauseButton == null)
            {
                mainMenuFromPauseButton = CreatePauseMenuButton(
                    pauseOverlay.transform,
                    "MainMenuButton",
                    "메인 메뉴",
                    new Vector2(0f, -90f));
            }

            if (settingsFromPauseButton == null)
            {
                Transform existingSettings = pauseOverlay.transform.Find("SettingsButton");
                if (existingSettings != null)
                {
                    settingsFromPauseButton = existingSettings.GetComponent<Button>();
                }
            }

            if (settingsFromPauseButton == null)
            {
                settingsFromPauseButton = CreatePauseMenuButton(
                    pauseOverlay.transform,
                    "SettingsButton",
                    "설정",
                    new Vector2(0f, -220f));
            }

            ApplyPauseButtonLayout(resumeButton, new Vector2(0f, 80f), "계속하기");
            ApplyPauseButtonLayout(settingsFromPauseButton, new Vector2(0f, -50f), "설정");

            bool canSaveContinue = _runState != null
                                   && _runState.Battle != null
                                   && _runState.Battle.IsRunActive
                                   && _runState.Battle.CurrentPhase == RunPhase.Preparing
                                   && !_runState.IsDailyRun;
            string mainMenuLabel = canSaveContinue
                ? "메인 메뉴 (이어하기 저장)"
                : "메인 메뉴 (전투 중 저장 안 됨)";
            ApplyPauseButtonLayout(mainMenuFromPauseButton, new Vector2(0f, -180f), mainMenuLabel);

            EnsurePauseSaveHint(canSaveContinue);

            if (settingsFromPauseButton != null)
            {
                settingsFromPauseButton.onClick.RemoveListener(OnSettingsFromPauseClicked);
                settingsFromPauseButton.onClick.AddListener(OnSettingsFromPauseClicked);
            }

            Transform title = pauseOverlay.transform.Find("PauseTitle");
            if (title is RectTransform titleRect)
            {
                titleRect.anchoredPosition = new Vector2(0f, 200f);
            }
        }

        private void EnsurePauseSaveHint(bool canSaveContinue)
        {
            if (pauseOverlay == null)
            {
                return;
            }

            Transform existing = pauseOverlay.transform.Find("SaveHint");
            Text hint;
            if (existing == null)
            {
                var go = new GameObject("SaveHint", typeof(RectTransform), typeof(Text));
                existing = go.transform;
                existing.SetParent(pauseOverlay.transform, false);
                hint = go.GetComponent<Text>();
                hint.font = GameFontProvider.Get();
                hint.fontSize = 22;
                hint.alignment = TextAnchor.MiddleCenter;
                hint.color = new Color(0.85f, 0.9f, 1f, 0.95f);
                hint.raycastTarget = false;
                RectTransform rect = go.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(0f, -280f);
                rect.sizeDelta = new Vector2(720f, 64f);
            }
            else
            {
                hint = existing.GetComponent<Text>();
            }

            if (hint != null)
            {
                hint.text = canSaveContinue
                    ? "준비 단계에서 나가면 이어하기가 저장됩니다."
                    : "전투 중에는 이어하기가 저장되지 않습니다.";
            }
        }

        private static void ApplyPauseButtonLayout(Button button, Vector2 anchoredPos, string label)
        {
            if (button == null)
            {
                return;
            }

            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(PauseButtonWidth, PauseButtonHeight);

            UiButtonStyler.ApplyStandardTheme(button);

            Text text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                if (!string.IsNullOrWhiteSpace(label))
                {
                    text.text = label;
                }

                text.alignment = TextAnchor.MiddleCenter;
                text.fontSize = 36;
                text.color = Color.white;
                if (text.font == null)
                {
                    text.font = GameFontProvider.Get();
                }

                RectTransform textRect = text.rectTransform;
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
            }
        }

        private static Button CreatePauseMenuButton(Transform parent, string name, string label, Vector2 anchoredPos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(PauseButtonWidth, PauseButtonHeight);

            Button button = go.GetComponent<Button>();
            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.SetParent(go.transform, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            Text text = textGo.GetComponent<Text>();
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 36;
            text.color = Color.white;
            text.font = GameFontProvider.Get();
            return button;
        }

        private void HandlePassengerSelected(int slotIndex)
        {
            if (_paused || slotIndex < 0)
            {
                rangeOverlay?.Hide();
                if (slotIndex < 0)
                {
                    detailPopup?.Close(playSfx: false);
                }

                return;
            }

            detailPopup?.Show(slotIndex);
            ShowRangeOverlayForSlot(slotIndex);
        }

        private void HandlePassengerDragStarted()
        {
            rangeOverlay?.Hide();
        }

        private void ShowRangeOverlayForSlot(int slotIndex)
        {
            if (rangeOverlay == null || _runState == null || gridManager == null)
            {
                return;
            }

            PassengerRuntime passenger = _runState.GetPassengerAtSlot(slotIndex);
            if (passenger == null || !gridManager.TryGetSlot(slotIndex, out GridSlot slot) || slot == null)
            {
                rangeOverlay.Hide();
                return;
            }

            RectTransform combatSpace = ResolveCombatSpace();
            Vector2 center = BattleCombatSpace.WorldToLocal(
                combatSpace,
                slot.ContentAnchor != null ? slot.ContentAnchor.position : slot.transform.position);
            float radius = PassengerRangeOverlay.RadiusForEffectiveRange(passenger.GetEffectiveRange());
            rangeOverlay.Show(center, radius);
        }

        private void EnsureCombatUxOverlays()
        {
            RectTransform safeArea = ResolveCombatSpace();
            if (safeArea == null)
            {
                return;
            }

            DestroyMisplacedThreatOverlays(safeArea);
            DestroyAllBossBriefingCards();

            Transform existingPath = safeArea.Find(EnemyPathDirectionView.RootName);
            _pathDirectionView = existingPath != null
                ? existingPath.GetComponent<EnemyPathDirectionView>()
                : EnemyPathDirectionView.Ensure(safeArea);
            SyncPathDirectionVisibility();

            RectTransform hudHost = transform as RectTransform ?? safeArea;
            _threatTicker = WaveThreatTickerView.Ensure(hudHost);
            if (rangeOverlay == null)
            {
                rangeOverlay = PassengerRangeOverlay.Ensure(safeArea);
            }
            else
            {
                rangeOverlay.Hide();
            }
        }

        private void DestroyMisplacedThreatOverlays(RectTransform combatSpace)
        {
            RectTransform hudHost = transform as RectTransform;
            WaveThreatTickerView[] tickers = FindObjectsByType<WaveThreatTickerView>(FindObjectsInactive.Include);
            for (int i = 0; i < tickers.Length; i++)
            {
                WaveThreatTickerView ticker = tickers[i];
                if (ticker == null)
                {
                    continue;
                }

                if (hudHost != null && ticker.transform.IsChildOf(hudHost) && ticker.transform.parent == hudHost)
                {
                    continue;
                }

                DestroyUiObject(ticker.gameObject);
            }

            if (combatSpace != null)
            {
                DestroyChildIfPresent(combatSpace, WaveThreatTickerView.RootName);
            }
        }

        private static void DestroyAllBossBriefingCards()
        {
            BossBriefingCardView[] cards = FindObjectsByType<BossBriefingCardView>(FindObjectsInactive.Include);
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] != null)
                {
                    DestroyUiObject(cards[i].gameObject);
                }
            }
        }

        private static void DestroyChildIfPresent(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child == null)
            {
                return;
            }

            DestroyUiObject(child.gameObject);
        }

        private static void DestroyUiObject(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(go);
            }
            else
            {
                DestroyImmediate(go);
            }
        }

        private void SyncPathDirectionVisibility()
        {
            RunPhase phase = _runState?.Battle != null ? _runState.Battle.CurrentPhase : RunPhase.None;
            bool show = EnemyPathDirectionView.ShouldShow(phase);
            if (show && _pathDirectionView == null)
            {
                RectTransform safeArea = ResolveCombatSpace();
                _pathDirectionView = EnemyPathDirectionView.Ensure(safeArea);
            }

            _pathDirectionView?.SetVisible(show);
        }

        private void RefreshThreatPreview()
        {
            StationManager stationManager = battleBootstrap != null ? battleBootstrap.StationManager : null;
            StationData station = stationManager != null ? stationManager.CurrentStation : null;
            RunPhase phase = _runState?.Battle != null ? _runState.Battle.CurrentPhase : RunPhase.None;
            int waveIndex = stationManager != null
                ? stationManager.CurrentWaveIndex
                : (_runState?.Station != null ? _runState.Station.CurrentWaveIndex : 0);

            var entries = ThreatPreviewResolver.ResolveUpcoming(station, waveIndex, phase);
            if (!CombatTopHudLayout.ShouldShowThreatTicker(phase, entries.Count > 0))
            {
                _threatTicker?.Hide();
                NotifySynergyLayoutChanged();
                return;
            }

            string caption = phase == RunPhase.Preparing || phase == RunPhase.WaveStarting
                ? "이번 웨이브"
                : "다음 웨이브";
            _threatTicker?.Bind(entries, VisualDatabaseLocator.Load(), caption);
            NotifySynergyLayoutChanged();
        }

        private static void NotifySynergyLayoutChanged()
        {
            SynergyHudController synergy = FindAnyObjectByType<SynergyHudController>();
            synergy?.RefreshLayout();
        }

        private RectTransform ResolveCombatSpace()
        {
            if (_battleManager != null && _battleManager.SpawnPoint != null
                && _battleManager.SpawnPoint.parent is RectTransform spawnParent)
            {
                return spawnParent;
            }

            Canvas canvas = gridManager != null ? gridManager.RootCanvas : FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                return null;
            }

            Transform root = canvas.rootCanvas != null ? canvas.rootCanvas.transform : canvas.transform;
            Transform safeArea = root.Find("SafeArea");
            return safeArea as RectTransform ?? canvas.transform as RectTransform;
        }

        private void OnDetailPopupClosed()
        {
            rangeOverlay?.Hide();
            if (gridManager != null && gridManager.SelectedSlotIndex >= 0)
            {
                gridManager.ClearSelection();
            }
        }

        private void HandleRunEnded(RunResult _)
        {
            if (_runEndHandled)
            {
                return;
            }

            _runEndHandled = true;
            MergeUndoService.Clear();
            _paused = true;
            battleBootstrap?.SetPaused(true);
            Time.timeScale = 1f;
            _threatTicker?.Hide();

            if (readyButton != null) readyButton.interactable = false;
            if (speedButton != null) speedButton.interactable = false;
            if (undoMergeButton != null)
            {
                undoMergeButton.interactable = false;
                undoMergeButton.gameObject.SetActive(false);
            }
            if (pauseButton != null) pauseButton.interactable = false;
            if (resumeButton != null) resumeButton.interactable = false;

            if (pauseOverlay != null) pauseOverlay.SetActive(false);

            detailPopup?.Close();

            gridManager?.ClearSelection();
            gridManager?.RefreshViews();

            SetStatus(string.Empty);
        }

        private void OnPassengerSold(int coins)
        {
            gridManager?.ClearSelection();
            gridManager?.RefreshViews();
            SpawnCoinGainText(coins);
            SetStatus($"판매 완료 (+{coins})");
            RefreshAll();
            RefreshUndoMergeButton();
        }

        private void HandleCoinsChanged(int coins)
        {
            if (_lastCoins >= 0 && coins > _lastCoins)
            {
                SpawnCoinGainText(coins - _lastCoins);
            }

            _lastCoins = coins;
            RefreshCoins();
        }

        private void HideTopCoinDisplay()
        {
            if (coinLabel != null)
            {
                coinLabel.gameObject.SetActive(false);
            }
        }

        private void BindCameraShakeTarget()
        {
            Canvas canvas = gridManager != null ? gridManager.RootCanvas : null;
            if (canvas == null)
            {
                canvas = FindAnyObjectByType<Canvas>();
            }

            if (canvas == null)
            {
                return;
            }

            Transform root = canvas.rootCanvas != null ? canvas.rootCanvas.transform : canvas.transform;
            Transform safeArea = root.Find("SafeArea");
            // Canvas 루트는 CanvasScaler가 위치를 덮어쓰므로 SafeArea를 흔든다.
            Ux.CameraShakeService.SetTarget(safeArea != null ? safeArea : root);
        }

        private void SpawnCoinGainText(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            GameSettingsService settings = AppRoot.Instance?.GameSettings;
            if (settings != null && !settings.CoinNumbersEnabled)
            {
                return;
            }

            string message = $"+{amount}";
            Color color = new Color(1f, 0.85f, 0.2f);
            FloatingTextPool pool = ResolveFloatingTextPool();
            Text bottomCoinLabel = ResolveBottomCoinLabel();
            if (pool != null && bottomCoinLabel != null)
            {
                Vector3 world = bottomCoinLabel.rectTransform.position + Vector3.up * 42f;
                pool.SpawnWorld(message, color, world, null, null, FloatingTextKind.Coin);
                return;
            }

            SpawnFloatingText(message, color, new Vector2(-520f, -820f), FloatingTextKind.Coin);
        }

        private Text ResolveBottomCoinLabel()
        {
            if (_summonPanel == null)
            {
                _summonPanel = FindAnyObjectByType<SummonPanelController>();
            }

            return _summonPanel != null ? _summonPanel.CoinLabel : null;
        }

        private void HandleHpChanged(int current, int max)
        {
            _lastHp = current;
            RefreshTrainHp();
        }

        private void HandlePhaseChanged(RunPhase phase)
        {
            if (phase != RunPhase.Preparing)
            {
                MergeUndoService.Clear();
            }

            RefreshPhase();
            RefreshReadyButton();
            RefreshUndoMergeButton();
            SyncPathDirectionVisibility();
            RefreshThreatPreview();
        }

        private void HandleStationChanged(int _)
        {
            RefreshStationWave();
            SyncPathDirectionVisibility();
            RefreshThreatPreview();
        }

        private void HandleStationStarted(StationData station)
        {
            StationManager stationManager = battleBootstrap != null ? battleBootstrap.StationManager : null;
            if (stationManager == null || station == null)
            {
                return;
            }

            StationBriefing briefing = stationManager.GetCurrentBriefing();
            SetStatus(BuildStationStatusLine(briefing));
            GameAudio.PlaySfx(SfxId.Switch);
            RefreshStationWave();
            RefreshReadyButton();
            SyncPathDirectionVisibility();
            RefreshThreatPreview();
        }

        private static string BuildStationStatusLine(StationBriefing briefing)
        {
            if (briefing == null)
            {
                return "준비 완료를 눌러 전투를 시작하세요.";
            }

            if (!string.IsNullOrWhiteSpace(briefing.StationName)
                && !string.IsNullOrWhiteSpace(briefing.StationTypeLabel))
            {
                return $"{briefing.StationName} · {briefing.StationTypeLabel}";
            }

            if (!string.IsNullOrWhiteSpace(briefing.StationName))
            {
                return briefing.StationName;
            }

            return "준비 완료를 눌러 전투를 시작하세요.";
        }

        private void HandleWaveChanged(int _)
        {
            RefreshStationWave();
            RefreshThreatPreview();
        }

        private void HandleBossSpawned(Enemy.EnemyRuntime boss)
        {
            SetBossVisible(true);
            if (bossNameLabel != null && boss?.Data != null)
            {
                bossNameLabel.text = boss.Data.DisplayName;
            }

            RefreshBossHp(boss);
            SetStatus($"보스 등장: {boss?.Data?.DisplayName}");
        }

        private void HandleBossDespawned(Enemy.EnemyRuntime boss)
        {
            SetBossVisible(false);

            if (boss != null && boss.Resolution == Enemy.EnemyResolution.Killed)
            {
                SetStatus("보스를 처치했습니다!");
            }
            else
            {
                SetStatus(string.Empty);
            }
        }

        private void HandleBossHealthChanged(Enemy.EnemyRuntime boss, float current, float max)
        {
            RefreshBossHp(boss, current, max);
        }

        private void HandleBossPhaseChanged(Enemy.BossPhase previous, Enemy.BossPhase next)
        {
            if (next == Enemy.BossPhase.Enraged)
            {
                SetStatus("보스가 광폭화했다!");
            }
        }

        private void RefreshBossHp(Enemy.EnemyRuntime boss, float? current = null, float? max = null)
        {
            if (boss == null)
            {
                return;
            }

            float cur = current ?? boss.CurrentHealth;
            float mx = max ?? boss.MaxHealth;
            if (bossHpSlider != null)
            {
                bossHpSlider.minValue = 0f;
                bossHpSlider.maxValue = Mathf.Max(1f, mx);
                bossHpSlider.value = cur;
            }

            if (bossHpLabel != null)
            {
                bossHpLabel.text = $"보스 {Mathf.CeilToInt(cur)}/{Mathf.CeilToInt(mx)}";
            }
        }

        private void SetBossVisible(bool visible)
        {
            if (bossHpRoot != null)
            {
                bossHpRoot.SetActive(visible);
            }
        }

        private void RefreshAll()
        {
            RefreshTrainHp();
            RefreshCoins();
            RefreshStationWave();
            RefreshPhase();
            RefreshReadyButton();
            RefreshUndoMergeButton();
            RefreshThreatPreview();
        }

        private void RefreshTrainHp()
        {
            if (_runState?.Train == null)
            {
                return;
            }

            int current = _runState.Train.CurrentHp;
            int max = _runState.Train.MaxHp;
            if (trainHpSlider != null)
            {
                trainHpSlider.minValue = 0f;
                trainHpSlider.maxValue = max;
                trainHpSlider.value = current;
            }

            if (trainHpLabel != null)
            {
                trainHpLabel.text = $"객차 {current}/{max}";
            }
        }

        private void RefreshCoins()
        {
            // 우상단 코인 HUD는 숨김. 실제 수치는 SummonPanelController가 갱신한다.
        }

        private void RefreshStationWave()
        {
            if (_runState?.Station == null)
            {
                return;
            }

            if (stationLabel != null)
            {
                stationLabel.text = $"역 {_runState.Station.CurrentStationIndex}/{Mathf.Max(1, _totalStations)}";
            }

            if (waveLabel != null)
            {
                StationData station = battleBootstrap != null ? battleBootstrap.StationManager?.CurrentStation : null;
                if (station != null && !station.RequiresWaves)
                {
                    waveLabel.text = StationTypeRules.GetNonCombatDescription(station.StationType);
                }
                else
                {
                    int waveDisplay = _runState.Station.CurrentWaveIndex + 1;
                    int waveCount = GetCurrentStationWaveCount();
                    waveLabel.text = waveCount > 0
                        ? $"웨이브 {waveDisplay}/{waveCount}"
                        : $"웨이브 {waveDisplay}";
                }
            }
        }

        private int GetCurrentStationWaveCount()
        {
            StationData station = battleBootstrap != null ? battleBootstrap.StationManager?.CurrentStation : null;
            return station != null ? station.WaveCount : 0;
        }

        private void RefreshPhase()
        {
            if (phaseLabel != null && _runState?.Battle != null)
            {
                phaseLabel.text = PhaseToText(_runState.Battle.CurrentPhase);
            }
        }

        private void RefreshReadyButton()
        {
            if (readyButton == null || _runState?.Battle == null)
            {
                return;
            }

            RunPhase phase = _runState.Battle.CurrentPhase;
            StationManager stationManager = battleBootstrap != null ? battleBootstrap.StationManager : null;
            bool waitingAbility = stationManager != null && stationManager.IsWaitingForAbilityReward
                                  || (_runState.Abilities != null && _runState.Abilities.IsSelectingReward);
            bool canReady = !waitingAbility
                            && (phase == RunPhase.Preparing
                                || phase == RunPhase.StationCompleted
                                || phase == RunPhase.RewardSelecting);
            readyButton.interactable = canReady && !_paused;
        }

        private void SpawnFloatingText(
            string message,
            Color color,
            Vector2 anchoredPos,
            FloatingTextKind kind = FloatingTextKind.Damage)
        {
            FloatingTextPool pool = ResolveFloatingTextPool();
            if (pool != null)
            {
                pool.Spawn(message, color, anchoredPos, kind);
                return;
            }

            GameSettingsService settings = AppRoot.Instance?.GameSettings;
            if (settings != null)
            {
                if (kind == FloatingTextKind.Damage && !settings.DamageNumbersEnabled)
                {
                    return;
                }

                if (kind == FloatingTextKind.Coin && !settings.CoinNumbersEnabled)
                {
                    return;
                }

                if (settings.LowFxMode && UnityEngine.Random.value > 0.4f)
                {
                    return;
                }
            }

            if (floatingTextPrefab == null || floatingTextRoot == null)
            {
                return;
            }

            FloatingCombatText instance = Instantiate(floatingTextPrefab, floatingTextRoot);
            instance.Play(message, color, anchoredPos);
        }

        private FloatingTextPool ResolveFloatingTextPool()
        {
            UiVfxInstaller installer = FindAnyObjectByType<UiVfxInstaller>();
            return installer != null ? installer.FloatingTexts : null;
        }

        public void ShowStatusMessage(string message)
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

        private void RefreshAutoStartCountdown()
        {
            if (_runEndHandled || _paused || _runState?.Battle == null)
            {
                return;
            }

            if (_runState.Battle.CurrentPhase != RunPhase.Preparing)
            {
                return;
            }

            StationManager stationManager = battleBootstrap != null ? battleBootstrap.StationManager : null;
            if (stationManager == null)
            {
                return;
            }

            float remaining = stationManager.AutoStartRemainingSeconds;
            if (remaining < 0f)
            {
                return;
            }

            int seconds = Mathf.CeilToInt(remaining);
            SetStatus($"{seconds}초 후 출발");
        }

        private static string PhaseToText(RunPhase phase)
        {
            return phase switch
            {
                RunPhase.Preparing => "준비",
                RunPhase.WaveStarting => "웨이브 시작",
                RunPhase.Fighting => "전투",
                RunPhase.WaveCompleted => "웨이브 완료",
                RunPhase.StationCompleted => "역 완료",
                RunPhase.RewardSelecting => "보상",
                RunPhase.ShopOpen => "상점",
                RunPhase.EventOpen => "이벤트",
                RunPhase.RunEnded => "종료",
                _ => phase.ToString()
            };
        }
    }
}
