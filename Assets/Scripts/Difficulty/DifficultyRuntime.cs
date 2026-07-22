using System;
using System.Collections.Generic;

namespace LastTrain.Difficulty
{
    /// <summary>
    /// 회차 시작 시 한 번 생성되는 난이도 스냅샷.
    /// ScriptableObject 원본을 수정하지 않으며, 배율 중복 적용을 방지한다.
    /// </summary>
    public sealed class DifficultyRuntime
    {
        public static DifficultyRuntime Identity { get; } = CreateIdentity();

        public string Id { get; }
        public string DisplayName { get; }
        public float EnemyHealthMultiplier { get; }
        public float EnemyMoveSpeedMultiplier { get; }
        public float EnemyTrainDamageMultiplier { get; }
        public float EnemyCountMultiplier { get; }
        public float SpawnIntervalMultiplier { get; }
        public float EliteSpawnRate { get; }
        public float BossHealthMultiplier { get; }
        public int BossAbilityCount { get; }
        public int StartingCoins { get; }
        public int StartingTrainHealth { get; }
        public float SummonCostMultiplier { get; }
        public float ShopPriceMultiplier { get; }
        public float RewardMultiplier { get; }
        public float PreparationTimeSeconds { get; }
        public IReadOnlyList<DifficultyModifierData> Modifiers { get; }

        private DifficultyRuntime(
            string id,
            string displayName,
            float enemyHealthMultiplier,
            float enemyMoveSpeedMultiplier,
            float enemyTrainDamageMultiplier,
            float enemyCountMultiplier,
            float spawnIntervalMultiplier,
            float eliteSpawnRate,
            float bossHealthMultiplier,
            int bossAbilityCount,
            int startingCoins,
            int startingTrainHealth,
            float summonCostMultiplier,
            float shopPriceMultiplier,
            float rewardMultiplier,
            float preparationTimeSeconds,
            IReadOnlyList<DifficultyModifierData> modifiers)
        {
            Id = id ?? DifficultyIds.Normal;
            DisplayName = displayName ?? Id;
            EnemyHealthMultiplier = enemyHealthMultiplier;
            EnemyMoveSpeedMultiplier = enemyMoveSpeedMultiplier;
            EnemyTrainDamageMultiplier = enemyTrainDamageMultiplier;
            EnemyCountMultiplier = enemyCountMultiplier;
            SpawnIntervalMultiplier = spawnIntervalMultiplier;
            EliteSpawnRate = eliteSpawnRate;
            BossHealthMultiplier = bossHealthMultiplier;
            BossAbilityCount = bossAbilityCount;
            StartingCoins = startingCoins;
            StartingTrainHealth = startingTrainHealth;
            SummonCostMultiplier = summonCostMultiplier;
            ShopPriceMultiplier = shopPriceMultiplier;
            RewardMultiplier = rewardMultiplier;
            PreparationTimeSeconds = preparationTimeSeconds;
            Modifiers = modifiers ?? Array.Empty<DifficultyModifierData>();
        }

        public static DifficultyRuntime FromData(DifficultyData data)
        {
            if (data == null)
            {
                return Identity;
            }

            return new DifficultyRuntime(
                data.Id,
                data.DisplayName,
                data.EnemyHealthMultiplier,
                data.EnemyMoveSpeedMultiplier,
                data.EnemyTrainDamageMultiplier,
                data.EnemyCountMultiplier,
                data.SpawnIntervalMultiplier,
                data.EliteSpawnRate,
                data.BossHealthMultiplier,
                data.BossAbilityCount,
                data.StartingCoins,
                data.StartingTrainHealth,
                data.SummonCostMultiplier,
                data.ShopPriceMultiplier,
                data.RewardMultiplier,
                data.PreparationTime,
                data.AllowedModifiers);
        }

        public static DifficultyRuntime CreateIdentity()
        {
            return new DifficultyRuntime(
                DifficultyIds.Normal,
                "일반 막차",
                enemyHealthMultiplier: 1f,
                enemyMoveSpeedMultiplier: 1f,
                enemyTrainDamageMultiplier: 1f,
                enemyCountMultiplier: 1f,
                spawnIntervalMultiplier: 1f,
                eliteSpawnRate: 0f,
                bossHealthMultiplier: 1f,
                bossAbilityCount: 0,
                startingCoins: 0,
                startingTrainHealth: 0,
                summonCostMultiplier: 1f,
                shopPriceMultiplier: 1f,
                rewardMultiplier: 1f,
                preparationTimeSeconds: 5f,
                modifiers: Array.Empty<DifficultyModifierData>());
        }
    }
}
