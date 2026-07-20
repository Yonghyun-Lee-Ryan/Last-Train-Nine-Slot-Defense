using LastTrain.Battle;
using LastTrain.Core;
using LastTrain.Data;
using LastTrain.Run;
using UnityEngine;

namespace LastTrain.UI
{
    /// <summary>
    /// Game Scene에서 BattleManager·StationManager를 초기화하고 웨이브를 진행한다.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class GameBattleBootstrap : MonoBehaviour
    {
        [SerializeField] private BattleManager battleManager;
        [SerializeField] private Grid.GridManager gridManager;
        [SerializeField] private GameDatabase gameDatabase;

        [Tooltip("true면 첫 웨이브를 자동 시작. HUD 준비 완료 버튼 사용 시 false 권장.")]
        [SerializeField] private bool autoStartFirstWave;

        private StationManager _stationManager;
        private GameSession _gameSession;
        private bool _paused;

        public StationManager StationManager => _stationManager;
        public GameDatabase GameDatabase => gameDatabase;
        public bool IsPaused => _paused;

        public void SetPaused(bool paused)
        {
            _paused = paused;
        }

        private void Start()
        {
            if (battleManager == null)
            {
                Debug.LogError("[GameBattleBootstrap] battleManager가 연결되지 않았습니다.", this);
                return;
            }

            if (gridManager == null)
            {
                gridManager = FindAnyObjectByType<Grid.GridManager>();
            }

            AppRoot appRoot = AppRoot.Instance;
            if (appRoot == null || !appRoot.GameSession.HasActiveRun)
            {
                Debug.LogWarning("[GameBattleBootstrap] 활성 RunState가 없습니다. GameGridBootstrap 이후 실행되도록 순서를 확인하세요.", this);
                return;
            }

            _gameSession = appRoot.GameSession;
            RunState runState = _gameSession.RunState;

            if (gameDatabase == null)
            {
                Debug.LogError("[GameBattleBootstrap] gameDatabase가 연결되지 않았습니다.", this);
                return;
            }

            if (!gameDatabase.TryGetStationByIndex(runState.Station.CurrentStationIndex, out StationData startingStation))
            {
                Debug.LogError(
                    $"[GameBattleBootstrap] stationIndex={runState.Station.CurrentStationIndex} 역 데이터를 찾지 못했습니다.",
                    this);
                return;
            }

            battleManager.Initialize(runState, gridManager);
            battleManager.SetStationDifficulty(startingStation.DifficultyMultiplier);

            _stationManager = new StationManager(ResolveStationByIndex);
            _stationManager.StationStarted += HandleStationStarted;
            _stationManager.Initialize(runState, startingStation);

            _gameSession.RunEnded += HandleRunEnded;

            if (autoStartFirstWave)
            {
                _stationManager.TryStartNextWave();
            }
        }

        private void Update()
        {
            if (_paused || _stationManager == null || _gameSession == null || !_gameSession.HasActiveRun)
            {
                return;
            }

            _stationManager.Tick(Time.deltaTime, battleManager);
        }

        private void OnDestroy()
        {
            if (_gameSession != null)
            {
                _gameSession.RunEnded -= HandleRunEnded;
            }

            if (_stationManager != null)
            {
                _stationManager.StationStarted -= HandleStationStarted;
                _stationManager.Cancel();
            }
        }

        private void HandleStationStarted(StationData station)
        {
            if (station != null && battleManager != null)
            {
                battleManager.SetStationDifficulty(station.DifficultyMultiplier);
            }
        }

        private void HandleRunEnded(RunResult _)
        {
            _stationManager?.Cancel();
            battleManager?.ClearEnemies();
        }

        private StationData ResolveStationByIndex(int stationIndex)
        {
            if (gameDatabase == null)
            {
                return null;
            }

            gameDatabase.TryGetStationByIndex(stationIndex, out StationData station);
            return station;
        }
    }
}
