using System.Collections.Generic;
using LastTrain.Data;
using LastTrain.Run;

namespace LastTrain.Synergy
{
    /// <summary>시너지 조건 판정·수정치 합산. EditMode 테스트 가능.</summary>
    public static class SynergyEffectCalculator
    {
        public const string SynergyBuffIdPrefix = "synergy:";

        public static SynergyEvaluation Evaluate(IReadOnlyList<SynergyData> catalog, RunState runState)
        {
            var active = new List<SynergyData>();
            var modifiers = new SynergyModifiers();
            var seenIds = new HashSet<string>();

            if (catalog == null || runState == null)
            {
                return new SynergyEvaluation(active, modifiers);
            }

            for (int i = 0; i < catalog.Count; i++)
            {
                SynergyData data = catalog[i];
                if (data == null
                    || string.IsNullOrWhiteSpace(data.Id)
                    || !seenIds.Add(data.Id)
                    || !IsActive(data, runState))
                {
                    continue;
                }

                active.Add(data);
                ApplyEffect(data, modifiers);
            }

            return new SynergyEvaluation(active, modifiers);
        }

        public static bool IsActive(SynergyData data, RunState runState)
        {
            if (data == null || runState == null)
            {
                return false;
            }

            bool hasTagCondition = data.RequiredTags != PassengerTag.None;
            bool hasUniqueCondition = data.RequiredUniquePassengerCount > 0;

            if (!hasTagCondition && !hasUniqueCondition)
            {
                return false;
            }

            if (hasTagCondition && !MatchesTagCondition(data, runState))
            {
                return false;
            }

            if (hasUniqueCondition
                && CountUniquePassengerTypes(runState) < data.RequiredUniquePassengerCount)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// requiredTags의 모든 비트가 그리드에 한 번 이상 등장하고,
        /// 해당 태그와 겹치는 승객 수가 requiredCount 이상인지 판정한다.
        /// </summary>
        public static bool MatchesTagCondition(SynergyData data, RunState runState)
        {
            if (data == null || data.RequiredTags == PassengerTag.None)
            {
                return true;
            }

            int matching = 0;
            PassengerTag covered = PassengerTag.None;

            for (int slot = 0; slot < RunState.GridSlotCount; slot++)
            {
                PassengerRuntime passenger = runState.GetPassengerAtSlot(slot);
                if (passenger?.Data == null)
                {
                    continue;
                }

                PassengerTag overlap = passenger.Data.Tags & data.RequiredTags;
                if (overlap == PassengerTag.None)
                {
                    continue;
                }

                matching++;
                covered |= overlap;
            }

            if (covered != data.RequiredTags)
            {
                return false;
            }

            return data.RequiredCount <= 0 || matching >= data.RequiredCount;
        }

        public static int CountUniquePassengerTypes(RunState runState)
        {
            var ids = new HashSet<string>();
            if (runState == null)
            {
                return 0;
            }

            for (int i = 0; i < RunState.GridSlotCount; i++)
            {
                PassengerRuntime passenger = runState.GetPassengerAtSlot(i);
                if (passenger?.Data != null)
                {
                    ids.Add(passenger.Data.Id);
                }
            }

            return ids.Count;
        }

        public static int CountMatchingPassengers(RunState runState, PassengerTag requiredTags)
        {
            if (runState == null || requiredTags == PassengerTag.None)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < RunState.GridSlotCount; i++)
            {
                PassengerRuntime passenger = runState.GetPassengerAtSlot(i);
                if (passenger?.Data == null)
                {
                    continue;
                }

                if ((passenger.Data.Tags & requiredTags) != PassengerTag.None)
                {
                    count++;
                }
            }

            return count;
        }

        private static void ApplyEffect(SynergyData data, SynergyModifiers modifiers)
        {
            switch (data.EffectType)
            {
                case SynergyEffectType.AttackSpeedPercent:
                    modifiers.GlobalAttackSpeedPercent += data.EffectValue;
                    break;
                case SynergyEffectType.AllAttackPercent:
                    modifiers.GlobalAttackPercent += data.EffectValue;
                    break;
                case SynergyEffectType.TrainHealPercent:
                    modifiers.TrainHealPercent += data.EffectValue;
                    break;
                case SynergyEffectType.CritChancePercent:
                    modifiers.CritChancePercent += data.EffectValue;
                    break;
                case SynergyEffectType.FastEnemyDamagePercent:
                    modifiers.FastEnemyDamagePercent += data.EffectValue;
                    break;
            }
        }
    }

    public readonly struct SynergyEvaluation
    {
        public SynergyEvaluation(IReadOnlyList<SynergyData> active, SynergyModifiers modifiers)
        {
            Active = active ?? System.Array.Empty<SynergyData>();
            Modifiers = modifiers ?? SynergyModifiers.Empty;
        }

        public IReadOnlyList<SynergyData> Active { get; }
        public SynergyModifiers Modifiers { get; }
    }
}
