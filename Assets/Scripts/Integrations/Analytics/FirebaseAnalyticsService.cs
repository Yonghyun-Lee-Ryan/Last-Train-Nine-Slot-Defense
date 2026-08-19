using System;
using System.Collections.Generic;
using LastTrain.Analytics;
using UnityEngine;
#if LASTTRAIN_FIREBASE
using Firebase.Analytics;
#endif

namespace LastTrain.Integrations
{
    /// <summary>
    /// Firebase Analytics IAnalyticsService 어댑터.
    /// LASTTRAIN_FIREBASE + Firebase SDK + google-services.json 준비 후 활성화한다.
    /// </summary>
    public sealed class FirebaseAnalyticsService : IAnalyticsService
    {
        private FirebaseAnalyticsService()
        {
        }

        public static IAnalyticsService TryCreate()
        {
#if LASTTRAIN_FIREBASE
            try
            {
                FirebaseAppBootstrap.EnsureStarted();
                return new FirebaseAnalyticsService();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[FirebaseAnalyticsService] TryCreate failed: " + ex.Message);
                return null;
            }
#else
            return null;
#endif
        }

        public void Track(string eventName, IDictionary<string, object> parameters = null)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                return;
            }

#if LASTTRAIN_FIREBASE
            if (!FirebaseAppBootstrap.IsReady)
            {
                FirebaseAppBootstrap.EnsureStarted();
                return;
            }

            try
            {
                if (parameters == null || parameters.Count == 0)
                {
                    FirebaseAnalytics.LogEvent(eventName);
                    return;
                }

                var list = new List<Parameter>(parameters.Count);
                foreach (KeyValuePair<string, object> pair in parameters)
                {
                    if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value == null)
                    {
                        continue;
                    }

                    list.Add(ToParameter(pair.Key, pair.Value));
                }

                FirebaseAnalytics.LogEvent(eventName, list.ToArray());
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[FirebaseAnalyticsService] LogEvent failed: " + ex.Message);
            }
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

#if LASTTRAIN_FIREBASE
        private static Parameter ToParameter(string key, object value)
        {
            switch (value)
            {
                case string s:
                    return new Parameter(key, s);
                case bool b:
                    return new Parameter(key, b ? 1L : 0L);
                case int i:
                    return new Parameter(key, (long)i);
                case long l:
                    return new Parameter(key, l);
                case float f:
                    return new Parameter(key, f);
                case double d:
                    return new Parameter(key, d);
                default:
                    return new Parameter(key, value.ToString());
            }
        }
#endif
    }
}
