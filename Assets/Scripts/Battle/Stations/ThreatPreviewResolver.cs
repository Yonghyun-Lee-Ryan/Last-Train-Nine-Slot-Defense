using System.Collections.Generic;
using LastTrain.Data;
using LastTrain.Run;

namespace LastTrain.Battle
{
    /// <summary>다음 웨이브에 등장할 적 한 종류. HUD 아이콘 티커용.</summary>
    public readonly struct ThreatPreviewEntry
    {
        public ThreatPreviewEntry(string enemyId, EnemyType enemyType, int count)
        {
            EnemyId = enemyId ?? string.Empty;
            EnemyType = enemyType;
            Count = count;
        }

        public string EnemyId { get; }
        public EnemyType EnemyType { get; }
        public int Count { get; }
    }

    /// <summary>
    /// 준비 단계에는 곧 시작할 웨이브, 전투 중에는 그 다음 웨이브의 적을 모은다.
    /// </summary>
    public static class ThreatPreviewResolver
    {
        public static int ResolvePreviewWaveIndex(int currentWaveIndex, RunPhase phase, int waveCount)
        {
            if (waveCount <= 0)
            {
                return -1;
            }

            bool showCurrentSlot = phase == RunPhase.Preparing
                                   || phase == RunPhase.WaveCompleted
                                   || phase == RunPhase.None;
            int preview = showCurrentSlot ? currentWaveIndex : currentWaveIndex + 1;
            if (preview < 0 || preview >= waveCount)
            {
                return -1;
            }

            return preview;
        }

        public static IReadOnlyList<ThreatPreviewEntry> ResolveUpcoming(
            StationData station,
            int currentWaveIndex,
            RunPhase phase)
        {
            if (station == null || !station.RequiresWaves || station.Waves == null)
            {
                return System.Array.Empty<ThreatPreviewEntry>();
            }

            int previewIndex = ResolvePreviewWaveIndex(currentWaveIndex, phase, station.WaveCount);
            if (previewIndex < 0)
            {
                return System.Array.Empty<ThreatPreviewEntry>();
            }

            return CollectWaveEntries(station.Waves[previewIndex]);
        }

        public static IReadOnlyList<ThreatPreviewEntry> CollectWaveEntries(WaveData wave)
        {
            if (wave?.Spawns == null || wave.Spawns.Count == 0)
            {
                return System.Array.Empty<ThreatPreviewEntry>();
            }

            var order = new List<string>();
            var counts = new Dictionary<string, ThreatPreviewEntry>();
            for (int i = 0; i < wave.Spawns.Count; i++)
            {
                WaveSpawnData spawn = wave.Spawns[i];
                EnemyData enemy = spawn.enemy;
                if (enemy == null || string.IsNullOrWhiteSpace(enemy.Id))
                {
                    continue;
                }

                int add = spawn.count > 0 ? spawn.count : 0;
                if (add <= 0)
                {
                    continue;
                }

                if (counts.TryGetValue(enemy.Id, out ThreatPreviewEntry existing))
                {
                    counts[enemy.Id] = new ThreatPreviewEntry(existing.EnemyId, existing.EnemyType, existing.Count + add);
                    continue;
                }

                order.Add(enemy.Id);
                counts[enemy.Id] = new ThreatPreviewEntry(enemy.Id, enemy.EnemyType, add);
            }

            var result = new ThreatPreviewEntry[order.Count];
            for (int i = 0; i < order.Count; i++)
            {
                result[i] = counts[order[i]];
            }

            return result;
        }
    }
}
