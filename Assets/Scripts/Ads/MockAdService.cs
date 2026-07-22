using System;
using UnityEngine;

namespace LastTrain.Ads
{
    /// <summary>
    /// Editor/개발용 Mock 광고.
    /// AutoResult가 설정되면 즉시 그 결과를 반환하고,
    /// 아니면 런타임 팝업으로 Completed/Cancelled/Failed를 고른다.
    /// </summary>
    public sealed class MockAdService : IAdService
    {
        /// <summary>테스트용. null이면 팝업(또는 기본 Completed).</summary>
        public AdResult? AutoResult { get; set; }

        public bool ForceNotReady { get; set; }

        public bool IsRewardedReady(RewardedAdPlacement placement)
        {
            return !ForceNotReady;
        }

        public void ShowRewardedAd(AdRequest request, Action<AdResult> onFinished)
        {
            if (request == null)
            {
                onFinished?.Invoke(AdResult.Failed);
                return;
            }

            if (ForceNotReady || !IsRewardedReady(request.Placement))
            {
                onFinished?.Invoke(AdResult.NotReady);
                return;
            }

            if (AutoResult.HasValue)
            {
                onFinished?.Invoke(AutoResult.Value);
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            MockAdPopup.Show(request, onFinished);
#else
            // 플레이어 개발 빌드 폴백: 성공 처리하지 않고 Cancelled
            onFinished?.Invoke(AdResult.Cancelled);
#endif
        }

        public void ShowInterstitial(AdRequest request, Action<AdResult> onFinished)
        {
            ShowRewardedAd(request, onFinished);
        }
    }

    /// <summary>광고 SDK 미연결/실패 시 사용. 항상 NotReady.</summary>
    public sealed class NoOpAdService : IAdService
    {
        public bool IsRewardedReady(RewardedAdPlacement placement) => false;

        public void ShowRewardedAd(AdRequest request, Action<AdResult> onFinished)
        {
            onFinished?.Invoke(AdResult.NotReady);
        }

        public void ShowInterstitial(AdRequest request, Action<AdResult> onFinished)
        {
            onFinished?.Invoke(AdResult.NotReady);
        }
    }
}
