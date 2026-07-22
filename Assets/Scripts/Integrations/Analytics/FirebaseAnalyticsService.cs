using System.Collections.Generic;
using LastTrain.Analytics;
using UnityEngine;

namespace LastTrain.Integrations
{
    /// <summary>
    /// Firebase Analytics IAnalyticsService 어댑터.
    /// LASTTRAIN_FIREBASE 심볼과 Firebase SDK 설치 후 활성화한다.
    /// </summary>
    public sealed class FirebaseAnalyticsService : IAnalyticsService
    {
        private FirebaseAnalyticsService()
        {
        }

        public static IAnalyticsService TryCreate()
        {
#if LASTTRAIN_FIREBASE
            Debug.LogWarning("[FirebaseAnalyticsService] LASTTRAIN_FIREBASE defined but SDK wiring is incomplete.");
            return null;
#else
            return null;
#endif
        }

        public void Track(string eventName, IDictionary<string, object> parameters = null)
        {
#if LASTTRAIN_FIREBASE
            // FirebaseAnalytics.LogEvent(eventName, Convert(parameters));
#endif
        }

        public void Track(AnalyticsEvent analyticsEvent)
        {
            if (analyticsEvent == null)
            {
                return;
            }

            Track(analyticsEvent.Name, analyticsEvent.Parameters);
        }
    }
}
