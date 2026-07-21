using System;
using System.Collections.Generic;
using LastTrain.Core;
using LastTrain.Data;
using LastTrain.Run;

namespace LastTrain.Ability
{
    /// <summary>희귀도 가중치 기반 능력 카드 후보 생성.</summary>
    public sealed class AbilityOfferService
    {
        public const float CommonWeight = 0.60f;
        public const float RareWeight = 0.30f;
        public const float LegendaryWeight = 0.10f;

        private readonly IReadOnlyList<AbilityData> _pool;
        private readonly RandomService _random;
        private readonly int _offerCount;

        public AbilityOfferService(
            IReadOnlyList<AbilityData> pool,
            RandomService random,
            int offerCount = 3)
        {
            _pool = pool ?? Array.Empty<AbilityData>();
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _offerCount = Math.Max(1, offerCount);
        }

        public IReadOnlyList<AbilityData> Pool => _pool;

        public List<AbilityData> GenerateOffers(AbilityProgress progress)
        {
            var offers = new List<AbilityData>(_offerCount);
            var eligible = BuildEligible(progress);
            if (eligible.Count == 0)
            {
                return offers;
            }

            var usedIds = new HashSet<string>();
            for (int i = 0; i < _offerCount; i++)
            {
                AbilityData pick = PickOne(eligible, usedIds);
                if (pick == null)
                {
                    break;
                }

                offers.Add(pick);
                usedIds.Add(pick.Id);
            }

            // 후보가 부족하면 이미 뽑힌 것 외 가능 목록에서 중복 허용 보충
            while (offers.Count < _offerCount && eligible.Count > 0)
            {
                AbilityData fallback = eligible[_random.Next(eligible.Count)];
                offers.Add(fallback);
            }

            return offers;
        }

        private List<AbilityData> BuildEligible(AbilityProgress progress)
        {
            var eligible = new List<AbilityData>();
            for (int i = 0; i < _pool.Count; i++)
            {
                AbilityData ability = _pool[i];
                if (ability == null || string.IsNullOrWhiteSpace(ability.Id))
                {
                    continue;
                }

                if (progress != null && !progress.CanSelect(ability))
                {
                    continue;
                }

                eligible.Add(ability);
            }

            return eligible;
        }

        private AbilityData PickOne(List<AbilityData> eligible, HashSet<string> usedIds)
        {
            var unused = new List<AbilityData>();
            for (int i = 0; i < eligible.Count; i++)
            {
                if (!usedIds.Contains(eligible[i].Id))
                {
                    unused.Add(eligible[i]);
                }
            }

            if (unused.Count == 0)
            {
                return null;
            }

            Rarity rarity = RollRarity();
            var byRarity = FilterByRarity(unused, rarity);
            if (byRarity.Count == 0)
            {
                byRarity = unused;
            }

            return byRarity[_random.Next(byRarity.Count)];
        }

        private Rarity RollRarity()
        {
            float roll = _random.NextFloat();
            if (roll < CommonWeight)
            {
                return Rarity.Common;
            }

            if (roll < CommonWeight + RareWeight)
            {
                return Rarity.Rare;
            }

            return Rarity.Legendary;
        }

        private static List<AbilityData> FilterByRarity(List<AbilityData> source, Rarity rarity)
        {
            var filtered = new List<AbilityData>();
            for (int i = 0; i < source.Count; i++)
            {
                if (source[i].Rarity == rarity)
                {
                    filtered.Add(source[i]);
                }
            }

            return filtered;
        }
    }
}
