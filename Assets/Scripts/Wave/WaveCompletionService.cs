using System.Collections.Generic;
using LastTrain.Data;

namespace LastTrain.Wave
{
    /// <summary>웨이브 완료 판정 순수 로직.</summary>
    public static class WaveCompletionService
    {
        public static bool IsWaveComplete(
            int totalEnemiesInWave,
            int spawnedCount,
            int remainingScheduled,
            int aliveEnemyCount)
        {
            if (totalEnemiesInWave <= 0)
            {
                return remainingScheduled == 0 && aliveEnemyCount <= 0;
            }

            return remainingScheduled == 0
                   && spawnedCount >= totalEnemiesInWave
                   && aliveEnemyCount <= 0;
        }
    }
}
