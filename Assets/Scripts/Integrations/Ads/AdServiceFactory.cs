using LastTrain.Ads;
using UnityEngine;

namespace LastTrain.Integrations
{
    public static class AdServiceFactory
    {
        public static IAdService Create(PrivacyConsentService consent, AdUnitConfig adUnits)
        {
            if (consent == null || !consent.CanRequestAds)
            {
                Debug.Log("[AdServiceFactory] Ads consent missing — NoOpAdService.");
                return new NoOpAdService();
            }

#if UNITY_EDITOR
            return new MockAdService();
#else
#if LASTTRAIN_ADMOB
            bool useTestIds = UseTestAdUnitIds || (adUnits != null && adUnits.UseGoogleTestAdUnits);
            IAdService admob = AdMobAdService.TryCreate(adUnits, useTestIds);
            if (admob != null)
            {
                Debug.Log("[AdServiceFactory] AdMobAdService active. useTestIds=" + useTestIds);
                return admob;
            }

            Debug.LogWarning("[AdServiceFactory] AdMob init failed — NoOpAdService.");
            return new NoOpAdService();
#else
#if DEVELOPMENT_BUILD
            return new MockAdService();
#else
            Debug.LogWarning("[AdServiceFactory] LASTTRAIN_ADMOB not defined — NoOpAdService.");
            return new NoOpAdService();
#endif
#endif
#endif
        }

        /// <summary>
        /// Play 스토어 등록 전에는 Release AAB도 Google 공식 테스트 광고 단위만 요청한다.
        /// 운영 단위 ID는 AdUnitConfig에 보관만 하고, 스토어 등록 후 플래그를 끈다.
        /// </summary>
        public static bool UseTestAdUnitIds => true;
    }
}
