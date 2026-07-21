using System;
using System.Collections.Generic;
using LastTrain.Data;
using LastTrain.Run;

namespace LastTrain.Ability
{
    /// <summary>능력 카드 목록으로부터 회차 수정치를 합산한다.</summary>
    public static class AbilityEffectCalculator
    {
        public const string AbilityBuffIdPrefix = "ability:";

        public static AbilityModifiers Compute(IReadOnlyList<AbilityData> abilities)
        {
            var modifiers = new AbilityModifiers();
            if (abilities == null)
            {
                return modifiers;
            }

            for (int i = 0; i < abilities.Count; i++)
            {
                AbilityData ability = abilities[i];
                if (ability == null)
                {
                    continue;
                }

                ApplyOne(modifiers, ability);
            }

            return modifiers;
        }

        public static int ApplyPercentBonus(int baseValue, float percentBonus)
        {
            if (baseValue <= 0)
            {
                return 0;
            }

            float result = baseValue * (1f + percentBonus / 100f);
            return Math.Max(0, (int)Math.Round(result));
        }

        public static int CalculateSummonCost(
            int baseCost,
            int costIncrease,
            int paidSummonCount,
            int costIncreaseReduction)
        {
            int count = paidSummonCount < 0 ? 0 : paidSummonCount;
            int increase = Math.Max(0, costIncrease - Math.Max(0, costIncreaseReduction));
            return Math.Max(0, baseCost + increase * count);
        }

        private static void ApplyOne(AbilityModifiers modifiers, AbilityData ability)
        {
            float value = ability.EffectValue;
            switch (ability.EffectType)
            {
                case AbilityEffectType.PassengerAttackPercent:
                    if (string.IsNullOrWhiteSpace(ability.TargetPassengerId))
                    {
                        modifiers.GlobalAttackPercent += value;
                    }
                    else
                    {
                        AddToMap(modifiers.PassengerAttackPercentById, ability.TargetPassengerId, value);
                    }

                    break;

                case AbilityEffectType.PassengerAttackSpeedPercent:
                    if (string.IsNullOrWhiteSpace(ability.TargetPassengerId))
                    {
                        modifiers.GlobalAttackSpeedPercent += value;
                    }
                    else
                    {
                        AddToMap(modifiers.PassengerAttackSpeedPercentById, ability.TargetPassengerId, value);
                    }

                    break;

                case AbilityEffectType.TrainMaxHpFlat:
                    modifiers.TrainMaxHpFlat += (int)Math.Round(value);
                    break;

                case AbilityEffectType.TrainHealOnStationComplete:
                    modifiers.TrainHealOnStationComplete += (int)Math.Round(value);
                    break;

                case AbilityEffectType.CoinOnKillPercent:
                    modifiers.CoinOnKillPercent += value;
                    break;

                case AbilityEffectType.SellPricePercent:
                    modifiers.SellPricePercent += value;
                    break;

                case AbilityEffectType.SummonCostIncreaseReduction:
                    modifiers.SummonCostIncreaseReduction += (int)Math.Round(value);
                    break;

                case AbilityEffectType.FrontRowAttackPercent:
                    modifiers.FrontRowAttackPercent += value;
                    break;

                case AbilityEffectType.SameRoleAttackSpeedPercent:
                    modifiers.SameRoleAttackSpeedPercent += value;
                    break;

                case AbilityEffectType.DiversePassengerDamagePercent:
                    modifiers.DiversePassengerDamagePercent += value;
                    break;

                case AbilityEffectType.NurseHealPercent:
                    modifiers.NurseHealPercent += value;
                    break;

                case AbilityEffectType.PoliceBossDamagePercent:
                    modifiers.PoliceBossDamagePercent += value;
                    break;

                case AbilityEffectType.CatCritChancePercent:
                    modifiers.CatCritChancePercent += value;
                    break;
            }
        }

        private static void AddToMap(Dictionary<string, float> map, string key, float value)
        {
            if (map.TryGetValue(key, out float existing))
            {
                map[key] = existing + value;
            }
            else
            {
                map[key] = value;
            }
        }
    }
}
