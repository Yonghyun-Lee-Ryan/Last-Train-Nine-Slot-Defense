using System;
using LastTrain.Ads;
using LastTrain.Analytics;
using LastTrain.Integrations;
using NUnit.Framework;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class IntegrationServiceTests
    {
        [SetUp]
        public void SetUp()
        {
            RemoteConfigRuntime.Apply(RemoteConfigSnapshot.Default);
            PlayerPrefs.DeleteKey("lasttrain.consent.ads");
            PlayerPrefs.DeleteKey("lasttrain.consent.analytics");
            PlayerPrefs.Save();
        }

        [Test]
        public void PrivacyConsent_EditorAutoGrant_AllowsAdsAndAnalytics()
        {
            var privacy = new PrivacyConsentService();
            privacy.Initialize(autoGrantInEditor: true);

            Assert.IsTrue(privacy.CanRequestAds);
            Assert.IsTrue(privacy.CanCollectAnalytics);
        }

        [Test]
        public void PrivacyConsent_NoGrant_BlocksAdsAndAnalytics()
        {
            var privacy = new PrivacyConsentService();
            privacy.Initialize(autoGrantInEditor: false);
            privacy.SetAdsConsent(false);
            privacy.SetAnalyticsConsent(false);

            IAdService ads = AdServiceFactory.Create(privacy, ScriptableObject.CreateInstance<AdUnitConfig>());
            Assert.IsInstanceOf<NoOpAdService>(ads);

            IAnalyticsService analytics = AnalyticsServiceFactory.Create(privacy);
            Assert.IsInstanceOf<SafeAnalyticsService>(analytics);
        }

        [Test]
        public void RemoteConfig_FetchFailure_UsesScriptableDefaults()
        {
            var defaults = ScriptableObject.CreateInstance<RemoteConfigDefaults>();
            var service = new RemoteConfigService();
            service.Initialize(defaults);

            bool? success = null;
            service.FetchAndActivate(ok => success = ok);

            Assert.IsFalse(success);
            Assert.AreEqual(defaults.ToSnapshot().BaseSummonCost, service.Snapshot.BaseSummonCost);
            Assert.AreEqual(defaults.ToSnapshot().BaseSummonCost, RemoteConfigRuntime.Current.BaseSummonCost);
        }

        [Test]
        public void AdLimitService_AppliesRemoteConfigDailyLimit()
        {
            var limits = new AdLimitService { UtcNowProvider = () => new DateTime(2026, 7, 22, 12, 0, 0, DateTimeKind.Utc) };
            limits.ApplyRemoteConfig(new RemoteConfigSnapshot(
                interstitialIntervalSeconds: 180,
                rewardedDailyLimit: 1,
                runsBeforeInterstitial: 3,
                baseSummonCost: 10,
                summonCostIncrease: 2,
                resultRewardMultiplier: 1f,
                freeRevivePerRun: 1,
                liveEventEnabled: false,
                loadedFromRemote: true));

            limits.BeginRun();
            Assert.IsTrue(limits.TryConsume(RewardedAdPlacement.PassengerReroll));
            Assert.IsFalse(limits.CanUse(RewardedAdPlacement.AbilityReroll));
        }

        [Test]
        public void CompositeAnalytics_FansOutToAllSinks()
        {
            var first = new CountingAnalytics();
            var second = new CountingAnalytics();
            var composite = new CompositeAnalyticsService(new IAnalyticsService[] { first, second });

            composite.Track("test_event");

            Assert.AreEqual(1, first.Count);
            Assert.AreEqual(1, second.Count);
        }

        [Test]
        public void PrivacyConsent_MarkPromptCompleted_Persists()
        {
            var privacy = new PrivacyConsentService();
            privacy.Initialize(autoGrantInEditor: false);
            privacy.MarkConsentPromptCompleted();

            var reloaded = new PrivacyConsentService();
            reloaded.Initialize(autoGrantInEditor: false);
            Assert.IsTrue(reloaded.HasCompletedConsentPrompt);
        }

        [Test]
        public void AdUnitConfig_UsesTestIdsInDevelopment()
        {
            var config = ScriptableObject.CreateInstance<AdUnitConfig>();
            string testId = config.GetRewardedUnitId(useTestIds: true);
            Assert.IsFalse(string.IsNullOrWhiteSpace(testId));
            Assert.IsTrue(testId.Contains("3940256099942544"));
        }

        private sealed class CountingAnalytics : IAnalyticsService
        {
            public int Count { get; private set; }

            public void Track(string eventName, System.Collections.Generic.IDictionary<string, object> parameters = null)
            {
                Count++;
            }

            public void Track(AnalyticsEvent analyticsEvent)
            {
                Count++;
            }
        }
    }
}
