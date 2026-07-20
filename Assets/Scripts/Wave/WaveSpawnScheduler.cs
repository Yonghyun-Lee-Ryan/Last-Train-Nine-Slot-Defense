using System;
using System.Collections.Generic;
using LastTrain.Data;

namespace LastTrain.Wave
{
    /// <summary>
    /// WaveSpawnData를 시간순 스폰 큐로 변환하고 deltaTime 기반으로 처리한다.
    /// </summary>
    public sealed class WaveSpawnScheduler
    {
        private const float SpawnTimeTolerance = 0.0001f;

        private readonly struct SpawnEntry
        {
            public SpawnEntry(float atTime, EnemyData enemy)
            {
                AtTime = atTime;
                Enemy = enemy;
            }

            public float AtTime { get; }
            public EnemyData Enemy { get; }
        }

        private readonly List<SpawnEntry> _entries = new();
        private int _nextIndex;
        private float _elapsed;

        public int TotalPlanned { get; private set; }
        public int SpawnedCount { get; private set; }
        public int RemainingScheduled => _entries.Count - _nextIndex;

        public void Reset(WaveData wave)
        {
            _entries.Clear();
            _nextIndex = 0;
            _elapsed = 0f;
            SpawnedCount = 0;
            TotalPlanned = 0;

            if (wave == null || wave.Spawns == null)
            {
                return;
            }

            float waveDelay = Math.Max(0f, wave.DelayBeforeStart);
            IReadOnlyList<WaveSpawnData> spawns = wave.Spawns;

            for (int groupIndex = 0; groupIndex < spawns.Count; groupIndex++)
            {
                WaveSpawnData group = spawns[groupIndex];
                if (group.enemy == null || group.count < 1)
                {
                    continue;
                }

                float groupStart = waveDelay + Math.Max(0f, group.spawnDelay);
                float interval = Math.Max(0f, group.spawnInterval);

                for (int i = 0; i < group.count; i++)
                {
                    float atTime = groupStart + interval * i;
                    _entries.Add(new SpawnEntry(atTime, group.enemy));
                }
            }

            _entries.Sort((a, b) => a.AtTime.CompareTo(b.AtTime));
            TotalPlanned = _entries.Count;
        }

        /// <summary>
        /// deltaTime만큼 진행하고, 예약 시각이 지난 적을 output에 추가한다.
        /// </summary>
        /// <returns>이번 Tick에서 스폰할 적 수.</returns>
        public int Tick(float deltaTime, List<EnemyData> output)
        {
            output?.Clear();
            if (output == null || deltaTime <= 0f || _nextIndex >= _entries.Count)
            {
                return 0;
            }

            _elapsed += deltaTime;
            int spawnedThisTick = 0;

            while (_nextIndex < _entries.Count
                   && _elapsed + SpawnTimeTolerance >= _entries[_nextIndex].AtTime)
            {
                output.Add(_entries[_nextIndex].Enemy);
                _nextIndex++;
                SpawnedCount++;
                spawnedThisTick++;
            }

            return spawnedThisTick;
        }
    }
}
