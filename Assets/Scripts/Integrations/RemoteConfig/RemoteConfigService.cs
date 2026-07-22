using System;
using UnityEngine;

namespace LastTrain.Integrations
{
    /// <summary>
    /// Remote Config fetch. Firebase 미설치/실패 시 ScriptableObject 기본값을 사용한다.
    /// </summary>
    public sealed class RemoteConfigService
    {
        private RemoteConfigDefaults _defaults;

        public RemoteConfigSnapshot Snapshot { get; private set; } = RemoteConfigSnapshot.Default;
        public bool LastFetchSucceeded { get; private set; }

        public void Initialize(RemoteConfigDefaults defaults)
        {
            _defaults = defaults;
            Snapshot = _defaults != null ? _defaults.ToSnapshot() : RemoteConfigSnapshot.Default;
            RemoteConfigRuntime.Apply(Snapshot);
        }

        /// <summary>비동기 fetch 시뮬레이션. Firebase 연결 시 교체한다.</summary>
        public void FetchAndActivate(Action<bool> onFinished = null)
        {
            try
            {
#if LASTTRAIN_FIREBASE
                if (TryFetchFromFirebase(out RemoteConfigSnapshot remote))
                {
                    Snapshot = remote;
                    LastFetchSucceeded = true;
                    RemoteConfigRuntime.Apply(Snapshot);
                    onFinished?.Invoke(true);
                    return;
                }
#endif
                Snapshot = _defaults != null ? _defaults.ToSnapshot() : RemoteConfigSnapshot.Default;
                LastFetchSucceeded = false;
                RemoteConfigRuntime.Apply(Snapshot);
                onFinished?.Invoke(false);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[RemoteConfig] Fetch failed: {e.Message}");
                Snapshot = _defaults != null ? _defaults.ToSnapshot() : RemoteConfigSnapshot.Default;
                LastFetchSucceeded = false;
                RemoteConfigRuntime.Apply(Snapshot);
                onFinished?.Invoke(false);
            }
        }

#if LASTTRAIN_FIREBASE
        private static bool TryFetchFromFirebase(out RemoteConfigSnapshot snapshot)
        {
            snapshot = RemoteConfigSnapshot.Default;
            // Firebase Remote Config SDK 연결 시 구현한다.
            return false;
        }
#endif
    }
}
