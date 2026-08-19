using System;
using LastTrain.Ability;
using LastTrain.Audio;
using LastTrain.Data;
using LastTrain.Difficulty;
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
        public event Action<StationData> AbilityRewardRequested;
        public event Action RunVictoryRequested;
        /// <summary>역 클리어 코인 지급 직후. (역, 지급 코인)</summary>
        public event Action<StationData, int> StationRewardGranted;

        private readonly WaveManager _waveManager = new();
        private readonly Func<int, StationData> _stationLookup;
        private readonly NonCombatStationServices _nonCombatServices;

        private RunState _runState;
        private StationData _currentStation;
        private IStationHandler _currentHandler;
        private StationHandlerContext _handlerContext;
        private int _currentWaveIndex;
        private bool _stationCompleteReported;
        private bool _runCancelled;
        private bool _waitingForAbilityReward;
        private float _preparationElapsed;

        /// <summary>false면 준비 시간 자동 시작을 건너뛴다(튜토리얼 등).</summary>
        public Func<bool> CanAutoStartPreparation { get; set; }

        public StationManager(
            Func<int, StationData> stationLookup,
            NonCombatStationServices nonCombatServices = null)
        {
            _stationLookup = stationLookup ?? throw new ArgumentNullException(nameof(stationLookup));
            _nonCombatServices = nonCombatServices;
            _waveManager.WaveCompleted += HandleWaveCompleted;
        }

        public WaveManager WaveManager => _waveManager;
        public float AutoStartRemainingSeconds
        {
            get
            {
                if (!ShouldAutoStartPreparation())
                {
                    return -1f;
                }

                float limit = _runState.DifficultyModifiers.ResolvePreparationTime(_runState.Difficulty);
                return Math.Max(0f, limit - _preparationElapsed);
            }
        }
        public StationData CurrentStation => _currentStation;
        public int CurrentWaveIndex => _currentWaveIndex;
        public bool UsesWaveManager => _currentHandler?.UsesWaveManager ?? true;
        public RunPhase CurrentPhase => _runState?.Battle?.CurrentPhase ?? RunPhase.None;
        public bool IsWaitingForAbilityReward => _waitingForAbilityReward;

        public bool IsWaitingForNonCombatInteraction =>
            (_runState?.Shop.IsActive == true && !_runState.Shop.IsResolved)
            || (_runState?.Events.IsActive == true && !_runState.Events.IsResolved);

        public StationBriefing GetCurrentBriefing()
        {
            if (_currentStation == null || _runState == null)
            {
                return new StationBriefing();
            }

            return StationBriefingBuilder.Build(_currentStation, _runState?.Difficulty, _runState);
        }

        public void Initialize(RunState runState, StationData startingStation)
        {
            _runState = runState ?? throw new ArgumentNullException(nameof(runState));
            _runCancelled = false;
            _waitingForAbilityReward = false;
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
            _waitingForAbilityReward = false;
            _runCancelled = false;
            _preparationElapsed = 0f;
            _currentHandler = StationHandlerFactory.Create(station.StationType);
            _handlerContext = new StationHandlerContext(
                _runState,
                station,
                CompleteStation,
                _nonCombatServices);
            _currentHandler.OnStationEntered(_handlerContext);
            _runState.Station.SetCurrentStation(station.Id, station.StationIndex, station.StationType);
            _runState.Battle.SetPhase(RunPhase.Preparing);
            StationStarted?.Invoke(station);
        }

        /// <summary>Preparing 상태에서 역 유형에 맞는 진행(전투 시작 또는 비전투 처리)을 시도한다.</summary>
        public bool TryActivateStation()
        {
            if (_runCancelled
                || _waitingForAbilityReward
                || IsWaitingForNonCombatInteraction
                || _currentStation == null
                || !_runState.Battle.IsRunActive)
            {
                return false;
            }

            if (_currentHandler != null && !_currentHandler.UsesWaveManager)
            {
                return _currentHandler.TryActivate(_handlerContext);
            }

            return TryStartNextWave();
        }

        /// <summary>Preparing 이후 첫 웨이브 또는 다음 웨이브를 시작한다.</summary>
        public bool TryStartNextWave()
        {
            if (_runCancelled
                || _waitingForAbilityReward
                || _currentStation == null
                || !_runState.Battle.IsRunActive
                || !UsesWaveManager)
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
            GameAudio.PlaySfx(SfxId.WaveStart);
            _runState.Battle.SetPhase(RunPhase.Fighting);
            return true;
        }

        public void Tick(float deltaTime, IBattleFlowContext battleContext)
        {
            if (_runCancelled
                || _waitingForAbilityReward
                || _runState == null
                || !_runState.Battle.IsRunActive
                || !UsesWaveManager)
            {
                return;
            }

            if (_runState.Battle.CurrentPhase == RunPhase.Preparing)
            {
                TickPreparation(deltaTime);
            }

            if (_runState.Battle.CurrentPhase != RunPhase.Fighting || battleContext == null)
            {
                return;
            }

            _waveManager.TickFighting(
                deltaTime,
                battleContext.TrySpawnEnemy,
                battleContext.GetAliveEnemyCount);
        }

        private void TickPreparation(float deltaTime)
        {
            if (!ShouldAutoStartPreparation())
            {
                return;
            }

            float limit = _runState.DifficultyModifiers.ResolvePreparationTime(_runState.Difficulty);
            if (limit < 0f)
            {
                return;
            }

            _preparationElapsed += Math.Max(0f, deltaTime);
            if (_preparationElapsed + 0.0001f >= limit)
            {
                TryActivateStation();
            }
        }

        private bool ShouldAutoStartPreparation()
        {
            if (CanAutoStartPreparation != null && !CanAutoStartPreparation())
            {
                return false;
            }

            float prep = _runState.DifficultyModifiers.ResolvePreparationTime(_runState.Difficulty);
            float baseline = DifficultyRuntime.Identity.PreparationTimeSeconds;
            return prep >= 0f && prep + 0.001f < baseline;
        }

        public void Cancel()
        {
            _runCancelled = true;
            _waitingForAbilityReward = false;
            _waveManager.Cancel();
        }

        /// <summary>능력 카드 선택 완료 후 다음 역으로 진행한다.</summary>
        public bool ContinueAfterAbilityReward()
        {
            if (!_waitingForAbilityReward)
            {
                return false;
            }

            _waitingForAbilityReward = false;
            return TryAdvanceToNextStation();
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
                _runState.Station.MarkCurrentStationCompleted();
                RunVictoryRequested?.Invoke();
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
            int rewardCoins = DifficultyCalculator.ApplyStationReward(
                _currentStation.RewardCoins,
                _runState.Difficulty);
            rewardCoins = UnityEngine.Mathf.RoundToInt(
                rewardCoins * _runState.NextStationModifiers.ConsumeRewardCoinMultiplier());
            rewardCoins += _runState.Relics.Modifiers.StationCompleteCoinBonus;
            _runState.Currency.AddCoins(rewardCoins);
            AbilityEffectApplier.ApplyStationCompleteHeal(_runState);
            GameAudio.PlaySfx(SfxId.StationClear);
            GameAudio.PlaySfx(SfxId.Coin);
            StationCompleted?.Invoke(_currentStation);
            StationRewardGranted?.Invoke(_currentStation, rewardCoins);

            _runState.Battle.SetPhase(RunPhase.RewardSelecting);

            if (_currentStation.GrantsAbilityChoice)
            {
                _waitingForAbilityReward = true;
                AbilityRewardRequested?.Invoke(_currentStation);
                return;
            }

            // 다음 역 준비 단계로 이동. 전투 시작은 UI의 준비 완료 버튼에서 처리한다.
            TryAdvanceToNextStation();
        }
    }
}
