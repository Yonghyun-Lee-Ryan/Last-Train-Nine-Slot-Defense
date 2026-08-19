using UnityEngine;

namespace LastTrain.Integrations
{
    /// <summary>
    /// 보상형·전면 광고 단위 ID.
    /// Play 스토어 등록 전에는 운영 ID를 요청하지 않는다.
    /// </summary>
    [CreateAssetMenu(fileName = "AdUnitConfig", menuName = "Last Train/Integration/Ad Unit Config")]
    public sealed class AdUnitConfig : ScriptableObject
    {
        [Header("Play 스토어 출시 전")]
        [Tooltip("켜면 Release AAB도 Google 공식 테스트 광고 단위만 요청한다. 실광고 정지를 피하려면 스토어 등록 전에는 끄지 않는다.")]
        [SerializeField] private bool useGoogleTestAdUnits = true;

        [Header("Android Rewarded (Google 테스트 ID 기본값)")]
        [SerializeField] private string androidRewardedTestId = "ca-app-pub-3940256099942544/5224354917";
        [SerializeField] private string androidRewardedProductionId = string.Empty;

        [Header("Android Interstitial")]
        [SerializeField] private string androidInterstitialTestId = "ca-app-pub-3940256099942544/1033173712";
        [SerializeField] private string androidInterstitialProductionId = string.Empty;

        [Header("iOS Rewarded")]
#pragma warning disable CS0414
        [SerializeField] private string iosRewardedTestId = "ca-app-pub-3940256099942544/1712485313";
        [SerializeField] private string iosRewardedProductionId = string.Empty;

        [Header("iOS Interstitial")]
        [SerializeField] private string iosInterstitialTestId = "ca-app-pub-3940256099942544/4411468910";
#pragma warning restore CS0414
        [SerializeField] private string iosInterstitialProductionId = string.Empty;

        /// <summary>true면 Editor/Dev/Release 모두 Google 테스트 광고 단위를 쓴다.</summary>
        public bool UseGoogleTestAdUnits => useGoogleTestAdUnits;

        public string GetRewardedUnitId(bool useTestIds)
        {
            if (ShouldUseTestIds(useTestIds))
            {
#if UNITY_IOS
                return iosRewardedTestId;
#else
                return androidRewardedTestId;
#endif
            }

#if UNITY_IOS
            return ResolveProduction(iosRewardedProductionId, iosRewardedTestId);
#else
            return ResolveProduction(androidRewardedProductionId, androidRewardedTestId);
#endif
        }

        public string GetInterstitialUnitId(bool useTestIds)
        {
            if (ShouldUseTestIds(useTestIds))
            {
#if UNITY_IOS
                return iosInterstitialTestId;
#else
                return androidInterstitialTestId;
#endif
            }

#if UNITY_IOS
            return ResolveProduction(iosInterstitialProductionId, iosInterstitialTestId);
#else
            return ResolveProduction(androidInterstitialProductionId, androidInterstitialTestId);
#endif
        }

        private bool ShouldUseTestIds(bool useTestIds)
        {
            return useGoogleTestAdUnits || useTestIds;
        }

        private static string ResolveProduction(string productionId, string fallbackTestId)
        {
            return string.IsNullOrWhiteSpace(productionId) ? fallbackTestId : productionId;
        }
    }
}
