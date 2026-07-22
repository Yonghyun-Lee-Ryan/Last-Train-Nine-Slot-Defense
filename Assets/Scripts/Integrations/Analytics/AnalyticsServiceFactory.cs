using System.Collections.Generic;
using LastTrain.Analytics;
using UnityEngine;

namespace LastTrain.Integrations
{
    public static class AnalyticsServiceFactory
    {
        public static IAnalyticsService Create(PrivacyConsentService consent)
        {
            var sinks = new List<IAnalyticsService>();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            sinks.Add(new DebugAnalyticsService());
#endif

            if (consent != null && consent.CanCollectAnalytics)
            {
#if LASTTRAIN_FIREBASE
                IAnalyticsService firebase = FirebaseAnalyticsService.TryCreate();
                if (firebase != null)
                {
                    sinks.Add(firebase);
                }
#endif
            }

            if (sinks.Count == 0)
            {
                sinks.Add(new NoOpAnalyticsService());
            }

            IAnalyticsService inner = sinks.Count == 1 ? sinks[0] : new CompositeAnalyticsService(sinks);
            return new SafeAnalyticsService(inner);
        }
    }
}
