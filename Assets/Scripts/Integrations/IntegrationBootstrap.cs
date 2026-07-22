using System;
using LastTrain.Ads;
using LastTrain.Analytics;
using LastTrain.Core;
using UnityEngine;

namespace LastTrain.Integrations
{
    /// <summary>
    /// 광고·분석·Remote Config·Crashlytics·개인정보 동의를 한곳에서 초기화한다.
    /// AppRoot가 SDK 타입을 직접 참조하지 않도록 한다.
    /// </summary>
    public sealed class IntegrationBootstrap : IDisposable
    {
        private AdUnitConfig _adUnits;
        private SceneLoader _sceneLoader;
        private Func<GameSession> _sessionProvider;
        private bool _logHooked;

        public PrivacyConsentService Privacy { get; } = new PrivacyConsentService();
        public RemoteConfigService RemoteConfig { get; } = new RemoteConfigService();
        public ICrashReporter CrashReporter { get; private set; }
        public InterstitialAdCoordinator Interstitials { get; private set; }

        public void Initialize(
            AdUnitConfig adUnits,
            RemoteConfigDefaults remoteDefaults,
            SceneLoader sceneLoader,
            Func<GameSession> sessionProvider)
        {
            _adUnits = adUnits ?? ScriptableObject.CreateInstance<AdUnitConfig>();
            _sceneLoader = sceneLoader;
            _sessionProvider = sessionProvider;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Privacy.Initialize(autoGrantInEditor: true);
#else
            Privacy.Initialize(autoGrantInEditor: false);
#endif

            RemoteConfig.Initialize(remoteDefaults ?? ScriptableObject.CreateInstance<RemoteConfigDefaults>());
            RemoteConfig.FetchAndActivate();

            CrashReporter = FirebaseCrashReporter.TryCreate(Privacy) ?? new DebugCrashReporter();
            HookCrashReporting();
        }

        public AnalyticsCoordinator CreateAnalyticsCoordinator()
        {
            IAnalyticsService service = AnalyticsServiceFactory.Create(Privacy);
            return new AnalyticsCoordinator(service);
        }

        public AdCoordinator CreateAdCoordinator(AnalyticsCoordinator analytics)
        {
            var limits = new AdLimitService();
            ApplyRemoteConfigToLimits(limits);

            var rewards = new AdRewardService(limits);
            IAdService adService = AdServiceFactory.Create(Privacy, _adUnits);
            var coordinator = new AdCoordinator(adService, limits, rewards)
            {
                Analytics = analytics,
            };

            coordinator.RewardedShowFinished += HandleRewardedShowFinished;

            Interstitials?.Unsubscribe();
            Interstitials = new InterstitialAdCoordinator(
                coordinator,
                Privacy,
                _sceneLoader,
                _sessionProvider);
            Interstitials.Subscribe();

            return coordinator;
        }

        public void NotifyRunCompleted()
        {
            Interstitials?.NotifyRunCompleted();
        }

        public void RefreshRemoteConfig(Action<bool> onFinished = null)
        {
            RemoteConfig.FetchAndActivate(success =>
            {
                onFinished?.Invoke(success);
            });
        }

        private void ApplyRemoteConfigToLimits(AdLimitService limits)
        {
            if (limits == null)
            {
                return;
            }

            limits.ApplyRemoteConfig(RemoteConfigRuntime.Current);
        }

        private void HandleRewardedShowFinished(AdResult result)
        {
            if (result == AdResult.Completed)
            {
                Interstitials?.NotifyRewardedCompleted();
            }
        }

        private void HookCrashReporting()
        {
            if (_logHooked)
            {
                return;
            }

            Application.logMessageReceived += HandleLogMessage;
            AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;
            _logHooked = true;
        }

        private void UnhookCrashReporting()
        {
            if (!_logHooked)
            {
                return;
            }

            Application.logMessageReceived -= HandleLogMessage;
            AppDomain.CurrentDomain.UnhandledException -= HandleUnhandledException;
            _logHooked = false;
        }

        private void HandleLogMessage(string condition, string stackTrace, LogType type)
        {
            if (CrashReporter == null)
            {
                return;
            }

            if (type == LogType.Exception)
            {
                CrashReporter.LogException(new Exception($"{condition}\n{stackTrace}"));
                return;
            }

            if (type == LogType.Error)
            {
                CrashReporter.Log($"[Error] {condition}\n{stackTrace}");
            }
        }

        private void HandleUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            if (CrashReporter == null)
            {
                return;
            }

            if (args.ExceptionObject is Exception ex)
            {
                CrashReporter.LogException(ex);
            }
        }

        public void Dispose()
        {
            Interstitials?.Unsubscribe();
            UnhookCrashReporting();
        }
    }
}
