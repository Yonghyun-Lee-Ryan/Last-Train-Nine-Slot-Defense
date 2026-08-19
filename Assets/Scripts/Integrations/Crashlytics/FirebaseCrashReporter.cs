using System;
using UnityEngine;
#if LASTTRAIN_FIREBASE
using Firebase.Crashlytics;
#endif

namespace LastTrain.Integrations
{
    /// <summary>
    /// Firebase Crashlytics 어댑터.
    /// LASTTRAIN_FIREBASE + SDK + google-services.json 준비 후 활성화한다.
    /// </summary>
    public sealed class FirebaseCrashReporter : ICrashReporter
    {
        private FirebaseCrashReporter()
        {
        }

        public static ICrashReporter TryCreate(PrivacyConsentService consent)
        {
            if (consent == null || !consent.CanCollectAnalytics)
            {
                return null;
            }

#if LASTTRAIN_FIREBASE
            try
            {
                FirebaseAppBootstrap.EnsureStarted(ready =>
                {
                    if (!ready)
                    {
                        return;
                    }

                    try
                    {
                        Crashlytics.ReportUncaughtExceptionsAsFatal = true;
                        Crashlytics.IsCrashlyticsCollectionEnabled = true;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning("[FirebaseCrashReporter] Enable failed: " + ex.Message);
                    }
                });
                return new FirebaseCrashReporter();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[FirebaseCrashReporter] TryCreate failed: " + ex.Message);
                return null;
            }
#else
            return null;
#endif
        }

        public void Log(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

#if LASTTRAIN_FIREBASE
            if (!FirebaseAppBootstrap.IsReady)
            {
                Debug.Log("[CrashReporter] " + message);
                return;
            }

            try
            {
                Crashlytics.Log(message);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[FirebaseCrashReporter] Log failed: " + ex.Message);
            }
#else
            Debug.Log("[CrashReporter] " + message);
#endif
        }

        public void LogException(Exception exception)
        {
            if (exception == null)
            {
                return;
            }

#if LASTTRAIN_FIREBASE
            if (!FirebaseAppBootstrap.IsReady)
            {
                Debug.LogException(exception);
                return;
            }

            try
            {
                Crashlytics.LogException(exception);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[FirebaseCrashReporter] LogException failed: " + ex.Message);
                Debug.LogException(exception);
            }
#else
            Debug.LogException(exception);
#endif
        }
    }
}
