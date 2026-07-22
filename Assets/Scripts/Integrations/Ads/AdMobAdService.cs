using LastTrain.Ads;
using UnityEngine;

namespace LastTrain.Integrations
{
    /// <summary>
    /// Google AdMob IAdService 어댑터.
    /// LASTTRAIN_ADMOB 심볼과 Google Mobile Ads SDK 설치 후 활성화한다.
    /// </summary>
    public sealed class AdMobAdService : IAdService
    {
        private readonly AdUnitConfig _config;
        private bool _initialized;

        private AdMobAdService(AdUnitConfig config)
        {
            _config = config;
        }

        public static IAdService TryCreate(AdUnitConfig config)
        {
            if (config == null)
            {
                return null;
            }

            var service = new AdMobAdService(config);
            return service.TryInitialize() ? service : null;
        }

        private bool TryInitialize()
        {
#if LASTTRAIN_ADMOB
            // Google Mobile Ads SDK 초기화 코드를 여기에 연결한다.
            // MobileAds.Initialize(_ => { _initialized = true; });
            Debug.LogWarning("[AdMobAdService] LASTTRAIN_ADMOB defined but SDK wiring is incomplete.");
            return false;
#else
            return false;
#endif
        }

        public bool IsRewardedReady(RewardedAdPlacement placement)
        {
            return _initialized;
        }

        public void ShowRewardedAd(AdRequest request, System.Action<AdResult> onFinished)
        {
            onFinished?.Invoke(_initialized ? AdResult.Failed : AdResult.NotReady);
        }

        public void ShowInterstitial(AdRequest request, System.Action<AdResult> onFinished)
        {
            onFinished?.Invoke(_initialized ? AdResult.Failed : AdResult.NotReady);
        }
    }
}
