using System.Collections.Generic;
using LastTrain.Data;

namespace LastTrain.LiveOps
{
    /// <summary>시즌 패스 보상 버튼에 내부 ID 대신 조각·경험치·해금을 보여 준다.</summary>
    public static class EventRewardStepFormatter
    {
        public static string FormatLane(RewardTrackLane lane)
        {
            return lane == RewardTrackLane.Ad ? "광고" : "무료";
        }

        public static string FormatSummary(EventRewardStep step, GameDatabase database)
        {
            if (step == null)
            {
                return "보상";
            }

            var parts = new List<string>(3);
            if (step.ticketFragments > 0)
            {
                parts.Add($"조각 {step.ticketFragments}");
            }

            if (step.accountXp > 0)
            {
                parts.Add($"계정 경험치 {step.accountXp}");
            }

            if (!string.IsNullOrWhiteSpace(step.unlockPassengerId))
            {
                string unlockName = step.unlockPassengerId;
                if (database != null && database.TryGetPassenger(step.unlockPassengerId, out PassengerData passenger)
                    && passenger != null
                    && !string.IsNullOrWhiteSpace(passenger.DisplayName))
                {
                    unlockName = passenger.DisplayName;
                }

                parts.Add($"해금 {unlockName}");
            }

            return parts.Count == 0 ? "보상" : string.Join(" · ", parts);
        }

        public static string FormatButtonLabel(EventRewardStep step, bool claimed, GameDatabase database)
        {
            string lane = FormatLane(step != null ? step.lane : RewardTrackLane.Free);
            string summary = FormatSummary(step, database);
            if (claimed)
            {
                return $"{lane} 수령 완료 · {summary}";
            }

            int cost = step != null ? step.requiredCurrency : 0;
            return step != null && step.lane == RewardTrackLane.Ad
                ? $"{lane}로 수령 · {summary} ({cost})"
                : $"{lane} 수령 · {summary} ({cost})";
        }
    }
}
