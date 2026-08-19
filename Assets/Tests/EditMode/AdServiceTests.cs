using System;
using System.IO;
using LastTrain.Ads;
using LastTrain.Core;
using LastTrain.Run;
using LastTrain.Save;
using NUnit.Framework;

namespace LastTrain.Tests.EditMode
{
    public class AdServiceTests
    {
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "LastTrainAdTests_" + Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDir);
            string runPath = Path.Combine(_tempDir, "run.json");
            string metaPath = Path.Combine(_tempDir, "meta.json");
            RunSaveSystem.SetServiceForTests(new JsonSaveService(runPath, metaPath));
        }

        [TearDown]
        public void TearDown()
        {
            RunSaveSystem.SetServiceForTests(null);
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }

        [Test]
        public void ShowRewarded_Completed_GrantsAndConsumesLimit()
        {
            var mock = new MockAdService { AutoResult = AdResult.Completed };
            var limits = new AdLimitService();
            limits.BeginRun();
            var coordinator = new AdCoordinator(mock, limits, new AdRewardService(limits));

            int grants = 0;
            AdResult? last = null;
            coordinator.ShowRewarded(
                RewardedAdPlacement.PassengerReroll,
                () => grants++,
                r => last = r);

            Assert.AreEqual(AdResult.Completed, last);
            Assert.AreEqual(1, grants);
            Assert.AreEqual(
                AdLimitService.PassengerRerollPerRun - 1,
                limits.GetRemaining(RewardedAdPlacement.PassengerReroll));
        }

        [Test]
        public void ShowRewarded_Cancelled_DoesNotGrant()
        {
            var mock = new MockAdService { AutoResult = AdResult.Cancelled };
            var limits = new AdLimitService();
            limits.BeginRun();
            var coordinator = new AdCoordinator(mock, limits, new AdRewardService(limits));

            int grants = 0;
            AdResult? last = null;
            coordinator.ShowRewarded(
                RewardedAdPlacement.AbilityReroll,
                () => grants++,
                r => last = r);

            Assert.AreEqual(AdResult.Cancelled, last);
            Assert.AreEqual(0, grants);
            Assert.AreEqual(
                AdLimitService.AbilityRerollPerRun,
                limits.GetRemaining(RewardedAdPlacement.AbilityReroll));
        }

        [Test]
        public void ShowRewarded_Failed_DoesNotGrant()
        {
            var mock = new MockAdService { AutoResult = AdResult.Failed };
            var limits = new AdLimitService();
            limits.BeginRun();
            var coordinator = new AdCoordinator(mock, limits, new AdRewardService(limits));

            int grants = 0;
            coordinator.ShowRewarded(RewardedAdPlacement.FreeSummon, () => grants++, null);
            Assert.AreEqual(0, grants);
        }

        [Test]
        public void ShowRewarded_NotReady_WhenForceNotReady()
        {
            var mock = new MockAdService { ForceNotReady = true, AutoResult = AdResult.Completed };
            var limits = new AdLimitService();
            limits.BeginRun();
            var coordinator = new AdCoordinator(mock, limits, new AdRewardService(limits));

            AdResult? last = null;
            int grants = 0;
            coordinator.ShowRewarded(RewardedAdPlacement.ShopRefresh, () => grants++, r => last = r);

            Assert.AreEqual(AdResult.NotReady, last);
            Assert.AreEqual(0, grants);
        }

        [Test]
        public void AdRewardService_DuplicateRequestId_DoesNotDoubleGrant()
        {
            var limits = new AdLimitService { Cooldown = TimeSpan.Zero };
            limits.BeginRun();
            var rewards = new AdRewardService(limits);
            var request = new AdRequest(RewardedAdPlacement.PassengerReroll, "fixed-id");

            int grants = 0;
            Assert.IsTrue(rewards.TryGrant(request, AdResult.Completed, () => grants++));
            Assert.IsFalse(rewards.TryGrant(request, AdResult.Completed, () => grants++));
            Assert.AreEqual(1, grants);
        }

        [Test]
        public void Revive_RestoresHpAndClearsPendingDefeat()
        {
            var session = new GameSession();
            session.StartNewRun();

            bool offered = false;
            session.ReviveOffered += () => offered = true;
            session.RunState.Train.ApplyDamage(session.RunState.Train.CurrentHp);

            Assert.IsTrue(offered);
            Assert.IsTrue(session.IsPendingDefeat);
            Assert.IsTrue(session.HasActiveRun);

            var mock = new MockAdService { AutoResult = AdResult.Completed };
            var limits = new AdLimitService();
            limits.BeginRun();
            var coordinator = new AdCoordinator(mock, limits, new AdRewardService(limits));

            AdResult? result = null;
            coordinator.ShowRevive(session, r => result = r);

            Assert.AreEqual(AdResult.Completed, result);
            Assert.IsFalse(session.IsPendingDefeat);
            Assert.Greater(session.RunState.Train.CurrentHp, 0);
            Assert.IsFalse(session.ReviveAvailableThisRun);
        }

        [Test]
        public void DoubleResultReward_AddsBonusTickets()
        {
            MetaSaveSystem.LoadOrCreate();

            var runResult = new RunResult(
                "ad-double-run",
                "line1",
                false,
                RunEndReason.Defeat,
                1,
                1,
                0,
                0,
                0,
                1,
                10,
                100,
                0,
                0,
                0,
                0,
                0,
                0);
            MetaSaveSystem.ApplyRunResult(runResult);
            int afterFirst = MetaSaveSystem.LoadOrCreate().ticketFragments;
            int bonus = MetaSaveSystem.LastApplyResult?.Breakdown?.TotalTickets ?? 0;
            Assume.That(bonus, Is.GreaterThan(0));

            var mock = new MockAdService { AutoResult = AdResult.Completed };
            var limits = new AdLimitService();
            limits.BeginRun();
            var coordinator = new AdCoordinator(mock, limits, new AdRewardService(limits));

            coordinator.ShowDoubleResultReward(null);
            int afterDouble = MetaSaveSystem.LoadOrCreate().ticketFragments;
            Assert.AreEqual(afterFirst + bonus, afterDouble);
        }

        [Test]
        public void StationRewardDouble_GrantsExtraCoins()
        {
            var session = new GameSession();
            session.StartNewRun();
            int before = session.RunState.Currency.CurrentCoins;

            var mock = new MockAdService { AutoResult = AdResult.Completed };
            var limits = new AdLimitService();
            limits.BeginRun();
            limits.NotifyStationChanged(1);
            var coordinator = new AdCoordinator(mock, limits, new AdRewardService(limits));

            coordinator.ShowStationRewardDouble(session.RunState, 20, null);
            Assert.AreEqual(before + 20, session.RunState.Currency.CurrentCoins);
            Assert.AreEqual(0, limits.GetRemaining(RewardedAdPlacement.StationRewardDouble));
        }

        [Test]
        public void Cooldown_BlocksImmediateSecondNonRerollAd()
        {
            DateTime now = new DateTime(2026, 7, 22, 0, 0, 0, DateTimeKind.Utc);
            var limits = new AdLimitService
            {
                Cooldown = TimeSpan.FromSeconds(2),
                UtcNowProvider = () => now,
            };
            limits.BeginRun();
            var rewards = new AdRewardService(limits);

            Assert.IsTrue(rewards.TryGrant(
                new AdRequest(RewardedAdPlacement.Revive, "a"),
                AdResult.Completed,
                () => { }));

            Assert.IsTrue(limits.IsOnCooldown);
            Assert.IsFalse(limits.CanUse(RewardedAdPlacement.DoubleResultReward));

            now = now.AddSeconds(2.1);
            Assert.IsFalse(limits.IsOnCooldown);
            Assert.IsTrue(limits.CanUse(RewardedAdPlacement.DoubleResultReward));
        }

        [Test]
        public void RerollPlacement_AllowsImmediateSecondUse()
        {
            DateTime now = new DateTime(2026, 7, 22, 0, 0, 0, DateTimeKind.Utc);
            var limits = new AdLimitService
            {
                Cooldown = TimeSpan.FromSeconds(2),
                UtcNowProvider = () => now,
            };
            limits.BeginRun();
            var rewards = new AdRewardService(limits);

            Assert.IsTrue(rewards.TryGrant(
                new AdRequest(RewardedAdPlacement.PassengerReroll, "a"),
                AdResult.Completed,
                () => { }));

            Assert.IsFalse(limits.IsOnCooldown);
            Assert.IsTrue(limits.CanUse(RewardedAdPlacement.PassengerReroll));
            Assert.AreEqual(1, limits.GetRemaining(RewardedAdPlacement.PassengerReroll));
        }

        [Test]
        public void FreeSummon_DailyLimit_Three()
        {
            DateTime now = new DateTime(2026, 8, 13, 12, 0, 0, DateTimeKind.Utc);
            var limits = new AdLimitService
            {
                Cooldown = TimeSpan.Zero,
                UtcNowProvider = () => now,
            };
            limits.BeginRun();
            var mock = new MockAdService { AutoResult = AdResult.Completed };
            var coordinator = new AdCoordinator(mock, limits, new AdRewardService(limits));

            int grants = 0;
            for (int i = 0; i < AdLimitService.FreeSummonPerDay; i++)
            {
                coordinator.ShowRewarded(RewardedAdPlacement.FreeSummon, () => grants++);
            }

            Assert.AreEqual(AdLimitService.FreeSummonPerDay, grants);
            Assert.AreEqual(0, limits.GetRemaining(RewardedAdPlacement.FreeSummon));

            AdResult? last = null;
            coordinator.ShowRewarded(RewardedAdPlacement.FreeSummon, () => grants++, r => last = r);
            Assert.AreEqual(AdResult.NotReady, last);
            Assert.AreEqual(AdLimitService.FreeSummonPerDay, grants);
        }

        [Test]
        public void ShopRefresh_PerRunLimit_Three()
        {
            var limits = new AdLimitService { Cooldown = TimeSpan.Zero };
            limits.BeginRun();
            var mock = new MockAdService { AutoResult = AdResult.Completed };
            var coordinator = new AdCoordinator(mock, limits, new AdRewardService(limits));

            int grants = 0;
            for (int i = 0; i < AdLimitService.ShopRefreshPerRun; i++)
            {
                coordinator.ShowRewarded(RewardedAdPlacement.ShopRefresh, () => grants++);
            }

            Assert.AreEqual(AdLimitService.ShopRefreshPerRun, grants);
            Assert.IsFalse(limits.CanUse(RewardedAdPlacement.ShopRefresh));
        }

        [Test]
        public void NoOpAdService_NeverReady()
        {
            var noop = new NoOpAdService();
            foreach (RewardedAdPlacement placement in Enum.GetValues(typeof(RewardedAdPlacement)))
            {
                Assert.IsFalse(noop.IsRewardedReady(placement), placement.ToString());
                AdResult? rewarded = null;
                noop.ShowRewardedAd(new AdRequest(placement), r => rewarded = r);
                Assert.AreEqual(AdResult.NotReady, rewarded, placement.ToString());
            }

            AdResult? interstitial = null;
            noop.ShowInterstitial(r => interstitial = r);
            Assert.AreEqual(AdResult.NotReady, interstitial);
        }
    }
}
