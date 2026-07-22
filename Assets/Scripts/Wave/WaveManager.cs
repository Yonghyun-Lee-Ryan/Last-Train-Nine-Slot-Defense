using System;
using System.Collections.Generic;
using LastTrain.Data;
using LastTrain.Difficulty;

namespace LastTrain.Wave
{
    /// <summary>단일 웨이브의 스폰·완료를 관리한다.</summary>
    public sealed class WaveManager
    {
        public event Action<int> WaveStarted;
        public event Action<int> WaveCompleted;

        private readonly WaveSpawnScheduler _scheduler = new();
        private readonly List<EnemyData> _spawnBuffer = new();

        private WaveData _currentWave;
        private int _waveIndex = -1;
        private bool _waveCompleteReported;
        private DifficultyRuntime _difficulty;

        public int CurrentWaveIndex => _waveIndex;
        public WaveData CurrentWave => _currentWave;
        public int SpawnedCount => _scheduler.SpawnedCount;
        public int RemainingScheduled => _scheduler.RemainingScheduled;

        public void SetDifficulty(DifficultyRuntime difficulty)
        {
            _difficulty = difficulty;
        }

        public void StartWave(int waveIndex, WaveData wave)
        {
            _waveIndex = waveIndex;
            _currentWave = wave;
            _waveCompleteReported = false;
            _scheduler.Reset(wave, _difficulty);
            WaveStarted?.Invoke(waveIndex);
        }

        public void Cancel()
        {
            _scheduler.Reset(null);
            _currentWave = null;
            _waveIndex = -1;
            _waveCompleteReported = false;
        }

        /// <summary>Fighting 단계에서 호출한다.</summary>
        public void TickFighting(
            float deltaTime,
            Func<EnemyData, bool> trySpawn,
            Func<int> getAliveEnemyCount)
        {
            if (_currentWave == null || _waveCompleteReported || trySpawn == null || getAliveEnemyCount == null)
            {
                return;
            }

            int spawnedThisTick = _scheduler.Tick(deltaTime, _spawnBuffer);
            for (int i = 0; i < spawnedThisTick; i++)
            {
                trySpawn(_spawnBuffer[i]);
            }

            if (!WaveCompletionService.IsWaveComplete(
                    _scheduler.TotalPlanned,
                    _scheduler.SpawnedCount,
                    _scheduler.RemainingScheduled,
                    getAliveEnemyCount()))
            {
                return;
            }

            _waveCompleteReported = true;
            WaveCompleted?.Invoke(_waveIndex);
        }
    }
}
