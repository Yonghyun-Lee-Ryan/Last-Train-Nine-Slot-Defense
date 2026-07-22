using System.Collections.Generic;
using LastTrain.Data;

namespace LastTrain.Relic
{
    public static class RelicEffectAggregator
    {
        public static RelicModifiers Compute(IReadOnlyList<RelicRuntime> relics)
        {
            var modifiers = new RelicModifiers();
            if (relics == null)
            {
                return modifiers;
            }

            for (int i = 0; i < relics.Count; i++)
            {
                RelicData data = relics[i]?.Data;
                if (data == null)
                {
                    continue;
                }

                Apply(data, modifiers);
            }

            return modifiers;
        }

        private static void Apply(RelicData data, RelicModifiers modifiers)
        {
            float value = data.EffectValue;
            switch (data.EffectType)
            {
                case RelicEffectType.FirstSummonFree:
                    modifiers.FirstSummonFree = true;
                    break;

                case RelicEffectType.OfficeWorkerAttackSpeedPercent:
                    modifiers.OfficeWorkerAttackSpeedPercent += value;
                    break;

                case RelicEffectType.DeveloperTurretDurationPercent:
                    modifiers.DeveloperTurretDurationPercent += value;
                    break;

                case RelicEffectType.StationCompleteCoinBonus:
                    modifiers.StationCompleteCoinBonus += (int)value;
                    break;

                case RelicEffectType.CritChancePercent:
                    modifiers.CritChancePercent += value;
                    break;

                case RelicEffectType.TrainMaxHpFlat:
                    modifiers.TrainMaxHpFlat += (int)value;
                    break;

                case RelicEffectType.SellPricePercent:
                    modifiers.SellPricePercent += value;
                    break;

                case RelicEffectType.BossFirstActionDelaySeconds:
                    modifiers.BossFirstActionDelaySeconds += value;
                    break;

                case RelicEffectType.EmergencyAutoHealFlat:
                    modifiers.EmergencyAutoHealFlat += (int)value;
                    break;

                case RelicEffectType.EventBadOutcomeReductionPercent:
                    modifiers.EventBadOutcomeReductionPercent += value;
                    break;
            }
        }
    }
}
