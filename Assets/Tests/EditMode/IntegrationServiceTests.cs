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
        public void AdServiceFactory_EditorWithConsent_UsesMockInEditMode()
        {
            var privacy = new PrivacyConsentService();
            privacy.Initialize(autoGrantInEditor: true);
            IAdService ads = AdServiceFactory.Create(privacy, ScriptableObject.CreateInstance<AdUnitConfig>());
            Assert.IsNotNull(ads);
            Assert.IsFalse(ads is NoOpAdService);
#if LASTTRAIN_ADMOB
            Assert.IsInstanceOf<MockAdService>(ads);
#else
            Assert.IsInstanceOf<MockAdService>(ads);
            Assert.IsTrue(ads.IsRewardedReady(RewardedAdPlacement.Revive));
#endif
        }

        [Test]
        public void AnalyticsEventNames_CoreLoop_AreStableSnakeCase()
        {
            Assert.AreEqual("app_started", AnalyticsEventNames.AppStarted);
            Assert.AreEqual("tutorial_started", AnalyticsEventNames.TutorialStarted);
            Assert.AreEqual("tutorial_skipped", AnalyticsEventNames.TutorialSkipped);
            Assert.AreEqual("tutorial_completed", AnalyticsEventNames.TutorialCompleted);
            Assert.AreEqual("tutorial_post_skip_guide_shown", AnalyticsEventNames.TutorialPostSkipGuideShown);
            Assert.AreEqual("run_started", AnalyticsEventNames.RunStarted);
            Assert.AreEqual("run_completed", AnalyticsEventNames.RunCompleted);
            Assert.AreEqual("run_failed", AnalyticsEventNames.RunFailed);
            Assert.AreEqual("rewarded_ad_offered", AnalyticsEventNames.RewardedAdOffered);
            Assert.AreEqual("rewarded_ad_completed", AnalyticsEventNames.RewardedAdCompleted);
            Assert.AreEqual("meta_reward_received", AnalyticsEventNames.MetaRewardReceived);
        }

        [Test]
        public void AnalyticsServiceFactory_WithoutFirebaseDefine_NeverThrows()
        {
            var privacy = new PrivacyConsentService();
            privacy.Initialize(autoGrantInEditor: true);
            IAnalyticsService analytics = AnalyticsServiceFactory.Create(privacy);
            Assert.IsNotNull(analytics);
            Assert.DoesNotThrow(() => analytics.Track(AnalyticsEventNames.RunStarted, null));
        }

        [Test]
        public void FirebaseAnalyticsService_TryCreate_WithoutDefine_ReturnsNull()
        {
#if LASTTRAIN_FIREBASE
            Assert.Ignore("LASTTRAIN_FIREBASE defined — SDK path is active.");
#else
            Assert.IsNull(FirebaseAnalyticsService.TryCreate());
            var privacy = new PrivacyConsentService();
            privacy.Initialize(autoGrantInEditor: true);
            Assert.IsNull(FirebaseCrashReporter.TryCreate(privacy));
#endif
        }

        [Test]
        public void AdUnitConfig_UsesTestIdsInDevelopment()
        {
            var config = ScriptableObject.CreateInstance<AdUnitConfig>();
            string testId = config.GetRewardedUnitId(useTestIds: true);
            Assert.IsFalse(string.IsNullOrWhiteSpace(testId));
            Assert.IsTrue(testId.Contains("3940256099942544"));
        }

        [Test]
        public void AdUnitConfig_ReleaseAsset_RequestsGoogleTestIdsUntilStoreLaunch()
        {
            var config = UnityEditor.AssetDatabase.LoadAssetAtPath<AdUnitConfig>(
                "Assets/Data/Integration/AdUnitConfig.asset");
            Assume.That(config, Is.Not.Null);
            Assert.IsTrue(config.UseGoogleTestAdUnits);
            Assert.IsTrue(AdServiceFactory.UseTestAdUnitIds);

            string rewarded = config.GetRewardedUnitId(useTestIds: false);
            string interstitial = config.GetInterstitialUnitId(useTestIds: false);

            Assert.AreEqual("ca-app-pub-3940256099942544/5224354917", rewarded);
            Assert.AreEqual("ca-app-pub-3940256099942544/1033173712", interstitial);

            var so = new UnityEditor.SerializedObject(config);
            Assert.IsTrue(string.IsNullOrWhiteSpace(so.FindProperty("androidRewardedProductionId").stringValue));
            Assert.IsTrue(string.IsNullOrWhiteSpace(so.FindProperty("androidInterstitialProductionId").stringValue));

            var gmaSettings = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.ScriptableObject>(
                "Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset");
            Assume.That(gmaSettings, Is.Not.Null);
            var gmaSo = new UnityEditor.SerializedObject(gmaSettings);
            Assert.AreEqual(
                "ca-app-pub-3940256099942544~3347511713",
                gmaSo.FindProperty("adMobAndroidAppId").stringValue);
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
