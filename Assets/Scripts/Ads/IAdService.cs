using System;

namespace LastTrain.Ads
{
    public enum RewardedAdPlacement
    {
        PassengerReroll = 0,
        AbilityReroll = 1,
        Revive = 2,
        DoubleResultReward = 3,
        FreeSummon = 4,
        ShopRefresh = 5,
        StationRewardDouble = 6,
        AttendanceBonus = 7,
        SeasonPassTrack = 8,
    }

    public enum AdResult
    {
        Completed = 0,
        Cancelled = 1,
        Failed = 2,
        NotReady = 3,
    }

    [Serializable]
    public sealed class AdRequest
    {
        public AdRequest(RewardedAdPlacement placement, string requestId = null)
        {
            Placement = placement;
            RequestId = string.IsNullOrWhiteSpace(requestId)
                ? Guid.NewGuid().ToString("N")
                : requestId;
        }

        public string RequestId { get; }
        public RewardedAdPlacement Placement { get; }
    }

    public interface IAdService
    {
        bool IsRewardedReady(RewardedAdPlacement placement);

        void ShowRewardedAd(AdRequest request, Action<AdResult> onFinished);

        /// <summary>전면 광고. placement와 무관하며 보상형 한도를 소비하지 않는다.</summary>
        void ShowInterstitial(Action<AdResult> onFinished);
    }
}
