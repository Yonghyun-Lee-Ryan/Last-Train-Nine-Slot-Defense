using System;
using System.Collections.Generic;
using LastTrain.Data;
using LastTrain.Difficulty;
using LastTrain.Run;
using LastTrain.Save;
using UnityEngine;

namespace LastTrain.Mission
{
    /// <summary>미션 진행·기간 갱신·보상 수령. 매 프레임 검사하지 않는다.</summary>
    public static class MissionProgressService
    {
        public static event Action MetaMissionsUpdated;

        public static void EnsurePeriods(MetaSaveData meta, IReadOnlyList<MissionData> missions, DateTime? utcNow = null)
        {
            if (meta == null)
            {
                return;
            }

            meta.EnsureDefaults();
            DateTime now = utcNow ?? MissionClock.UtcNow();

            if (MissionClock.TryDetectClockRegression(meta.missionLastTrustedUtc, now, out string warning))
            {
                Debug.LogWarning(warning);
                // 역행 시 기간 키를 갱신하지 않고 신뢰 시각만 유지해 조작 이득을 줄인다.
                return;
            }

            meta.missionLastTrustedUtc = MissionClock.ToIso(now);
            string dailyKey = MissionClock.GetDailyKey(now);
            string weeklyKey = MissionClock.GetWeeklyKey(now);

            bool dailyChanged = !string.Equals(meta.missionDailyKey, dailyKey, StringComparison.Ordinal);
            bool weeklyChanged = !string.Equals(meta.missionWeeklyKey, weeklyKey, StringComparison.Ordinal);

            if (dailyChanged)
            {
                meta.missionDailyKey = dailyKey;
                ResetPeriod(meta, MissionPeriod.Daily, dailyKey, missions);
            }

            if (weeklyChanged)
            {
                meta.missionWeeklyKey = weeklyKey;
                ResetPeriod(meta, MissionPeriod.Weekly, weeklyKey, missions);
            }

            EnsureEntriesExist(meta, missions, dailyKey, weeklyKey);
        }

        public static void ApplyEvent(
            MetaSaveData meta,
            IReadOnlyList<MissionData> missions,
            MissionEventType eventType,
            int amount = 1,
            string id = null,
            int param = 0,
            DateTime? utcNow = null)
        {
            if (meta == null || missions == null || amount == 0)
            {
                return;
            }

            EnsurePeriods(meta, missions, utcNow);
            bool changed = false;

            for (int i = 0; i < missions.Count; i++)
            {
                MissionData mission = missions[i];
                if (mission == null || string.IsNullOrWhiteSpace(mission.Id))
                {
                    continue;
                }

                MissionCondition condition = mission.Condition;
                if (condition == null || condition.Type == MissionConditionType.None)
                {
                    continue;
                }

                int delta = ResolveEventDelta(condition, eventType, amount, id, param);
                if (delta <= 0)
                {
                    continue;
                }

                MissionProgressSave entry = GetOrCreateEntry(meta, mission, utcNow);
                if (entry.claimed)
                {
                    continue;
                }

                int before = entry.progress;
                entry.progress = Math.Min(condition.TargetValue, entry.progress + delta);
                if (entry.progress >= condition.TargetValue)
                {
                    entry.completed = true;
                }

                if (entry.progress != before)
                {
                    changed = true;
                }
            }

            if (changed)
            {
                MetaMissionsUpdated?.Invoke();
            }
        }

        public static void ApplyRunResult(
            MetaSaveData meta,
            IReadOnlyList<MissionData> missions,
            RunResult result,
            DateTime? utcNow = null)
        {
            if (meta == null || result == null || missions == null)
            {
                return;
            }

            EnsurePeriods(meta, missions, utcNow);

            // 런타임 바인더가 처리하지 않는 회차 종료 전용 조건만 반영한다.
            if (result.IsVictory)
            {
                ApplyEvent(meta, missions, MissionEventType.RunCleared, 1, result.DifficultyId, utcNow: utcNow);
            }

            if (result.BossesKilled > 0 && result.IsVictory)
            {
                ApplyEvent(meta, missions, MissionEventType.FinalBossDefeated, result.BossesKilled, utcNow: utcNow);
            }
        }

        public static bool TryClaimReward(
            MetaSaveData meta,
            MissionData mission,
            out int ticketsGranted,
            out int xpGranted,
            DateTime? utcNow = null)
        {
            ticketsGranted = 0;
            xpGranted = 0;
            if (meta == null || mission == null || string.IsNullOrWhiteSpace(mission.Id))
            {
                return false;
            }

            EnsurePeriods(meta, new[] { mission }, utcNow);
            MissionProgressSave entry = FindEntry(meta, mission.Id);
            if (entry == null || !entry.completed || entry.claimed)
            {
                return false;
            }

            entry.claimed = true;
            ticketsGranted = mission.RewardTicketFragments;
            xpGranted = mission.RewardAccountXp;
            meta.ticketFragments = Math.Max(0, meta.ticketFragments + ticketsGranted);
            meta.accountXp = Math.Max(0, meta.accountXp + xpGranted);

            // 레벨업은 메타 진행 서비스와 동일한 규칙으로 맞춤
            while (meta.accountXp >= MetaProgressionDefaults.AccountXpPerLevel)
            {
                meta.accountXp -= MetaProgressionDefaults.AccountXpPerLevel;
                meta.accountLevel++;
            }

            MetaMissionsUpdated?.Invoke();
            return true;
        }

        public static List<MissionProgressView> BuildViews(
            MetaSaveData meta,
            IReadOnlyList<MissionData> missions,
            MissionPeriod? periodFilter = null,
            DateTime? utcNow = null)
        {
            var views = new List<MissionProgressView>();
            if (meta == null || missions == null)
            {
                return views;
            }

            EnsurePeriods(meta, missions, utcNow);
            for (int i = 0; i < missions.Count; i++)
            {
                MissionData mission = missions[i];
                if (mission == null)
                {
                    continue;
                }

                if (periodFilter.HasValue && mission.Period != periodFilter.Value)
                {
                    continue;
                }

                MissionProgressSave entry = FindEntry(meta, mission.Id);
                int progress = entry?.progress ?? 0;
                bool completed = entry != null && entry.completed;
                bool claimed = entry != null && entry.claimed;
                string periodKey = entry?.periodKey
                    ?? MissionClock.GetPeriodKey(mission.Period, utcNow ?? MissionClock.UtcNow());
                views.Add(new MissionProgressView(
                    mission,
                    progress,
                    mission.Condition.TargetValue,
                    completed,
                    claimed,
                    periodKey));
            }

            return views;
        }

        private static int ResolveEventDelta(
            MissionCondition condition,
            MissionEventType eventType,
            int amount,
            string id,
            int param)
        {
            switch (condition.Type)
            {
                case MissionConditionType.MergeCount:
                    return eventType == MissionEventType.Merge ? amount : 0;
                case MissionConditionType.SummonCount:
                    return eventType == MissionEventType.Summoned ? amount : 0;
                case MissionConditionType.ShopPurchaseCount:
                    return eventType == MissionEventType.ShopPurchased ? amount : 0;
                case MissionConditionType.EliteKillCount:
                    return eventType == MissionEventType.EnemyKilled && param == (int)EnemyType.Elite
                        ? amount
                        : 0;
                case MissionConditionType.DealBossDamage:
                    return eventType == MissionEventType.BossDamaged ? amount : 0;
                case MissionConditionType.DistinctPassengersPlaced:
                    return eventType == MissionEventType.DistinctPassengerPlaced ? amount : 0;
                case MissionConditionType.ReachPassengerStar:
                    if (eventType != MissionEventType.PassengerStarReached)
                    {
                        return 0;
                    }

                    if (!string.IsNullOrWhiteSpace(condition.TargetId)
                        && !string.Equals(condition.TargetId, id, StringComparison.Ordinal))
                    {
                        return 0;
                    }

                    // amount = 도달 별 수. 목표 별 이상이면 완료로 점프
                    return amount >= condition.TargetValue ? condition.TargetValue : 0;
                case MissionConditionType.ReachStationWithMinHp:
                    if (eventType != MissionEventType.StationCompleted)
                    {
                        return 0;
                    }

                    return param >= condition.TargetParam ? amount : 0;
                case MissionConditionType.ReachStationWithoutAds:
                    if (eventType != MissionEventType.StationCompleted)
                    {
                        return 0;
                    }

                    return string.Equals(id, "no_ads", StringComparison.Ordinal) ? amount : 0;
                case MissionConditionType.RareOrHigherAbilitySelect:
                    if (eventType != MissionEventType.AbilitySelected)
                    {
                        return 0;
                    }

                    return param >= (int)Rarity.Rare ? amount : 0;
                case MissionConditionType.ClearRouteCount:
                    return eventType == MissionEventType.RunCleared ? amount : 0;
                case MissionConditionType.ClearDifficultyOrHigher:
                    if (eventType != MissionEventType.RunCleared)
                    {
                        return 0;
                    }

                    return IsDifficultyAtLeast(id, condition.TargetId) ? amount : 0;
                case MissionConditionType.DefeatFinalBoss:
                    return eventType == MissionEventType.FinalBossDefeated ? amount : 0;
                default:
                    return 0;
            }
        }

        private static bool IsDifficultyAtLeast(string actualId, string requiredId)
        {
            int actual = DifficultyRank(actualId);
            int required = DifficultyRank(string.IsNullOrWhiteSpace(requiredId)
                ? DifficultyIds.Express
                : requiredId);
            return actual >= required;
        }

        private static int DifficultyRank(string id)
        {
            if (string.Equals(id, DifficultyIds.NonstopHell, StringComparison.Ordinal))
            {
                return 3;
            }

            if (string.Equals(id, DifficultyIds.MidnightExpress, StringComparison.Ordinal))
            {
                return 2;
            }

            if (string.Equals(id, DifficultyIds.Express, StringComparison.Ordinal))
            {
                return 1;
            }

            return 0;
        }

        private static void ResetPeriod(
            MetaSaveData meta,
            MissionPeriod period,
            string periodKey,
            IReadOnlyList<MissionData> missions)
        {
            var kept = new List<MissionProgressSave>();
            MissionProgressSave[] existing = meta.missionProgresses ?? Array.Empty<MissionProgressSave>();
            for (int i = 0; i < existing.Length; i++)
            {
                MissionProgressSave entry = existing[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.missionId))
                {
                    continue;
                }

                MissionData data = FindMission(missions, entry.missionId);
                if (data != null && data.Period == period)
                {
                    continue; // 해당 기간 미션 제거 후 재생성
                }

                kept.Add(entry);
            }

            meta.missionProgresses = kept.ToArray();
            EnsureEntriesExist(
                meta,
                missions,
                period == MissionPeriod.Daily ? periodKey : meta.missionDailyKey,
                period == MissionPeriod.Weekly ? periodKey : meta.missionWeeklyKey);
        }

        private static void EnsureEntriesExist(
            MetaSaveData meta,
            IReadOnlyList<MissionData> missions,
            string dailyKey,
            string weeklyKey)
        {
            if (missions == null)
            {
                return;
            }

            var list = new List<MissionProgressSave>(meta.missionProgresses ?? Array.Empty<MissionProgressSave>());
            for (int i = 0; i < missions.Count; i++)
            {
                MissionData mission = missions[i];
                if (mission == null || string.IsNullOrWhiteSpace(mission.Id))
                {
                    continue;
                }

                if (FindEntry(list, mission.Id) != null)
                {
                    continue;
                }

                list.Add(new MissionProgressSave
                {
                    missionId = mission.Id,
                    periodKey = mission.Period == MissionPeriod.Weekly ? weeklyKey : dailyKey,
                    progress = 0,
                    claimed = false,
                    completed = false,
                });
            }

            meta.missionProgresses = list.ToArray();
        }

        private static MissionProgressSave GetOrCreateEntry(
            MetaSaveData meta,
            MissionData mission,
            DateTime? utcNow)
        {
            MissionProgressSave entry = FindEntry(meta, mission.Id);
            if (entry != null)
            {
                return entry;
            }

            DateTime now = utcNow ?? MissionClock.UtcNow();
            entry = new MissionProgressSave
            {
                missionId = mission.Id,
                periodKey = MissionClock.GetPeriodKey(mission.Period, now),
            };
            var list = new List<MissionProgressSave>(meta.missionProgresses ?? Array.Empty<MissionProgressSave>())
            {
                entry,
            };
            meta.missionProgresses = list.ToArray();
            return entry;
        }

        private static MissionProgressSave FindEntry(MetaSaveData meta, string missionId)
        {
            return FindEntry(meta?.missionProgresses, missionId);
        }

        private static MissionProgressSave FindEntry(IList<MissionProgressSave> list, string missionId)
        {
            if (list == null || string.IsNullOrWhiteSpace(missionId))
            {
                return null;
            }

            for (int i = 0; i < list.Count; i++)
            {
                MissionProgressSave entry = list[i];
                if (entry != null && string.Equals(entry.missionId, missionId, StringComparison.Ordinal))
                {
                    return entry;
                }
            }

            return null;
        }

        private static MissionData FindMission(IReadOnlyList<MissionData> missions, string id)
        {
            if (missions == null)
            {
                return null;
            }

            for (int i = 0; i < missions.Count; i++)
            {
                MissionData mission = missions[i];
                if (mission != null && string.Equals(mission.Id, id, StringComparison.Ordinal))
                {
                    return mission;
                }
            }

            return null;
        }
    }
}
