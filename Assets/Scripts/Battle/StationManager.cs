using System;
using LastTrain.Data;
using LastTrain.Run;
using LastTrain.Wave;

namespace LastTrain.Battle
{
    /// <summary>
    /// StationData 웨이브 순서·RunPhase 상태 머신을 관리한다.
    /// </summary>
    public sealed class StationManager
    {
        public event Action<StationData> StationStarted;
        public event Action<StationData> StationCompleted;

        private readonly WaveManager _waveManager = new();
        private readonly Func<int, StationData> _stationLookup;

        private RunState _runState;
        private StationData _currentStation;
        private int _currentWaveIndex;
        private bool _stationCompleteReported;
        private bool _runCancelled;

        public StationManager(Func<int, StationData> stationLookup)
        {
            _stationLookup = stationLookup ?? throw new ArgumentNullException(nameof(stationLookup));
            _waveManager.WaveCompleted += HandleWaveCompleted;
        }

        public WaveManager WaveManager => _waveManager;
        public StationData CurrentStation => _currentStation;
        public int CurrentWaveIndex => _currentWaveIndex;
        public RunPhase CurrentPhase => _runState?.Battle?.CurrentPhase ?? RunPhase.None;

        public void Initialize(RunState runState, StationData startingStation)
        {
            _runState = runState ?? throw new ArgumentNullException(nameof(runState));
            _runCancelled = false;
            BeginStation(startingStation);
        }

        public void BeginStation(StationData station)
        {
            if (station == null)
            {
                throw new ArgumentNullException(nameof(station));
            }

            _currentStation = station;
            _currentWaveIndex = 0;
            _stationCompleteReported = false;
            _runState.Station.SetCurrentStation(station.Id, station.StationIndex);
            _runState.Battle.SetPhase(RunPhase.Preparing);
            StationStarted?.Invoke(station);
        }

        /// <summary>Preparing 이후 첫 웨이브 또는 다음 웨이브를 시작한다.</summary>
        public bool TryStartNextWave()
        {
            if (_runCancelled || _currentStation == null || !_runState.Battle.IsRunActive)
            {
                return false;
            }

            if (_currentWaveIndex >= _currentStation.WaveCount)
            {
                CompleteStation();
                return false;
            }

            WaveData wave = _currentStation.Waves[_currentWaveIndex];
            _runState.Battle.SetPhase(RunPhase.WaveStarting);
            _runState.Station.SetWaveIndex(_currentWaveIndex);
            _waveManager.StartWave(_currentWaveIndex, wave);
            _runState.Battle.SetPhase(RunPhase.Fighting);
            return true;
        }

        public void Tick(float deltaTime, IBattleFlowContext battleContext)
        {
            if (_runCancelled
                || _runState == null
                || !_runState.Battle.IsRunActive
                || battleContext == null)
            {
                return;
            }

            if (_runState.Battle.CurrentPhase != RunPhase.Fighting)
            {
                return;
            }

            _waveManager.TickFighting(
                deltaTime,
                battleContext.TrySpawnEnemy,
                battleContext.GetAliveEnemyCount);
        }

        public void Cancel()
        {
            _runCancelled = true;
            _waveManager.Cancel();
        }

        public bool TryAdvanceToNextStation()
        {
            if (_currentStation == null)
            {
                return false;
            }

            int nextIndex = _currentStation.StationIndex + 1;
            StationData nextStation = _stationLookup(nextIndex);
            if (nextStation == null)
            {
                return false;
            }

            _runState.Station.AdvanceToNextStation(nextIndex, nextStation.Id);
            BeginStation(nextStation);
            return true;
        }

        private void HandleWaveCompleted(int waveIndex)
        {
            if (_runCancelled || _runState == null)
            {
                return;
            }

            _runState.Battle.SetPhase(RunPhase.WaveCompleted);
            _currentWaveIndex = waveIndex + 1;

            if (_currentWaveIndex >= _currentStation.WaveCount)
            {
                CompleteStation();
                return;
            }

            TryStartNextWave();
        }

        private void CompleteStation()
        {
            if (_stationCompleteReported || _currentStation == null)
            {
                return;
            }

            _stationCompleteReported = true;
            _runState.Battle.SetPhase(RunPhase.StationCompleted);
            _runState.Currency.AddCoins(_currentStation.RewardCoins);
            StationCompleted?.Invoke(_currentStation);

            _runState.Battle.SetPhase(RunPhase.RewardSelecting);

            if (TryAdvanceToNextStation())
            {
                TryStartNextWave();
            }
        }
    }
}
