using System;
using System.Collections.Generic;
using LastTrain.Data;
using LastTrain.Run;

namespace LastTrain.Ability
{
    /// <summary>계산된 능력 수정치를 RunState(객차·승객 버프)에 반영한다.</summary>
    public static class AbilityEffectApplier
    {
        /// <summary>3x3 그리드에서 앞줄(하단 행) 슬롯.</summary>
        public static readonly int[] FrontRowSlots = { 6, 7, 8 };

        public static void Refresh(RunState runState, int baseTrainMaxHp)
        {
            if (runState?.Abilities == null || runState.Train == null)
            {
                return;
            }

            AbilityModifiers modifiers = AbilityEffectCalculator.Compute(runState.Abilities.ExpandSelectedWithStacks());
            runState.Abilities.SetModifiers(modifiers);

            int targetMaxHp = Math.Max(1, baseTrainMaxHp + modifiers.TrainMaxHpFlat);
            int previousMax = runState.Train.MaxHp;
            runState.Train.SetMaxHp(targetMaxHp, healToFull: false);
            if (targetMaxHp > previousMax)
            {
                runState.Train.Heal(targetMaxHp - previousMax);
            }

            RefreshPassengerBuffs(runState, modifiers);
        }

        public static void RefreshPassengerBuffs(RunState runState)
        {
            if (runState?.Abilities == null)
            {
                return;
            }

            RefreshPassengerBuffs(runState, runState.Abilities.Modifiers);
        }

        public static void ApplyStationCompleteHeal(RunState runState)
        {
            if (runState?.Abilities == null || runState.Train == null)
            {
                return;
            }

            int heal = runState.Abilities.Modifiers.TrainHealOnStationComplete;
            if (heal > 0)
            {
                runState.Train.Heal(heal);
            }
        }

        private static void RefreshPassengerBuffs(RunState runState, AbilityModifiers modifiers)
        {
            bool diverseActive = CountUniquePassengerTypes(runState) >= modifiers.DiversePassengerThreshold
                                 && modifiers.DiversePassengerDamagePercent > 0f;

            for (int slot = 0; slot < RunState.GridSlotCount; slot++)
            {
                PassengerRuntime passenger = runState.GetPassengerAtSlot(slot);
                if (passenger == null)
                {
                    continue;
                }

                ClearAbilityBuffs(passenger);

                float attackPercent = modifiers.GlobalAttackPercent
                                      + modifiers.GetPassengerAttackPercent(passenger.Data.Id);
                float speedPercent = modifiers.GlobalAttackSpeedPercent
                                     + modifiers.GetPassengerAttackSpeedPercent(passenger.Data.Id);

                if ((passenger.Data.Tags & PassengerTag.OfficeWorker) != 0
                    && runState.Relics?.Modifiers != null)
                {
                    speedPercent += runState.Relics.Modifiers.OfficeWorkerAttackSpeedPercent;
                }

                if (IsFrontRow(slot))
                {
                    attackPercent += modifiers.FrontRowAttackPercent;
                }

                if (diverseActive)
                {
                    attackPercent += modifiers.DiversePassengerDamagePercent;
                }

                attackPercent += runState.GetLiveEventAttackPercentBonus(passenger.Data.Id);

                if (attackPercent == 0f && speedPercent == 0f)
                {
                    continue;
                }

                string buffId = AbilityEffectCalculator.AbilityBuffIdPrefix + passenger.InstanceId;
                passenger.AddBuff(new RuntimeBuff(buffId, attackPercent, speedPercent));
            }
        }

        private static void ClearAbilityBuffs(PassengerRuntime passenger)
        {
            var toRemove = new List<string>();
            for (int i = 0; i < passenger.Buffs.Count; i++)
            {
                string id = passenger.Buffs[i].BuffId;
                if (id != null && id.StartsWith(AbilityEffectCalculator.AbilityBuffIdPrefix, StringComparison.Ordinal))
                {
                    toRemove.Add(id);
                }
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                passenger.RemoveBuff(toRemove[i]);
            }
        }

        private static bool IsFrontRow(int slotIndex)
        {
            for (int i = 0; i < FrontRowSlots.Length; i++)
            {
                if (FrontRowSlots[i] == slotIndex)
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountUniquePassengerTypes(RunState runState)
        {
            var ids = new HashSet<string>();
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
    }
}
