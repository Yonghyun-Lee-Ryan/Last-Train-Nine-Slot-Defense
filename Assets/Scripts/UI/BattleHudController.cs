using System;
using LastTrain.Battle;
using LastTrain.Core;
using LastTrain.Data;
using LastTrain.Grid;
using LastTrain.Run;
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

        [Header("Actions")]
        [SerializeField] private Button readyButton;
        [SerializeField] private Button speedButton;
        [SerializeField] private Button pauseButton;
        [SerializeField] private GameObject pauseOverlay;
        [SerializeField] private Button resumeButton;

        private RunState _runState;
        private GameSession _session;
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

            Subscribe();
            WireButtons();
            detailPopup?.Initialize(_runState, OnPassengerSold);
            if (pauseOverlay != null)
            {
                pauseOverlay.SetActive(false);
            }

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
        }

        private void Subscribe()
        {
            if (_runState == null)
            {
                return;
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
        }

        private void Unsubscribe()
        {
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
            bool canReady = phase == RunPhase.Preparing
                            || phase == RunPhase.RewardSelecting
                            || phase == RunPhase.StationCompleted;
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
