namespace LastTrain.Attendance
{
    /// <summary>7일 출석 보상 정의.</summary>
    public readonly struct AttendanceDayReward
    {
        public AttendanceDayReward(int ticketFragments, int accountXp, int freeSummonCharges)
        {
            TicketFragments = ticketFragments;
            AccountXp = accountXp;
            FreeSummonCharges = freeSummonCharges;
        }

        public int TicketFragments { get; }
        public int AccountXp { get; }
        public int FreeSummonCharges { get; }
    }

    public static class AttendanceRewardTable
    {
        public const int CycleLength = 7;

        private static readonly AttendanceDayReward[] Rewards =
        {
            new AttendanceDayReward(ticketFragments: 5, accountXp: 0, freeSummonCharges: 0),
            new AttendanceDayReward(ticketFragments: 10, accountXp: 25, freeSummonCharges: 0),
            new AttendanceDayReward(ticketFragments: 15, accountXp: 0, freeSummonCharges: 0),
            new AttendanceDayReward(ticketFragments: 20, accountXp: 50, freeSummonCharges: 0),
            new AttendanceDayReward(ticketFragments: 25, accountXp: 0, freeSummonCharges: 0),
            new AttendanceDayReward(ticketFragments: 30, accountXp: 75, freeSummonCharges: 0),
            new AttendanceDayReward(ticketFragments: 10, accountXp: 100, freeSummonCharges: 1),
        };

        public static AttendanceDayReward GetReward(int cycleDayIndex)
        {
            if (cycleDayIndex < 0)
            {
                cycleDayIndex = 0;
            }

            int index = cycleDayIndex % CycleLength;
            return Rewards[index];
        }

        public static string Describe(in AttendanceDayReward reward)
        {
            var parts = new System.Collections.Generic.List<string>(3);
            if (reward.TicketFragments > 0)
            {
                parts.Add($"조각 {reward.TicketFragments}");
            }

            if (reward.AccountXp > 0)
            {
                parts.Add($"XP {reward.AccountXp}");
            }

            if (reward.FreeSummonCharges > 0)
            {
                parts.Add($"무료 소환 {reward.FreeSummonCharges}");
            }

            return parts.Count > 0 ? string.Join(" · ", parts) : "보상 없음";
        }
    }
}
