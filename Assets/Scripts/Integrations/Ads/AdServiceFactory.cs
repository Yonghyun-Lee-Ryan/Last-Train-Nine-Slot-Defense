using System.Collections.Generic;
using LastTrain.Ads;
using LastTrain.Analytics;
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

#if LASTTRAIN_ADMOB
            IAdService admob = AdMobAdService.TryCreate(adUnits);
            if (admob != null)
            {
                return admob;
            }

            Debug.LogWarning("[AdServiceFactory] AdMob init failed — NoOpAdService.");
            return new NoOpAdService();
#else
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return new MockAdService();
#else
            Debug.LogWarning("[AdServiceFactory] LASTTRAIN_ADMOB not defined — NoOpAdService.");
            return new NoOpAdService();
#endif
#endif
        }

        public static bool UseTestAdUnitIds =>
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            true;
#else
            false;
#endif
    }
}
