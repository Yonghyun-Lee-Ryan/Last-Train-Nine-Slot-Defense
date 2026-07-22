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

        void ShowInterstitial(AdRequest request, Action<AdResult> onFinished);
    }
}
