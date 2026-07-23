using System;
using System.Globalization;

namespace LastTrain.Mission
{
    /// <summary>일일/주간 기간 키와 시간 역행 감지.</summary>
    public static class MissionClock
    {
        public const string DailyKeyFormat = "yyyy-MM-dd";

        /// <summary>테스트용. null이면 UtcNow.</summary>
        public static Func<DateTime> UtcNowProvider { get; set; }

        public static DateTime UtcNow()
        {
            return UtcNowProvider?.Invoke() ?? DateTime.UtcNow;
        }

        public static string GetDailyKey(DateTime utc)
        {
            return utc.ToString(DailyKeyFormat, CultureInfo.InvariantCulture);
        }

        /// <summary>ISO 주 번호 기반 주간 키 (예: 2026-W30).</summary>
        public static string GetWeeklyKey(DateTime utc)
        {
            var cal = CultureInfo.InvariantCulture.Calendar;
            int week = cal.GetWeekOfYear(utc, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            int year = utc.Year;
            // 1월 초가 이전 해 주차에 속할 수 있음
            if (utc.Month == 1 && week >= 52)
            {
                year -= 1;
            }
            else if (utc.Month == 12 && week == 1)
            {
                year += 1;
            }

            return $"{year}-W{week:00}";
        }

        public static string GetPeriodKey(MissionPeriod period, DateTime utc)
        {
            return period == MissionPeriod.Weekly ? GetWeeklyKey(utc) : GetDailyKey(utc);
        }

        /// <summary>
        /// 비정상 시간 역행 감지. 역행이면 true와 함께 로그용 메시지를 반환한다.
        /// 서버 시간이 없을 때 로컬(UTC) 기준 최소 방어.
        /// </summary>
        public static bool TryDetectClockRegression(
            string lastTrustedUtcIso,
            DateTime nowUtc,
            out string warning)
        {
            warning = null;
            if (string.IsNullOrWhiteSpace(lastTrustedUtcIso))
            {
                return false;
            }

            if (!DateTime.TryParse(
                    lastTrustedUtcIso,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out DateTime last))
            {
                return false;
            }

            if (last.Kind == DateTimeKind.Unspecified)
            {
                last = DateTime.SpecifyKind(last, DateTimeKind.Utc);
            }
            else
            {
                last = last.ToUniversalTime();
            }

            TimeSpan delta = last - nowUtc;
            if (delta > TimeSpan.FromMinutes(5))
            {
                warning =
                    $"[MissionClock] 시간 역행 감지: last={last:o}, now={nowUtc:o}, delta={delta}";
                return true;
            }

            return false;
        }

        public static string ToIso(DateTime utc)
        {
            return utc.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);
        }
    }
}
