using System;
using UnityEngine;

namespace LastTrain.Difficulty
{
    /// <summary>난이도 배율을 런타임 계산에만 적용한다.</summary>
    public static class DifficultyCalculator
    {
        public static DifficultyRuntime CreateRuntime(DifficultyData data)
        {
            return DifficultyRuntime.FromData(data);
        }

        public static float CombineLineDifficulty(float stationDifficulty, DifficultyRuntime difficulty)
        {
            float station = stationDifficulty > 0f ? stationDifficulty : 1f;
            float line = difficulty?.EnemyHealthMultiplier ?? 1f;
            return station * line;
        }

        public static float CombineLineDifficulty(
            float stationDifficulty,
            DifficultyRuntime difficulty,
            float enemyHealthBonusMultiplier)
        {
            float bonus = enemyHealthBonusMultiplier > 0f ? enemyHealthBonusMultiplier : 1f;
            return CombineLineDifficulty(stationDifficulty, difficulty) * bonus;
        }

        public static float GetBossLineDifficulty(
            float stationDifficulty,
            DifficultyRuntime difficulty,
            float enemyHealthBonusMultiplier = 1f)
        {
            float combined = CombineLineDifficulty(stationDifficulty, difficulty, enemyHealthBonusMultiplier);
            float boss = difficulty?.BossHealthMultiplier ?? 1f;
            return combined * boss;
        }

        public static int ScaleEnemyCount(int baseCount, DifficultyRuntime difficulty)
        {
            if (baseCount <= 0)
            {
                return 0;
            }

            float multiplier = difficulty?.EnemyCountMultiplier ?? 1f;
            return Math.Max(1, Mathf.CeilToInt(baseCount * multiplier));
        }

        public static float ScaleSpawnInterval(float baseInterval, DifficultyRuntime difficulty)
        {
            float multiplier = difficulty?.SpawnIntervalMultiplier ?? 1f;
            return Math.Max(0f, baseInterval * multiplier);
        }

        public static int ApplyStartingCoins(int configDefault, DifficultyRuntime difficulty)
        {
            if (difficulty == null || difficulty.StartingCoins <= 0)
            {
                return configDefault;
            }

            return difficulty.StartingCoins;
        }

        public static int ApplyStartingTrainHealth(int configDefault, DifficultyRuntime difficulty)
        {
            if (difficulty == null || difficulty.StartingTrainHealth <= 0)
            {
                return configDefault;
            }

            return difficulty.StartingTrainHealth;
        }

        public static int ApplySummonCost(int baseCost, DifficultyRuntime difficulty)
        {
            if (baseCost <= 0)
            {
                return 0;
            }

            float multiplier = difficulty?.SummonCostMultiplier ?? 1f;
            return Math.Max(0, Mathf.CeilToInt(baseCost * multiplier));
        }

        public static int ApplyShopPrice(int basePrice, DifficultyRuntime difficulty, float modifierSellMultiplier = 1f)
        {
            if (basePrice <= 0)
            {
                return 0;
            }

            float shop = difficulty?.ShopPriceMultiplier ?? 1f;
            float combined = shop * Mathf.Max(0.01f, modifierSellMultiplier);
            return Math.Max(0, Mathf.FloorToInt(basePrice * combined));
        }

        public static int ApplyStationReward(int baseReward, DifficultyRuntime difficulty)
        {
            if (baseReward <= 0)
            {
                return 0;
            }

            float multiplier = difficulty?.RewardMultiplier ?? 1f;
            return Math.Max(0, Mathf.FloorToInt(baseReward * multiplier));
        }

        public static int ApplyMetaReward(int baseTickets, DifficultyRuntime difficulty)
        {
            return ApplyMetaReward(baseTickets, difficulty?.RewardMultiplier ?? 1f);
        }

        public static int ApplyMetaReward(int baseTickets, float multiplier)
        {
            if (baseTickets <= 0)
            {
                return 0;
            }

            if (Math.Abs(multiplier - 1f) < 0.001f)
            {
                return baseTickets;
            }

            return Math.Max(0, Mathf.RoundToInt(baseTickets * multiplier));
        }

        public static bool ShouldPromoteToElite(DifficultyRuntime difficulty, System.Random random)
        {
            if (difficulty == null || difficulty.EliteSpawnRate <= 0f)
            {
                return false;
            }

            random ??= new System.Random();
            return random.NextDouble() < difficulty.EliteSpawnRate;
        }
    }
}
