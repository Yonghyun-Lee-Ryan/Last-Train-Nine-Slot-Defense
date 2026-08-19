using System;
using System.Collections.Generic;
using UnityEngine;
#if LASTTRAIN_FIREBASE
using Firebase.Extensions;
using Firebase.RemoteConfig;
#endif

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

        /// <summary>Firebase 연결 시 비동기 fetch. 실패·미연결이면 ScriptableObject 기본값.</summary>
        public void FetchAndActivate(Action<bool> onFinished = null)
        {
            ApplyDefaults(succeeded: false);

#if LASTTRAIN_FIREBASE
            FirebaseAppBootstrap.EnsureStarted(ready =>
            {
                if (!ready)
                {
                    onFinished?.Invoke(false);
                    return;
                }

                BeginFirebaseFetch(onFinished);
            });
#else
            onFinished?.Invoke(false);
#endif
        }

        private void ApplyDefaults(bool succeeded)
        {
            Snapshot = _defaults != null ? _defaults.ToSnapshot() : RemoteConfigSnapshot.Default;
            LastFetchSucceeded = succeeded;
            RemoteConfigRuntime.Apply(Snapshot);
        }

#if LASTTRAIN_FIREBASE
        private void BeginFirebaseFetch(Action<bool> onFinished)
        {
            try
            {
                FirebaseRemoteConfig rc = FirebaseRemoteConfig.DefaultInstance;
                Dictionary<string, object> defaults = BuildFirebaseDefaults();

                rc.SetDefaultsAsync(defaults).ContinueWithOnMainThread(setTask =>
                {
                    if (setTask.IsFaulted)
                    {
                        Debug.LogWarning("[RemoteConfig] SetDefaults failed.");
                        ApplyDefaults(false);
                        onFinished?.Invoke(false);
                        return;
                    }

                    rc.FetchAsync(TimeSpan.Zero).ContinueWithOnMainThread(fetchTask =>
                    {
                        if (fetchTask.IsFaulted || fetchTask.IsCanceled)
                        {
                            Debug.LogWarning("[RemoteConfig] Fetch failed — using ScriptableObject defaults.");
                            ApplyDefaults(false);
                            onFinished?.Invoke(false);
                            return;
                        }

                        rc.ActivateAsync().ContinueWithOnMainThread(activateTask =>
                        {
                            try
                            {
                                Snapshot = ReadSnapshotFromFirebase(rc);
                                LastFetchSucceeded = true;
                                RemoteConfigRuntime.Apply(Snapshot);
                                onFinished?.Invoke(true);
                            }
                            catch (Exception ex)
                            {
                                Debug.LogWarning("[RemoteConfig] Activate/read failed: " + ex.Message);
                                ApplyDefaults(false);
                                onFinished?.Invoke(false);
                            }
                        });
                    });
                });
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RemoteConfig] Firebase fetch exception: " + ex.Message);
                ApplyDefaults(false);
                onFinished?.Invoke(false);
            }
        }

        private Dictionary<string, object> BuildFirebaseDefaults()
        {
            RemoteConfigSnapshot local = _defaults != null ? _defaults.ToSnapshot() : RemoteConfigSnapshot.Default;
            return new Dictionary<string, object>
            {
                ["interstitial_interval_seconds"] = local.InterstitialIntervalSeconds,
                ["rewarded_daily_limit"] = local.RewardedDailyLimit,
                ["runs_before_interstitial"] = local.RunsBeforeInterstitial,
                ["base_summon_cost"] = local.BaseSummonCost,
                ["summon_cost_increase"] = local.SummonCostIncrease,
                ["result_reward_multiplier"] = local.ResultRewardMultiplier,
                ["free_revive_per_run"] = local.FreeRevivePerRun,
                ["live_event_enabled"] = local.LiveEventEnabled,
                ["quick_run_reward_multiplier"] = local.QuickRunRewardMultiplier,
                ["live_ops_use_remote_catalog"] = local.LiveOpsUseRemoteCatalog,
                ["live_ops_catalog_json"] = local.LiveOpsCatalogJson,
                ["live_event_server_utc"] = local.LiveEventServerUtc,
            };
        }

        private static RemoteConfigSnapshot ReadSnapshotFromFirebase(FirebaseRemoteConfig rc)
        {
            return new RemoteConfigSnapshot(
                interstitialIntervalSeconds: (int)rc.GetValue("interstitial_interval_seconds").LongValue,
                rewardedDailyLimit: (int)rc.GetValue("rewarded_daily_limit").LongValue,
                runsBeforeInterstitial: (int)rc.GetValue("runs_before_interstitial").LongValue,
                baseSummonCost: (int)rc.GetValue("base_summon_cost").LongValue,
                summonCostIncrease: (int)rc.GetValue("summon_cost_increase").LongValue,
                resultRewardMultiplier: (float)rc.GetValue("result_reward_multiplier").DoubleValue,
                freeRevivePerRun: (int)rc.GetValue("free_revive_per_run").LongValue,
                liveEventEnabled: rc.GetValue("live_event_enabled").BooleanValue,
                loadedFromRemote: true,
                quickRunRewardMultiplier: (float)rc.GetValue("quick_run_reward_multiplier").DoubleValue,
                liveOpsUseRemoteCatalog: rc.GetValue("live_ops_use_remote_catalog").BooleanValue,
                liveOpsCatalogJson: rc.GetValue("live_ops_catalog_json").StringValue,
                liveEventServerUtc: rc.GetValue("live_event_server_utc").StringValue);
        }
#endif
    }
}
