using System;
using System.Globalization;

namespace LastTrain.Attendance
{
    /// <summary>출석 일자 키. WBS Unit 40 — 로컬 자정 기준.</summary>
    public static class AttendanceClock
    {
        public const string LocalDayKeyFormat = "yyyy-MM-dd";

        /// <summary>테스트용. null이면 DateTime.Now(로컬).</summary>
        public static Func<DateTime> LocalNowProvider { get; set; }

        public static DateTime LocalNow()
        {
            return LocalNowProvider?.Invoke() ?? DateTime.Now;
        }

        public static string GetLocalDayKey(DateTime? localNow = null)
        {
            DateTime now = localNow ?? LocalNow();
            return now.ToString(LocalDayKeyFormat, CultureInfo.InvariantCulture);
        }

        public static bool TryParseLocalDayKey(string key, out DateTime date)
        {
            date = default;
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            return DateTime.TryParseExact(
                key,
                LocalDayKeyFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out date);
        }

        public static bool IsYesterday(string lastClaimLocalDate, DateTime? localNow = null)
        {
            if (!TryParseLocalDayKey(lastClaimLocalDate, out DateTime last))
            {
                return false;
            }

            DateTime today = (localNow ?? LocalNow()).Date;
            return last.Date == today.AddDays(-1);
        }

        public static bool IsToday(string localDateKey, DateTime? localNow = null)
        {
            return string.Equals(localDateKey, GetLocalDayKey(localNow), StringComparison.Ordinal);
        }
    }
}
