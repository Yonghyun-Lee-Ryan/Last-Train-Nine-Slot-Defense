using UnityEngine;

namespace LastTrain.Integrations
{
    /// <summary>보상형·전면 광고 단위 ID. Editor/Dev는 테스트 ID, Release는 운영 ID.</summary>
    [CreateAssetMenu(fileName = "AdUnitConfig", menuName = "Last Train/Integration/Ad Unit Config")]
    public sealed class AdUnitConfig : ScriptableObject
    {
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

        public string GetRewardedUnitId(bool useTestIds)
        {
#if UNITY_IOS
            return useTestIds ? iosRewardedTestId : ResolveProduction(iosRewardedProductionId, iosRewardedTestId);
#else
            return useTestIds ? androidRewardedTestId : ResolveProduction(androidRewardedProductionId, androidRewardedTestId);
#endif
        }

        public string GetInterstitialUnitId(bool useTestIds)
        {
#if UNITY_IOS
            return useTestIds ? iosInterstitialTestId : ResolveProduction(iosInterstitialProductionId, iosInterstitialTestId);
#else
            return useTestIds ? androidInterstitialTestId : ResolveProduction(androidInterstitialProductionId, androidInterstitialTestId);
#endif
        }

        private static string ResolveProduction(string productionId, string fallbackTestId)
        {
            return string.IsNullOrWhiteSpace(productionId) ? fallbackTestId : productionId;
        }
    }
}
