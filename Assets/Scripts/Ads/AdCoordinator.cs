using System;
using System.Collections.Generic;
using LastTrain.Analytics;
using LastTrain.Core;
using LastTrain.Run;
using UnityEngine;

namespace LastTrain.Ads
{
    /// <summary>
    /// UI/게임 로직이 IAdService만 쓰도록 감싸는 진입점.
    /// 표시 중 중복 요청을 막고, Completed일 때만 보상을 지급한다.
    /// </summary>
    public sealed class AdCoordinator
    {
        private readonly IAdService _adService;
        private readonly AdLimitService _limits;
        private readonly AdRewardService _rewards;
        private bool _isShowing;

        public AdCoordinator(IAdService adService, AdLimitService limits, AdRewardService rewards)
        {
            _adService = adService ?? throw new ArgumentNullException(nameof(adService));
            _limits = limits ?? throw new ArgumentNullException(nameof(limits));
            _rewards = rewards ?? throw new ArgumentNullException(nameof(rewards));
        }

        public IAdService AdService => _adService;
        public AdLimitService Limits => _limits;
        public AdRewardService Rewards => _rewards;
        public bool IsShowing => _isShowing;

        /// <summary>선택적. AppRoot가 연결한다.</summary>
        public AnalyticsCoordinator Analytics { get; set; }

        /// <summary>보상형 광고 표시가 끝났을 때(전면 광고 쿨다운 등).</summary>
        public event Action<AdResult> RewardedShowFinished;

        public bool IsReady(RewardedAdPlacement placement)
        {
            return !_isShowing
                   && _limits.CanUse(placement)
                   && _adService.IsRewardedReady(placement);
        }

        /// <summary>리롤 UI 버튼용. 게임 쪽 RemainingAdRerolls와 함께 사용한다.</summary>
        public bool CanOfferReroll(RewardedAdPlacement placement)
        {
            return placement == RewardedAdPlacement.PassengerReroll
                   || placement == RewardedAdPlacement.AbilityReroll
                ? !_isShowing
                  && _limits.GetRemaining(placement) > 0
                  && _adService.IsRewardedReady(placement)
                : IsReady(placement);
        }

        /// <summary>
        /// 광고 표시 후 Completed이면 onGrantBeforeConsume 검증 없이
        /// AdRewardService가 한도·RequestId를 소비하며 onGranted를 실행한다.
        /// </summary>
        public void ShowRewarded(
            RewardedAdPlacement placement,
            Action onGranted,
            Action<AdResult> onFinished = null)
        {
            BeginShow(placement, request =>
            {
                bool granted = _rewards.TryGrant(request, AdResult.Completed, onGranted);
                return granted ? AdResult.Completed : AdResult.Failed;
            }, onFinished);
        }

        public void ShowRevive(GameSession session, Action<AdResult> onFinished = null)
        {
            BeginShow(RewardedAdPlacement.Revive, request =>
            {
                bool granted = _rewards.TryGrantRevive(session, request, AdResult.Completed);
                return granted ? AdResult.Completed : AdResult.Failed;
            }, onFinished);
        }

        public void ShowDoubleResultReward(Action<AdResult> onFinished = null)
        {
            BeginShow(RewardedAdPlacement.DoubleResultReward, request =>
            {
                bool granted = _rewards.TryGrantDoubleResultTickets(request, AdResult.Completed);
                return granted ? AdResult.Completed : AdResult.Failed;
            }, onFinished);
        }

        public void ShowStationRewardDouble(
            RunState runState,
            int baseCoins,
            Action<AdResult> onFinished = null)
        {
            BeginShow(RewardedAdPlacement.StationRewardDouble, request =>
            {
                bool granted = _rewards.TryGrantStationRewardDouble(
                    runState,
                    baseCoins,
                    request,
                    AdResult.Completed);
                return granted ? AdResult.Completed : AdResult.Failed;
            }, onFinished);
        }

        private void BeginShow(
            RewardedAdPlacement placement,
            Func<AdRequest, AdResult> onCompletedGrant,
            Action<AdResult> onFinished)
        {
            TrackAd(AnalyticsEventNames.RewardedAdOffered, placement);

            if (_isShowing)
            {
                TrackAd(AnalyticsEventNames.RewardedAdFailed, placement, reason: "already_showing");
                onFinished?.Invoke(AdResult.Failed);
                return;
            }

            if (!_limits.CanUse(placement))
            {
                TrackAd(AnalyticsEventNames.RewardedAdFailed, placement, reason: "limit");
                onFinished?.Invoke(AdResult.NotReady);
                return;
            }

            if (!_adService.IsRewardedReady(placement))
            {
                TrackAd(AnalyticsEventNames.RewardedAdFailed, placement, reason: "not_ready");
                onFinished?.Invoke(AdResult.NotReady);
                return;
            }

            var request = new AdRequest(placement);
            _isShowing = true;
            TrackAd(AnalyticsEventNames.RewardedAdStarted, placement, request.RequestId);

            try
            {
                _adService.ShowRewardedAd(request, result =>
                {
                    AdResult finalResult = result;
                    try
                    {
                        if (result == AdResult.Completed)
                        {
                            finalResult = onCompletedGrant(request);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        finalResult = AdResult.Failed;
                    }
                    finally
                    {
                        _isShowing = false;
                        TrackAdResult(placement, request.RequestId, finalResult);
                        RewardedShowFinished?.Invoke(finalResult);
                        onFinished?.Invoke(finalResult);
                    }
                });
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                _isShowing = false;
                TrackAd(AnalyticsEventNames.RewardedAdFailed, placement, request.RequestId, "exception");
                onFinished?.Invoke(AdResult.Failed);
            }
        }

        private void TrackAdResult(RewardedAdPlacement placement, string requestId, AdResult result)
        {
            string eventName = result switch
            {
                AdResult.Completed => AnalyticsEventNames.RewardedAdCompleted,
                AdResult.Cancelled => AnalyticsEventNames.RewardedAdCancelled,
                _ => AnalyticsEventNames.RewardedAdFailed,
            };
            TrackAd(eventName, placement, requestId, result.ToString());
        }

        private void TrackAd(
            string eventName,
            RewardedAdPlacement placement,
            string requestId = null,
            string reason = null)
        {
            if (Analytics == null)
            {
                return;
            }

            var extra = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["placement"] = placement.ToString(),
            };
            if (!string.IsNullOrWhiteSpace(requestId))
            {
                extra["request_id"] = requestId;
            }

            if (!string.IsNullOrWhiteSpace(reason))
            {
                extra["reason"] = reason;
            }

            Analytics.Track(eventName, extra);
        }
    }
}
