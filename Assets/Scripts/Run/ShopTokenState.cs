using System;

namespace LastTrain.Run
{
    /// <summary>상점 구매로 얻은 일회성·소모성 혜택 토큰.</summary>
    public sealed class ShopTokenState
    {
        public int FreeSummonCharges { get; private set; }
        public int SummonCostReductionStacks { get; private set; }

        public void Reset()
        {
            FreeSummonCharges = 0;
            SummonCostReductionStacks = 0;
        }

        public void AddFreeSummon(int amount)
        {
            if (amount > 0)
            {
                FreeSummonCharges += amount;
            }
        }

        public bool TryConsumeFreeSummon()
        {
            if (FreeSummonCharges <= 0)
            {
                return false;
            }

            FreeSummonCharges--;
            return true;
        }

        public void AddSummonCostReduction(int amount)
        {
            if (amount > 0)
            {
                SummonCostReductionStacks += amount;
            }
        }

        public int ConsumeSummonCostReduction()
        {
            return SummonCostReductionStacks;
        }

        public void Restore(int freeSummonCharges, int summonCostReductionStacks)
        {
            FreeSummonCharges = Math.Max(0, freeSummonCharges);
            SummonCostReductionStacks = Math.Max(0, summonCostReductionStacks);
        }
    }
}
