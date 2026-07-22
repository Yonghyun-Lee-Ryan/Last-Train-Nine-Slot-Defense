using UnityEngine;

namespace LastTrain.Integrations
{
    public sealed class FirebaseCrashReporter : ICrashReporter
    {
        public static ICrashReporter TryCreate(PrivacyConsentService consent)
        {
            if (consent == null || !consent.CanCollectAnalytics)
            {
                return null;
            }

#if LASTTRAIN_FIREBASE
            Debug.LogWarning("[FirebaseCrashReporter] LASTTRAIN_FIREBASE defined but SDK wiring is incomplete.");
            return null;
#else
            return null;
#endif
        }

        public void Log(string message)
        {
#if LASTTRAIN_FIREBASE
            // Firebase.Crashlytics.Crashlytics.Log(message);
#endif
        }

        public void LogException(System.Exception exception)
        {
#if LASTTRAIN_FIREBASE
            // Firebase.Crashlytics.Crashlytics.LogException(exception);
#endif
            Debug.LogException(exception);
        }
    }
}
