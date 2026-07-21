using System;
using System.Collections.Generic;
using LastTrain.Run;

namespace LastTrain.Synergy
{
    /// <summary>계산된 시너지 수정치를 RunState(승객 버프)에 반영한다.</summary>
    public static class SynergyEffectApplier
    {
        public static void Refresh(RunState runState)
        {
            if (runState?.Synergies == null)
            {
                return;
            }

            SynergyEvaluation evaluation = SynergyEffectCalculator.Evaluate(
                runState.Synergies.Catalog,
                runState);

            runState.Synergies.SetActive(evaluation.Active, evaluation.Modifiers);
            RefreshPassengerBuffs(runState, evaluation.Modifiers);
        }

        private static void RefreshPassengerBuffs(RunState runState, SynergyModifiers modifiers)
        {
            for (int slot = 0; slot < RunState.GridSlotCount; slot++)
            {
                PassengerRuntime passenger = runState.GetPassengerAtSlot(slot);
                if (passenger == null)
                {
                    continue;
                }

                ClearSynergyBuffs(passenger);

                float attackPercent = modifiers.GlobalAttackPercent;
                float speedPercent = modifiers.GlobalAttackSpeedPercent;
                if (attackPercent == 0f && speedPercent == 0f)
                {
                    continue;
                }

                string buffId = SynergyEffectCalculator.SynergyBuffIdPrefix + passenger.InstanceId;
                passenger.AddBuff(new RuntimeBuff(buffId, attackPercent, speedPercent));
            }
        }

        private static void ClearSynergyBuffs(PassengerRuntime passenger)
        {
            var toRemove = new List<string>();
            for (int i = 0; i < passenger.Buffs.Count; i++)
            {
                string id = passenger.Buffs[i].BuffId;
                if (id != null
                    && id.StartsWith(SynergyEffectCalculator.SynergyBuffIdPrefix, StringComparison.Ordinal))
                {
                    toRemove.Add(id);
                }
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                passenger.RemoveBuff(toRemove[i]);
            }
        }
    }
}
