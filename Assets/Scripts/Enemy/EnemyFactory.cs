using LastTrain.Data;
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
            float lineDifficulty = 1f,
            string instanceId = null)
        {
            if (data == null)
            {
                throw new System.ArgumentNullException(nameof(data));
            }

            float maxHealth = data.GetScaledHealth(stationDifficulty, lineDifficulty);
            return new EnemyRuntime(data, maxHealth, spawnPosition, instanceId);
        }
    }
}
