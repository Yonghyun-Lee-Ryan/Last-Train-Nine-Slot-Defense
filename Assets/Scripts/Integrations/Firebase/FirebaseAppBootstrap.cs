using System;
using UnityEngine;
#if LASTTRAIN_FIREBASE
using Firebase;
using Firebase.Extensions;
#endif

namespace LastTrain.Integrations
{
    /// <summary>
    /// FirebaseApp 공통 초기화. LASTTRAIN_FIREBASE + Firebase SDK + google-services.json 준비 후 사용.
    /// </summary>
    public static class FirebaseAppBootstrap
    {
        private static bool _started;
        private static bool _ready;
        private static bool _failed;

        public static bool IsReady => _ready;
        public static bool HasFailed => _failed;

        public static void EnsureStarted(Action<bool> onFinished = null)
        {
#if LASTTRAIN_FIREBASE
            if (_ready)
            {
                onFinished?.Invoke(true);
                return;
            }

            if (_failed)
            {
                onFinished?.Invoke(false);
                return;
            }

            if (_started)
            {
                onFinished?.Invoke(false);
                return;
            }

            _started = true;
            try
            {
                FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
                {
                    try
                    {
                        if (task.IsFaulted || task.IsCanceled)
                        {
                            _failed = true;
                            Debug.LogWarning("[FirebaseAppBootstrap] Dependency check faulted.");
                            onFinished?.Invoke(false);
                            return;
                        }

                        DependencyStatus status = task.Result;
                        if (status != DependencyStatus.Available)
                        {
                            _failed = true;
                            Debug.LogWarning("[FirebaseAppBootstrap] Dependencies unavailable: " + status);
                            onFinished?.Invoke(false);
                            return;
                        }

                        _ready = true;
                        Debug.Log("[FirebaseAppBootstrap] Firebase ready.");
                        onFinished?.Invoke(true);
                    }
                    catch (Exception ex)
                    {
                        _failed = true;
                        Debug.LogWarning("[FirebaseAppBootstrap] Init exception: " + ex.Message);
                        onFinished?.Invoke(false);
                    }
                });
            }
            catch (Exception ex)
            {
                _failed = true;
                Debug.LogWarning("[FirebaseAppBootstrap] Start exception: " + ex.Message);
                onFinished?.Invoke(false);
            }
#else
            onFinished?.Invoke(false);
#endif
        }
    }
}
