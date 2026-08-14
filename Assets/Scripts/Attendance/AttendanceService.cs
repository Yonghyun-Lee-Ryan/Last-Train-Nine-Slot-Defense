using System;
using LastTrain.Save;
using UnityEngine;

namespace LastTrain.Attendance
{
    public readonly struct AttendanceGrant
    {
        public AttendanceGrant(int cycleDayIndex, int ticketFragments, int accountXp, int freeSummonCharges)
        {
            CycleDayIndex = cycleDayIndex;
            TicketFragments = ticketFragments;
            AccountXp = accountXp;
            FreeSummonCharges = freeSummonCharges;
        }

        public int CycleDayIndex { get; }
        public int TicketFragments { get; }
        public int AccountXp { get; }
        public int FreeSummonCharges { get; }
    }

    /// <summary>7일 출석 상태·수령. MetaSaveData와 연동한다.</summary>
    public static class AttendanceService
    {
        public static void EnsureDayState(MetaSaveData meta, DateTime? localNow = null)
        {
            if (meta == null)
            {
                return;
            }

            meta.EnsureDefaults();
            string today = AttendanceClock.GetLocalDayKey(localNow);
            string lastClaim = meta.attendanceLastClaimLocalDate ?? string.Empty;

            if (string.IsNullOrWhiteSpace(lastClaim)
                || AttendanceClock.IsToday(lastClaim, localNow)
                || AttendanceClock.IsYesterday(lastClaim, localNow))
            {
                return;
            }

            meta.attendanceCycleDay = 0;
        }

        public static bool CanClaimToday(MetaSaveData meta, DateTime? localNow = null)
        {
            if (meta == null)
            {
                return false;
            }

            meta.EnsureDefaults();
            EnsureDayState(meta, localNow);
            return !AttendanceClock.IsToday(meta.attendanceLastClaimLocalDate, localNow);
        }

        public static bool CanClaimAdBonus(MetaSaveData meta, DateTime? localNow = null)
        {
            if (meta == null)
            {
                return false;
            }

            meta.EnsureDefaults();
            EnsureDayState(meta, localNow);
            string today = AttendanceClock.GetLocalDayKey(localNow);
            return AttendanceClock.IsToday(meta.attendanceLastClaimLocalDate, localNow)
                   && !AttendanceClock.IsToday(meta.attendanceLastAdBonusLocalDate, localNow);
        }

        public static bool TryClaimBase(MetaSaveData meta, out AttendanceGrant grant, DateTime? localNow = null)
        {
            grant = default;
            if (meta == null || !CanClaimToday(meta, localNow))
            {
                return false;
            }

            int dayIndex = Mathf.Clamp(meta.attendanceCycleDay, 0, AttendanceRewardTable.CycleLength - 1);
            AttendanceDayReward reward = AttendanceRewardTable.GetReward(dayIndex);
            ApplyGrant(meta, reward);

            meta.attendanceLastClaimLocalDate = AttendanceClock.GetLocalDayKey(localNow);
            meta.attendanceCycleDay = (dayIndex + 1) % AttendanceRewardTable.CycleLength;

            grant = new AttendanceGrant(dayIndex, reward.TicketFragments, reward.AccountXp, reward.FreeSummonCharges);
            return true;
        }

        public static bool TryGrantAdBonus(MetaSaveData meta, out AttendanceGrant grant, DateTime? localNow = null)
        {
            grant = default;
            if (meta == null || !CanClaimAdBonus(meta, localNow))
            {
                return false;
            }

            int dayIndex = (meta.attendanceCycleDay + AttendanceRewardTable.CycleLength - 1)
                % AttendanceRewardTable.CycleLength;
            AttendanceDayReward reward = AttendanceRewardTable.GetReward(dayIndex);
            ApplyGrant(meta, reward);
            meta.attendanceLastAdBonusLocalDate = AttendanceClock.GetLocalDayKey(localNow);

            grant = new AttendanceGrant(dayIndex, reward.TicketFragments, reward.AccountXp, reward.FreeSummonCharges);
            return true;
        }

        public static int GetNextRewardDayIndex(MetaSaveData meta, DateTime? localNow = null)
        {
            if (meta == null)
            {
                return 0;
            }

            meta.EnsureDefaults();
            EnsureDayState(meta, localNow);
            return Mathf.Clamp(meta.attendanceCycleDay, 0, AttendanceRewardTable.CycleLength - 1);
        }

        public static int GetCompletedDayCount(MetaSaveData meta, DateTime? localNow = null)
        {
            if (meta == null)
            {
                return 0;
            }

            meta.EnsureDefaults();
            EnsureDayState(meta, localNow);
            int cycleDay = Mathf.Clamp(meta.attendanceCycleDay, 0, AttendanceRewardTable.CycleLength - 1);
            if (AttendanceClock.IsToday(meta.attendanceLastClaimLocalDate, localNow))
            {
                return cycleDay == 0 ? AttendanceRewardTable.CycleLength : cycleDay;
            }

            return cycleDay;
        }

        private static void ApplyGrant(MetaSaveData meta, in AttendanceDayReward reward)
        {
            if (reward.TicketFragments > 0)
            {
                meta.ticketFragments = Math.Max(0, meta.ticketFragments + reward.TicketFragments);
            }

            if (reward.AccountXp > 0)
            {
                meta.accountXp = Math.Max(0, meta.accountXp + reward.AccountXp);
                while (meta.accountXp >= MetaProgressionDefaults.AccountXpPerLevel)
                {
                    meta.accountXp -= MetaProgressionDefaults.AccountXpPerLevel;
                    meta.accountLevel++;
                }
            }

            if (reward.FreeSummonCharges > 0)
            {
                meta.metaPendingFreeSummonCharges = Math.Max(
                    0,
                    meta.metaPendingFreeSummonCharges + reward.FreeSummonCharges);
            }
        }
    }
}
