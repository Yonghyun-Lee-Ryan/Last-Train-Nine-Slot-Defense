using LastTrain.Data;
using LastTrain.Difficulty;
using UnityEngine;

namespace LastTrain.Enemy
{
    /// <summary>EnemyData 기반 EnemyRuntime 생성.</summary>
    public static class EnemyFactory
    {
        public static EnemyRuntime CreateRuntime(
            EnemyData data,
            Vector2 spawnPosition,
            float stationDifficulty = 1f,
            DifficultyRuntime difficulty = null,
            string instanceId = null)
        {
            if (data == null)
            {
                throw new System.ArgumentNullException(nameof(data));
            }

            float lineMultiplier = difficulty?.EnemyHealthMultiplier ?? 1f;
            if (data.EnemyType == EnemyType.Boss)
            {
                lineMultiplier *= difficulty?.BossHealthMultiplier ?? 1f;
            }

            float maxHealth = data.GetScaledHealth(stationDifficulty, lineMultiplier);
            var runtime = new EnemyRuntime(data, maxHealth, spawnPosition, instanceId);

            if (difficulty != null)
            {
                runtime.MoveSpeedMultiplier = difficulty.EnemyMoveSpeedMultiplier;
                runtime.TrainDamageMultiplier = difficulty.EnemyTrainDamageMultiplier;
            }

            return runtime;
        }
    }
}
