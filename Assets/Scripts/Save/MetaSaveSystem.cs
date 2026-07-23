using System;
using System.Collections.Generic;
using LastTrain.Core;
using LastTrain.Data;
using LastTrain.Run;

namespace LastTrain.Save
{
    /// <summary>MetaSaveData 로드/저장과 회차 보상 적용 진입점.</summary>
    public static class MetaSaveSystem
    {
        public static MetaApplyResult LastApplyResult { get; private set; }

        public static MetaSaveData LoadOrCreate()
        {
            if (RunSaveSystem.TryLoadMeta(out MetaSaveData meta) && meta != null)
            {
                meta.EnsureDefaults();
                return meta;
            }

            meta = new MetaSaveData();
            meta.EnsureDefaults();
            RunSaveSystem.SaveMeta(meta);
            return meta;
        }

        public static bool Save(MetaSaveData meta)
        {
            if (meta == null)
            {
                return false;
            }

            meta.EnsureDefaults();
            meta.version = MetaSaveData.CurrentVersion;
            return RunSaveSystem.SaveMeta(meta);
        }

        public static MetaApplyResult ApplyRunResult(RunResult result)
        {
            MetaSaveData meta = LoadOrCreate();
            MetaApplyResult applyResult = MetaProgressionService.TryApplyRunResult(meta, result);
            if (applyResult.Applied)
            {
                GameDatabase database = GameDatabaseLocator.Load();
                if (database?.Missions != null)
                {
                    Mission.MissionProgressService.ApplyRunResult(meta, database.Missions, result);
                }
            }

            if (result != null && result.IsEndlessRun)
            {
                var mock = new Leaderboard.MockLeaderboardService();
                Endless.EndlessProgressService.TrySubmitRun(
                    meta,
                    result,
                    AppRoot.Instance?.GameSession?.RunState,
                    mock,
                    out _,
                    out _);
            }

            if (applyResult.Applied || (result != null && result.IsEndlessRun))
            {
                Save(meta);
            }

            LastApplyResult = applyResult;
            return applyResult;
        }

        public static MetaProgressSnapshot GetSnapshot()
        {
            return MetaProgressionService.CreateSnapshot(LoadOrCreate());
        }

        public static void ClearPendingDiscoveries()
        {
            MetaSaveData meta = LoadOrCreate();
            meta.pendingNewDiscoveryIds = Array.Empty<string>();
            Save(meta);
        }

        public static List<PassengerData> FilterUnlockedPassengers(IReadOnlyList<PassengerData> allPassengers)
        {
            var result = new List<PassengerData>();
            if (allPassengers == null)
            {
                return result;
            }

            MetaSaveData meta = LoadOrCreate();
            for (int i = 0; i < allPassengers.Count; i++)
            {
                PassengerData passenger = allPassengers[i];
                if (passenger == null || string.IsNullOrWhiteSpace(passenger.Id))
                {
                    continue;
                }

                if (MetaProgressionService.IsPassengerUnlocked(meta, passenger.Id)
                    || passenger.StartsUnlocked)
                {
                    // StartsUnlocked는 메타에 없어도 소환 가능하게 하되, 메타에도 반영한다.
                    if (!MetaProgressionService.IsPassengerUnlocked(meta, passenger.Id))
                    {
                        AppendUnlocked(meta, passenger.Id);
                        Save(meta);
                    }

                    result.Add(passenger);
                }
            }

            if (result.Count == 0)
            {
                // 안전장치: 기본 해금 ID만이라도 매칭
                for (int i = 0; i < allPassengers.Count; i++)
                {
                    PassengerData passenger = allPassengers[i];
                    if (passenger == null)
                    {
                        continue;
                    }

                    for (int d = 0; d < MetaProgressionDefaults.DefaultUnlockedPassengerIds.Length; d++)
                    {
                        if (string.Equals(
                                passenger.Id,
                                MetaProgressionDefaults.DefaultUnlockedPassengerIds[d],
                                StringComparison.Ordinal))
                        {
                            result.Add(passenger);
                            break;
                        }
                    }
                }
            }

            return result;
        }

        private static void AppendUnlocked(MetaSaveData meta, string passengerId)
        {
            if (meta.unlockedPassengerIds == null)
            {
                meta.unlockedPassengerIds = new[] { passengerId };
                return;
            }

            for (int i = 0; i < meta.unlockedPassengerIds.Length; i++)
            {
                if (string.Equals(meta.unlockedPassengerIds[i], passengerId, StringComparison.Ordinal))
                {
                    return;
                }
            }

            var next = new string[meta.unlockedPassengerIds.Length + 1];
            Array.Copy(meta.unlockedPassengerIds, next, meta.unlockedPassengerIds.Length);
            next[meta.unlockedPassengerIds.Length] = passengerId;
            meta.unlockedPassengerIds = next;
        }
    }
}
