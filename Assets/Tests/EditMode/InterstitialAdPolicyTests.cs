using LastTrain.Data;
using LastTrain.Ads;
using LastTrain.Battle;
using LastTrain.Core;
using LastTrain.Integrations;
using LastTrain.Release;
using LastTrain.Run;
using NUnit.Framework;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public sealed class InterstitialAdPolicyTests
    {
        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            PlayerPrefs.DeleteKey("lasttrain.settings.battleSpeed");
            PlayerPrefs.Save();
        }

        [Test]
        public void BattleSpeedRuntime_AfterAd_RestoresPresetCapturedBeforeOverlay()
        {
            PlayerPrefs.SetInt("lasttrain.settings.battleSpeed", 3);
            PlayerPrefs.Save();
            BattleSpeedRuntime.BeginAdOverlay();

            PlayerPrefs.SetInt("lasttrain.settings.battleSpeed", 1);
            PlayerPrefs.Save();
            Time.timeScale = 0f;
            BattleSpeedRuntime.RestoreTimeScaleAfterAd();

            Assert.AreEqual(3f, Time.timeScale, 0.001f);
        }

        [Test]
        public void BattleSpeedRuntime_SkipsRestoreWhilePaused()
        {
            Time.timeScale = 0f;
            BattleSpeedRuntime.RestoreTimeScaleFromSettings();
            Assert.AreEqual(0f, Time.timeScale, 0.001f);
        }

        [Test]
        public void TryShowAfterRunEnded_NoOpsWhenRunEndNotPending()
        {
            var ads = new AdCoordinator(new NoOpAdService(), new AdLimitService(), new AdRewardService(new AdLimitService()));
            var privacy = new PrivacyConsentService();
            privacy.Initialize(autoGrantInEditor: true);

            var coordinator = new InterstitialAdCoordinator(
                ads,
                privacy,
                sceneLoader: null,
                sessionProvider: () => null);

            Assert.DoesNotThrow(() => coordinator.TryShowAfterRunEnded());
        }

        [Test]
        public void EndlessRun_BelowStandardStationCount_DoesNotAttemptInterstitial()
        {
            var ads = new AdCoordinator(new MockAdService { AutoResult = AdResult.Completed }, new AdLimitService(), new AdRewardService(new AdLimitService()));
            var privacy = new PrivacyConsentService();
            privacy.Initialize(autoGrantInEditor: true);

            var coordinator = new InterstitialAdCoordinator(
                ads,
                privacy,
                sceneLoader: null,
                sessionProvider: () => null);

            var result = new RunResult(
                runId: "endless-short",
                lineId: RouteIds.Endless,
                isVictory: false,
                endReason: RunEndReason.Defeat,
                reachedStationIndex: 3,
                completedStationCount: 2,
                enemiesKilled: 0,
                bossesKilled: 0,
                mergeCount: 0,
                highestPassengerStar: 1,
                remainingTrainHp: 0,
                trainMaxHp: 100,
                finalCoins: 0,
                totalCoinsEarned: 0,
                totalCoinsSpent: 0,
                passengersSummoned: 0,
                passengersSold: 0,
                abilityCardsSelected: 0,
                isEndlessRun: true);

            coordinator.NotifyRunCompleted(result);
            coordinator.TryShowAfterRunEnded();

            Assert.Pass("Endless short run does not require interstitial attempt on non-Result scene.");
        }
    }
}
