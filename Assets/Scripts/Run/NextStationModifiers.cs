namespace LastTrain.Run
{
    /// <summary>다음 전투 역에만 적용되는 일시 수정치.</summary>
    public sealed class NextStationModifiers
    {
        public float EnemyHealthMultiplier { get; private set; } = 1f;
        public float RewardCoinMultiplier { get; private set; } = 1f;

        public void Reset()
        {
            EnemyHealthMultiplier = 1f;
            RewardCoinMultiplier = 1f;
        }

        public void MultiplyEnemyHealth(float multiplier)
        {
            if (multiplier > 0f)
            {
                EnemyHealthMultiplier *= multiplier;
            }
        }

        public void MultiplyRewardCoins(float multiplier)
        {
            if (multiplier > 0f)
            {
                RewardCoinMultiplier *= multiplier;
            }
        }

        public void Consume()
        {
            Reset();
        }

        public float ConsumeEnemyHealthMultiplier()
        {
            float value = EnemyHealthMultiplier;
            EnemyHealthMultiplier = 1f;
            return value;
        }

        public float ConsumeRewardCoinMultiplier()
        {
            float value = RewardCoinMultiplier;
            RewardCoinMultiplier = 1f;
            return value;
        }

        public void Restore(float enemyHealthMultiplier, float rewardCoinMultiplier)
        {
            EnemyHealthMultiplier = enemyHealthMultiplier > 0f ? enemyHealthMultiplier : 1f;
            RewardCoinMultiplier = rewardCoinMultiplier > 0f ? rewardCoinMultiplier : 1f;
        }
    }
}
