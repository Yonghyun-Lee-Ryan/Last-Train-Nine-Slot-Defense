using System;
using LastTrain.Data;
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

            return Ability.AbilityEffectCalculator.CalculateSummonCost(
                config.BaseSummonCost,
                config.SummonCostIncrease,
                paidSummonCount,
                costIncreaseReduction);
        }

        public static int CalculateCost(SummonEconomyConfig config, RunState runState)
        {
            int reduction = runState?.Abilities?.Modifiers?.SummonCostIncreaseReduction ?? 0;
            int count = runState?.Summon?.PaidSummonCount ?? 0;
            return CalculateCost(config, count, reduction);
        }

        public static int CalculateNextCost(SummonEconomyConfig config, int paidSummonCount)
        {
            return CalculateCost(config, paidSummonCount);
        }
    }
}
