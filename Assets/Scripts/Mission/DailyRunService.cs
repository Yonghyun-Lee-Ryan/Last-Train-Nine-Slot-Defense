using System;
using System.Collections.Generic;
using LastTrain.Data;
using LastTrain.Release;
using LastTrain.Run;

namespace LastTrain.Mission
{
    /// <summary>오늘의 막차: 날짜+게임 버전으로 동일 Seed·규칙을 생성한다.</summary>
    public static class DailyRunService
    {
        public const string ModeId = "daily_last_train";

        public static int ComputeSeed(DateTime utcDate, string versionName)
        {
            string day = MissionClock.GetDailyKey(utcDate.Date);
            string version = string.IsNullOrWhiteSpace(versionName) ? "0.0.0" : versionName.Trim();
            string payload = $"{ModeId}|{day}|{version}";
            unchecked
            {
                int hash = 23;
                for (int i = 0; i < payload.Length; i++)
                {
                    hash = (hash * 31) + payload[i];
                }

                return hash == 0 ? 1 : hash;
            }
        }

        public static int ComputeSeedForToday(string versionName = null)
        {
            if (string.IsNullOrWhiteSpace(versionName))
            {
                AppReleaseConfig config = AppReleaseConfigLocator.Load();
                versionName = config != null ? config.VersionName : "0.1.0";
            }

            return ComputeSeed(MissionClock.UtcNow(), versionName);
        }

        public static string GetTodayKey()
        {
            return MissionClock.GetDailyKey(MissionClock.UtcNow());
        }

        public static DailyRuleData ResolveRule(IReadOnlyList<DailyRuleData> catalog, int seed)
        {
            if (catalog == null || catalog.Count == 0)
            {
                return null;
            }

            int count = 0;
            for (int i = 0; i < catalog.Count; i++)
            {
                if (catalog[i] != null && !string.IsNullOrWhiteSpace(catalog[i].Id))
                {
                    count++;
                }
            }

            if (count <= 0)
            {
                return null;
            }

            int pick = Math.Abs(seed) % count;
            int seen = 0;
            for (int i = 0; i < catalog.Count; i++)
            {
                DailyRuleData rule = catalog[i];
                if (rule == null || string.IsNullOrWhiteSpace(rule.Id))
                {
                    continue;
                }

                if (seen == pick)
                {
                    return rule;
                }

                seen++;
            }

            return null;
        }

        public static DailyRuleData ResolveToday(IReadOnlyList<DailyRuleData> catalog, string versionName = null)
        {
            return ResolveRule(catalog, ComputeSeedForToday(versionName));
        }

        public static int ResolveLockedSlot(int seed, int catalogCount)
        {
            int[] weighted =
            {
                4, 4, 4,
                1, 7,
                3, 5,
                0, 2, 6, 8,
            };
            int divisor = Math.Max(1, catalogCount);
            int pick = Math.Abs(seed / divisor) % weighted.Length;
            return weighted[pick];
        }

        public static void BindRule(RunStartConfig config, DailyRuleData rule, int seed, int catalogCount)
        {
            if (config == null || !config.IsDailyRun || rule == null)
            {
                return;
            }

            config.DailyRuleId = rule.Id ?? string.Empty;
            config.DailyRuleDisplayName = rule.DisplayName ?? string.Empty;
            switch (rule.Kind)
            {
                case DailyRuleKind.LockSeat:
                    config.DailyLockedSlotIndex = ResolveLockedSlot(seed, catalogCount);
                    break;
                case DailyRuleKind.SummonCostMul:
                    config.DailySummonCostMultiplier = rule.Magnitude > 0.01f ? rule.Magnitude : 1f;
                    break;
                case DailyRuleKind.EnemySpeedMul:
                    config.DailyEnemySpeedMultiplier = rule.Magnitude > 0.01f ? rule.Magnitude : 1f;
                    break;
                case DailyRuleKind.GrantRelic:
                    config.DailyStartingRelicId = rule.TargetId ?? string.Empty;
                    break;
                case DailyRuleKind.ReducedPrepTime:
                    config.DailyPreparationTimeSeconds = rule.Magnitude > 0f ? rule.Magnitude : 2f;
                    break;
            }
        }
    }
}
