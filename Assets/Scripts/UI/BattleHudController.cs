using System;
using LastTrain.Audio;
using LastTrain.Battle;
using LastTrain.Core;
using LastTrain.Data;
using LastTrain.Grid;
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
        [SerializeField] private Button pauseButton;
        [SerializeField] private GameObject pauseOverlay;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button mainMenuFromPauseButton;

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

            Subscribe();
            EnsurePauseOverlayButtons();
            WireButtons();
            ApplyHudTheme();
            detailPopup?.Initialize(_runState, OnPassengerSold);
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

        private void ApplyHudTheme()
        {
            VisualTheme theme = VisualThemeLocator.Load();
            if (theme == null)
            {
                return;
            }

            UiButtonStyler.ApplyStandardTheme(readyButton);
            UiButtonStyler.ApplyStandardTheme(speedButton);
            UiButtonStyler.ApplyStandardTheme(pauseButton);
            UiButtonStyler.ApplyStandardTheme(resumeButton);

            AttachLabelIcon(coinLabel, theme.IconCoin);
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
            }
        }

        private static void AttachLabelIcon(Text label, Sprite sprite)
        {
            if (label == null || sprite == null || label.transform.Find("ThemeIcon") != null)
            {
                return;
            }

            var go = new GameObject("ThemeIcon", typeof(RectTransform), typeof(Image));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(label.transform, false);
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(-8f, 0f);
            rect.sizeDelta = new Vector2(36f, 36f);
            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = Color.white;

            RectTransform labelRect = label.rectTransform;
            labelRect.offsetMin = new Vector2(labelRect.offsetMin.x + 28f, labelRect.offsetMin.y);
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
        }

        private void OnReadyClicked()
        {
            if (!_inputGuard.TryAcquire() || _paused)
            {
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

            _speedScale = Mathf.Approximately(_speedScale, 1f) ? 2f : 1f;
            Time.timeScale = _speedScale;
            GameAudio.PlaySfx(SfxId.Switch);
            if (speedButton != null)
            {
                Text label = speedButton.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = $"{_speedScale:0}x";
                }
            }
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

            if (pauseOverlay != null)
            {
                pauseOverlay.transform.SetAsLastSibling();
                pauseOverlay.SetActive(true);
            }

            HideAbilityOwnedLabel(true);
        }

        private void OnResumeClicked()
        {
            if (!_inputGuard.TryAcquire())
            {
                return;
            }

            _paused = false;
            Time.timeScale = _speedScale;
            battleBootstrap?.SetPaused(false);
            GameAudio.PlaySfx(SfxId.Resume);
            if (pauseOverlay != null)
            {
                pauseOverlay.SetActive(false);
            }

            HideAbilityOwnedLabel(false);
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
                    new Vector2(0f, -160f));
            }

            if (resumeButton != null)
            {
                RectTransform resumeRect = resumeButton.GetComponent<RectTransform>();
                if (resumeRect != null)
                {
                    resumeRect.anchoredPosition = new Vector2(0f, -20f);
                }

                UiButtonStyler.ApplyStandardTheme(resumeButton);
            }

            UiButtonStyler.ApplyStandardTheme(mainMenuFromPauseButton);

            Transform title = pauseOverlay.transform.Find("PauseTitle");
            if (title is RectTransform titleRect)
            {
                titleRect.anchoredPosition = new Vector2(0f, 140f);
            }
        }

        private static Button CreatePauseMenuButton(Transform parent, string name, string label, Vector2 anchoredPos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(320f, 100f);

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

        private static void HideAbilityOwnedLabel(bool hide)
        {
            AbilityPanelController panel = UnityEngine.Object.FindAnyObjectByType<AbilityPanelController>();
            if (panel == null)
            {
                return;
            }

            Transform owned = panel.transform.Find("AbilityOwnedListLabel");
            if (owned != null)
            {
                owned.gameObject.SetActive(!hide);
            }
        }

        private void HandlePassengerSelected(int slotIndex)
        {
            if (_paused || slotIndex < 0)
            {
                return;
            }

            detailPopup?.Show(slotIndex);
        }

        private void HandleRunEnded(RunResult _)
        {
            if (_runEndHandled)
            {
                return;
            }

            _runEndHandled = true;
            _paused = true;
            battleBootstrap?.SetPaused(true);
            Time.timeScale = 1f;

            if (readyButton != null) readyButton.interactable = false;
            if (speedButton != null) speedButton.interactable = false;
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
            SpawnFloatingText($"+{coins}", new Color(1f, 0.85f, 0.2f), new Vector2(0f, 120f));
            SetStatus($"판매 완료 (+{coins})");
            RefreshAll();
        }

        private void HandleCoinsChanged(int coins)
        {
            if (_lastCoins >= 0 && coins > _lastCoins)
            {
                SpawnFloatingText($"+{coins - _lastCoins}", new Color(1f, 0.85f, 0.2f), new Vector2(180f, 200f));
            }

            _lastCoins = coins;
            RefreshCoins();
        }

        private void HandleHpChanged(int current, int max)
        {
            if (_lastHp >= 0 && current < _lastHp)
            {
                SpawnFloatingText($"-{_lastHp - current}", new Color(1f, 0.35f, 0.35f), new Vector2(-180f, 200f));
            }

            _lastHp = current;
            RefreshTrainHp();
        }

        private void HandlePhaseChanged(RunPhase _)
        {
            RefreshPhase();
            RefreshReadyButton();
        }

        private void HandleStationChanged(int _)
        {
            RefreshStationWave();
        }

        private void HandleStationStarted(StationData station)
        {
            StationManager stationManager = battleBootstrap != null ? battleBootstrap.StationManager : null;
            if (stationManager == null)
            {
                return;
            }

            StationBriefing briefing = stationManager.GetCurrentBriefing();
            SetStatus(briefing.BuildDisplayText());
            GameAudio.PlaySfx(SfxId.Switch);
            RefreshStationWave();
            RefreshReadyButton();
        }

        private void HandleWaveChanged(int _)
        {
            RefreshStationWave();
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
            if (coinLabel != null && _runState?.Currency != null)
            {
                coinLabel.text = $"코인 {_runState.Currency.CurrentCoins}";
            }
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

        private void SpawnFloatingText(string message, Color color, Vector2 anchoredPos)
        {
            if (floatingTextPrefab == null || floatingTextRoot == null)
            {
                return;
            }

            FloatingCombatText instance = Instantiate(floatingTextPrefab, floatingTextRoot);
            instance.Play(message, color, anchoredPos);
        }

        private void SetStatus(string message)
        {
            if (statusLabel != null)
            {
                statusLabel.text = message ?? string.Empty;
            }
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
