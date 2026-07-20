using LastTrain.Data;

namespace LastTrain.Economy
{
    /// <summary>소환 비용 계산 순수 로직.</summary>
    public static class SummonCostCalculator
    {
        public static int CalculateCost(SummonEconomyConfig config, int paidSummonCount)
        {
            if (config == null)
            {
                return 0;
            }

            int count = paidSummonCount < 0 ? 0 : paidSummonCount;
            return config.BaseSummonCost + config.SummonCostIncrease * count;
        }

        public static int CalculateNextCost(SummonEconomyConfig config, int paidSummonCount)
        {
            return CalculateCost(config, paidSummonCount);
        }
    }
}
