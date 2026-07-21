using System;
using System.Collections.Generic;
using LastTrain.Ability;
using LastTrain.Data;
using LastTrain.Run;

namespace LastTrain.Save
{
    /// <summary>
    /// RunState <-> RunSaveData 변환을 담당한다.
    /// </summary>
    public static class RunSaveMapper
    {
        public static RunStartConfig CreateStartConfigFromSave(RunSaveData data)
        {
            var config = RunStartConfig.CreateDefault();
            if (data == null)
            {
                return config;
            }

            config.InitialTrainMaxHp = data.trainMaxHp > 0 ? data.trainMaxHp : config.InitialTrainMaxHp;
            config.InitialTrainCurrentHp = data.trainHp >= 0 ? data.trainHp : config.InitialTrainCurrentHp;
            config.InitialCoins = data.coinsCurrent >= 0 ? data.coinsCurrent : config.InitialCoins;

            config.InitialStationIndex = data.stationIndex >= 1 ? data.stationIndex : config.InitialStationIndex;
            config.LineId = string.IsNullOrWhiteSpace(data.lineId) ? config.LineId : data.lineId;

            return config;
        }

        public static RunSaveData CreateFromRunState(RunState runState)
        {
            if (runState == null)
            {
                return null;
            }

            var data = new RunSaveData
            {
                version = RunSaveData.CurrentVersion,
                savedBattlePhase = (int)(runState.Battle?.CurrentPhase ?? RunPhase.None),

                stationIndex = runState.Station?.CurrentStationIndex ?? 1,
                stationId = runState.Station?.CurrentStationId ?? string.Empty,
                currentWaveIndex = runState.Station?.CurrentWaveIndex ?? 0,
                completedStationCount = runState.Station?.CompletedStationCount ?? 0,

                trainHp = runState.Train?.CurrentHp ?? 0,
                trainMaxHp = runState.Train?.MaxHp ?? 0,

                coinsCurrent = runState.Currency?.CurrentCoins ?? 0,
                coinsTotalEarned = runState.Currency?.TotalEarned ?? 0,
                coinsTotalSpent = runState.Currency?.TotalSpent ?? 0,

                enemiesKilled = runState.History?.EnemiesKilled ?? 0,
                mergeCount = runState.History?.MergeCount ?? 0,
                passengersSummoned = runState.History?.PassengersSummoned ?? 0,
                passengersSold = runState.History?.PassengersSold ?? 0,
                highestPassengerStar = runState.History?.HighestPassengerStar ?? 1,
                abilityCardsSelected = runState.History?.AbilityCardsSelected ?? 0,

                lineId = runState.LineId ?? string.Empty
            };

            data.slots = new RunSaveData.SlotSave[RunState.GridSlotCount];
            for (int i = 0; i < RunState.GridSlotCount; i++)
            {
                PassengerRuntime passenger = runState.GetPassengerAtSlot(i);
                if (passenger == null)
                {
                    data.slots[i] = new RunSaveData.SlotSave();
                    continue;
                }

                data.slots[i] = new RunSaveData.SlotSave
                {
                    passengerId = passenger.Data?.Id ?? string.Empty,
                    starLevel = passenger.StarLevel
                };
            }

            // 선택 능력은 스택 수만큼 id를 확장해서 저장
            List<AbilityData> expanded = runState.Abilities?.ExpandSelectedWithStacks() ?? new List<AbilityData>();
            if (expanded.Count > 0)
            {
                var ids = new string[expanded.Count];
                for (int i = 0; i < expanded.Count; i++)
                {
                    ids[i] = expanded[i]?.Id ?? string.Empty;
                }

                data.selectedAbilityIdsExpanded = ids;
            }
            else
            {
                data.selectedAbilityIdsExpanded = Array.Empty<string>();
            }

            return data;
        }

        public static bool ApplyToRunState(
            RunState runState,
            RunSaveData data,
            GameDatabase gameDatabase)
        {
            if (runState == null || data == null || gameDatabase == null)
            {
                return false;
            }

            // 1) Station / Currency / History
            runState.Station.RestoreFromSave(
                data.stationIndex,
                data.stationId,
                data.currentWaveIndex,
                data.completedStationCount);

            runState.Currency.RestoreFromSave(
                data.coinsCurrent,
                data.coinsTotalEarned,
                data.coinsTotalSpent);

            runState.History.RestoreFromSave(
                data.enemiesKilled,
                data.mergeCount,
                data.passengersSummoned,
                data.passengersSold,
                data.highestPassengerStar,
                data.abilityCardsSelected);

            // 2) Passengers
            if (data.slots != null)
            {
                for (int slot = 0; slot < Math.Min(data.slots.Length, RunState.GridSlotCount); slot++)
                {
                    RunSaveData.SlotSave slotData = data.slots[slot];
                    if (string.IsNullOrWhiteSpace(slotData.passengerId))
                    {
                        continue;
                    }

                    if (!gameDatabase.TryGetPassenger(slotData.passengerId, out PassengerData passengerData))
                    {
                        continue;
                    }

                    PassengerRuntime passenger = PassengerRuntime.Create(
                        passengerData,
                        starLevel: Math.Max(1, slotData.starLevel));

                    runState.TryPlacePassengerFromSave(slot, passenger);
                }
            }

            // 3) Abilities (selected)
            if (data.selectedAbilityIdsExpanded != null && data.selectedAbilityIdsExpanded.Length > 0)
            {
                var expanded = new List<AbilityData>(data.selectedAbilityIdsExpanded.Length);
                for (int i = 0; i < data.selectedAbilityIdsExpanded.Length; i++)
                {
                    string abilityId = data.selectedAbilityIdsExpanded[i];
                    if (string.IsNullOrWhiteSpace(abilityId))
                    {
                        continue;
                    }

                    if (!gameDatabase.TryGetAbility(abilityId, out AbilityData ability))
                    {
                        continue;
                    }

                    expanded.Add(ability);
                }

                runState.Abilities.RestoreSelectedExpanded(expanded);
            }

            // 선택 능력으로부터 modifiers 계산 후 passenger buff만 반영(Train HP는 저장값을 유지)
            var expandedSelected = runState.Abilities.ExpandSelectedWithStacks();
            var modifiers = AbilityEffectCalculator.Compute(expandedSelected);
            runState.Abilities.SetModifiers(modifiers);
            AbilityEffectApplier.RefreshPassengerBuffs(runState);

            // 4) Ensure battle is at Preparing
            runState.Battle.SetPhase(RunPhase.Preparing);
            return true;
        }
    }
}

