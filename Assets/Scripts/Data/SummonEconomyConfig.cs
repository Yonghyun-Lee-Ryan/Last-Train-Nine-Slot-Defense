using UnityEngine;

namespace LastTrain.Data
{
    /// <summary>소환·리롤 경제 설정. ScriptableObject 정적 데이터.</summary>
    [CreateAssetMenu(fileName = "SummonEconomyConfig", menuName = "Last Train/Summon Economy Config")]
    public class SummonEconomyConfig : ScriptableObject
    {
        [Header("Summon Cost")]
        [SerializeField] private int baseSummonCost = 10;
        [SerializeField] private int summonCostIncrease = 2;

        [Header("Offers")]
        [SerializeField] private int offerCount = 3;

        [Header("Reroll")]
        [SerializeField] private int freeRerollsPerRun = 1;
        [SerializeField] private int adRerollsPerRun = 2;

        [Header("Run Start")]
        [SerializeField] private int defaultInitialCoins = 50;

        public int BaseSummonCost => baseSummonCost;
        public int SummonCostIncrease => summonCostIncrease;
        public int OfferCount => Mathf.Max(1, offerCount);
        public int FreeRerollsPerRun => Mathf.Max(0, freeRerollsPerRun);
        public int AdRerollsPerRun => Mathf.Max(0, adRerollsPerRun);
        public int DefaultInitialCoins => Mathf.Max(0, defaultInitialCoins);

        private void OnValidate()
        {
            if (baseSummonCost < 0)
            {
                Debug.LogWarning("[SummonEconomyConfig] baseSummonCost는 0 이상이어야 합니다.", this);
            }

            if (summonCostIncrease < 0)
            {
                Debug.LogWarning("[SummonEconomyConfig] summonCostIncrease는 0 이상이어야 합니다.", this);
            }

            if (offerCount < 1)
            {
                Debug.LogWarning("[SummonEconomyConfig] offerCount는 1 이상이어야 합니다.", this);
            }
        }
    }
}
