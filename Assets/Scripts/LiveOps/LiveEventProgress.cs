using System;

namespace LastTrain.LiveOps
{
    /// <summary>라이브 이벤트 진행 저장. 스테이션 EventProgress와 이름이 겹치지 않게 Live 접두를 둔다.</summary>
    [Serializable]
    public sealed class LiveEventProgress
    {
        public string eventId = string.Empty;
        public int currencyBalance;
        public int currencyEarnedToday;
        public string lastEarnDayKey = string.Empty;
        public string[] claimedRewardIds = Array.Empty<string>();
        public bool finalized;

        public void EnsureDefaults()
        {
            eventId ??= string.Empty;
            lastEarnDayKey ??= string.Empty;
            claimedRewardIds ??= Array.Empty<string>();
            if (currencyBalance < 0)
            {
                currencyBalance = 0;
            }

            if (currencyEarnedToday < 0)
            {
                currencyEarnedToday = 0;
            }
        }

        public bool HasClaimed(string rewardId)
        {
            if (string.IsNullOrWhiteSpace(rewardId) || claimedRewardIds == null)
            {
                return false;
            }

            for (int i = 0; i < claimedRewardIds.Length; i++)
            {
                if (string.Equals(claimedRewardIds[i], rewardId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public void MarkClaimed(string rewardId)
        {
            if (string.IsNullOrWhiteSpace(rewardId) || HasClaimed(rewardId))
            {
                return;
            }

            var next = new string[claimedRewardIds.Length + 1];
            for (int i = 0; i < claimedRewardIds.Length; i++)
            {
                next[i] = claimedRewardIds[i];
            }

            next[claimedRewardIds.Length] = rewardId;
            claimedRewardIds = next;
        }
    }
}
