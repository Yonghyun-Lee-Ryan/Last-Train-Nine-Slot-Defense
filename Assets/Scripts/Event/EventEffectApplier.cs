using System;
using LastTrain.Ability;
using LastTrain.Data;
using LastTrain.Passenger;
using LastTrain.Relic;
using LastTrain.Run;
using UnityEngine;

namespace LastTrain.Event
{
    public static class EventEffectApplier
    {
        public static bool ApplyAll(
            RunState runState,
            GameDatabase database,
            RelicManager relicManager,
            EventEffectData[] effects,
            float badOutcomeReductionPercent)
        {
            if (runState == null || effects == null || effects.Length == 0)
            {
                return true;
            }

            for (int i = 0; i < effects.Length; i++)
            {
                if (!Apply(runState, database, relicManager, effects[i], badOutcomeReductionPercent))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool Apply(
            RunState runState,
            GameDatabase database,
            RelicManager relicManager,
            EventEffectData effect,
            float badOutcomeReductionPercent)
        {
            float value = ScaleNegative(effect, badOutcomeReductionPercent);
            switch (effect.effectType)
            {
                case EventEffectType.AddCoins:
                    runState.Currency.AddCoins(Mathf.RoundToInt(Mathf.Max(0f, value)));
                    return true;

                case EventEffectType.RemoveCoins:
                {
                    int amount = Mathf.RoundToInt(Mathf.Max(0f, -value));
                    if (amount <= 0)
                    {
                        return true;
                    }

                    return runState.Currency.TrySpend(amount);
                }

                case EventEffectType.HealTrain:
                    runState.Train.Heal(Mathf.RoundToInt(Mathf.Max(0f, value)));
                    return true;

                case EventEffectType.DamageTrain:
                    runState.Train.ApplyDamage(Mathf.RoundToInt(Mathf.Max(0f, -value)));
                    return true;

                case EventEffectType.GrantPassenger:
                    return TryGrantPassenger(runState, database, effect.targetId, 1);

                case EventEffectType.RemoveRandomPassenger:
                    return TryRemoveRandomPassenger(runState);

                case EventEffectType.GrantAbility:
                    return TryGrantAbility(runState, database, effect.targetId);

                case EventEffectType.GrantRelic:
                    return relicManager != null && relicManager.TryAcquire(effect.targetId);

                case EventEffectType.NextStationEnemyBuff:
                    runState.NextStationModifiers.MultiplyEnemyHealth(Mathf.Max(1f, value));
                    return true;

                case EventEffectType.NextStationRewardBonus:
                    runState.NextStationModifiers.MultiplyRewardCoins(Mathf.Max(1f, value));
                    return true;

                default:
                    return true;
            }
        }

        private static float ScaleNegative(EventEffectData effect, float reductionPercent)
        {
            float value = effect.value;
            if (value >= 0f)
            {
                return value;
            }

            float scale = 1f - Mathf.Clamp01(reductionPercent / 100f);
            return value * scale;
        }

        private static bool TryGrantPassenger(RunState runState, GameDatabase database, string passengerId, int starLevel)
        {
            if (database == null
                || string.IsNullOrWhiteSpace(passengerId)
                || !database.TryGetPassenger(passengerId, out PassengerData data))
            {
                return false;
            }

            PassengerRuntime runtime = PassengerRuntime.Create(data, starLevel);
            int slot = runState.FindFirstEmptySlot();
            if (slot >= 0)
            {
                return runState.TryPlacePassenger(slot, runtime);
            }

            // 칸이 가득 차면 대기열에 넣고, 판매 등으로 빈 칸이 생기면 배치한다.
            runState.EnqueuePendingPassenger(runtime);
            return true;
        }

        private static bool TryGrantAbility(RunState runState, GameDatabase database, string abilityId)
        {
            if (database == null
                || string.IsNullOrWhiteSpace(abilityId)
                || !database.TryGetAbility(abilityId, out AbilityData ability)
                || !runState.Abilities.CanSelect(ability))
            {
                return false;
            }

            runState.Abilities.AddSelected(ability);
            AbilityEffectApplier.Refresh(runState, runState.BaseTrainMaxHp);
            return true;
        }

        private static bool TryRemoveRandomPassenger(RunState runState)
        {
            int count = 0;
            for (int i = 0; i < RunState.GridSlotCount; i++)
            {
                if (runState.GetPassengerAtSlot(i) != null)
                {
                    count++;
                }
            }

            if (count == 0)
            {
                return false;
            }

            int pick = UnityEngine.Random.Range(0, count);
            for (int slot = 0; slot < RunState.GridSlotCount; slot++)
            {
                if (runState.GetPassengerAtSlot(slot) == null)
                {
                    continue;
                }

                if (pick == 0)
                {
                    runState.TryRemovePassenger(slot, out _);
                    return true;
                }

                pick--;
            }

            return false;
        }
    }
}
