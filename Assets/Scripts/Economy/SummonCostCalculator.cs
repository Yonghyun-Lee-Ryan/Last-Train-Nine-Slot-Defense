using System;
using LastTrain.Data;
using LastTrain.Difficulty;
using LastTrain.Integrations;
using LastTrain.Run;

namespace LastTrain.Economy
{
    /// <summary>소환 비용 계산 순수 로직.</summary>
    public static class SummonCostCalculator
    {
        public static int CalculateCost(
            SummonEconomyConfig config,
            int paidSummonCount,
            int costIncreaseReduction = 0)
        {
            if (config == null)
            {
                return 0;
            }

            RemoteConfigSnapshot remote = RemoteConfigRuntime.Current;
            int baseCost = remote.BaseSummonCost;
            int costIncrease = remote.SummonCostIncrease;

            return Ability.AbilityEffectCalculator.CalculateSummonCost(
                baseCost,
                costIncrease,
                paidSummonCount,
                costIncreaseReduction);
        }

        public static int CalculateCost(SummonEconomyConfig config, RunState runState)
        {
            int reduction = runState?.Abilities?.Modifiers?.SummonCostIncreaseReduction ?? 0;
            reduction += runState?.ShopTokens?.SummonCostReductionStacks ?? 0;
            int count = runState?.Summon?.PaidSummonCount ?? 0;
            int baseCost = CalculateCost(config, count, reduction);
            return DifficultyCalculator.ApplySummonCost(baseCost, runState?.Difficulty);
        }

        public static int CalculateNextCost(SummonEconomyConfig config, int paidSummonCount)
        {
            return CalculateCost(config, paidSummonCount);
        }
    }
}
