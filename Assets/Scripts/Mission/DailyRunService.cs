using System;
using LastTrain.Release;

namespace LastTrain.Mission
{
    /// <summary>오늘의 막차: 날짜+게임 버전으로 동일 Seed를 생성한다.</summary>
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

                // 0은 "미지정"과 충돌하지 않게 보정
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
    }
}
