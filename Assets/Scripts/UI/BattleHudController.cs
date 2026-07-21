using System;
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
    [DefaultExecutionOrder(60)]
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
                _totalStations = battleBootstrap.GameDatabase.Stations?.Count ?? 5;
            }

            _battleManager = FindAnyObjectByType<BattleManager>();

            Subscribe();
            WireButtons();
            detailPopup?.Initialize(_runState, OnPassengerSold);
            if (pauseOverlay != null)
            {
                pauseOverlay.SetActive(false);
            }

            SetBossVisible(false);
            _lastCoins = _runState.Currency.CurrentCoins;
            _lastHp = _runState.Train.CurrentHp;
            RefreshAll();
            SetStatus("준비 완료를 눌러 전투를 시작하세요.");
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
                || (_runState.Abilities != null && _runState.Abilities.IsSelectingReward))
            {
                SetStatus("능력 카드를 먼저 선택하세요.");
                return;
            }

            if (_runState.Battle.CurrentPhase != RunPhase.Preparing
                && _runState.Battle.CurrentPhase != RunPhase.RewardSelecting
                && _runState.Battle.CurrentPhase != RunPhase.StationCompleted
                && _runState.Battle.CurrentPhase != RunPhase.WaveCompleted)
            {
                SetStatus("지금은 준비할 수 없습니다.");
                return;
            }

            if (_runState.Battle.CurrentPhase == RunPhase.Preparing
                || _runState.Battle.CurrentPhase == RunPhase.RewardSelecting
                || _runState.Battle.CurrentPhase == RunPhase.StationCompleted)
            {
                bool started = stationManager.TryStartNextWave();
                SetStatus(started ? "전투 시작!" : "시작할 웨이브가 없습니다.");
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

            // Unit 16: Preparing 상태에서만 이어하기 저장 생성
            RunSaveSystem.TrySavePreparing(_session);

            if (pauseOverlay != null)
            {
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
            Time.timeScale = _speedScale;
            battleBootstrap?.SetPaused(false);
            if (pauseOverlay != null)
            {
                pauseOverlay.SetActive(false);
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
                int waveDisplay = _runState.Station.CurrentWaveIndex + 1;
                int waveCount = GetCurrentStationWaveCount();
                waveLabel.text = waveCount > 0
                    ? $"웨이브 {waveDisplay}/{waveCount}"
                    : $"웨이브 {waveDisplay}";
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
                                || phase == RunPhase.StationCompleted);
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
                RunPhase.RunEnded => "종료",
                _ => phase.ToString()
            };
        }
    }
}
