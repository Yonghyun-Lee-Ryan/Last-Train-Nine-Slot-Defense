using LastTrain.Data;
using LastTrain.Integrations;
using LastTrain.Run;
using LastTrain.Save;
using NUnit.Framework;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class Unit50QuickRunTests
    {
        [Test]
        public void RouteQuick_HasFiveSequentialStations()
        {
            GameDatabase db = GameDatabaseLocator.Load();
            Assert.IsNotNull(db);
            Assert.IsTrue(db.TryGetRoute(RouteIds.Quick, out RouteData route));
            Assert.AreEqual(5, route.StationCount);
            Assert.AreEqual(5, db.GetRouteStationCount(RouteIds.Quick));
            for (int i = 1; i <= 5; i++)
            {
                Assert.IsTrue(db.TryGetStationByRouteIndex(RouteIds.Quick, i, out StationData station));
                Assert.AreEqual(i, station.StationIndex);
            }

            Assert.IsFalse(db.TryGetStationByRouteIndex(RouteIds.Quick, 6, out _));
        }

        [Test]
        public void CreateQuickRun_SetsLineId()
        {
            RunStartConfig config = RunStartConfig.CreateQuickRun();
            var run = new RunState();
            run.Initialize(config);
            Assert.AreEqual(RouteIds.Quick, run.LineId);
            Assert.IsFalse(run.IsDailyRun);
            Assert.IsFalse(run.IsEndlessRun);
            run.Dispose();
        }

        [Test]
        public void QuickRun_VictoryAfterStationFive()
        {
            GameDatabase db = GameDatabaseLocator.Load();
            Assert.IsTrue(db.TryGetStationByRouteIndex(RouteIds.Quick, 5, out StationData last));
            Assert.IsNotNull(last);
            Assert.IsFalse(db.TryGetStationByRouteIndex(RouteIds.Quick, last.StationIndex + 1, out _));
        }

        [Test]
        public void QuickRun_MetaRewardUsesRouteMultiplier()
        {
            GameDatabase db = GameDatabaseLocator.Load();
            Assert.IsTrue(db.TryGetRoute(RouteIds.Quick, out RouteData route));
            Assert.AreEqual(0.7f, route.RewardMultiplier, 0.001f);

            var result = new RunResult(
                runId: "quick-test-run",
                lineId: RouteIds.Quick,
                isVictory: true,
                endReason: RunEndReason.Victory,
                reachedStationIndex: 5,
                completedStationCount: 5,
                enemiesKilled: 0,
                bossesKilled: 0,
                mergeCount: 0,
                highestPassengerStar: 1,
                remainingTrainHp: 50,
                trainMaxHp: 100,
                finalCoins: 0,
                totalCoinsEarned: 0,
                totalCoinsSpent: 0,
                passengersSummoned: 0,
                passengersSold: 0,
                abilityCardsSelected: 0);

            var line1 = new RunResult(
                runId: "line1-test-run",
                lineId: RouteIds.Default,
                isVictory: true,
                endReason: RunEndReason.Victory,
                reachedStationIndex: 5,
                completedStationCount: 5,
                enemiesKilled: 0,
                bossesKilled: 0,
                mergeCount: 0,
                highestPassengerStar: 1,
                remainingTrainHp: 50,
                trainMaxHp: 100,
                finalCoins: 0,
                totalCoinsEarned: 0,
                totalCoinsSpent: 0,
                passengersSummoned: 0,
                passengersSold: 0,
                abilityCardsSelected: 0);

            MetaRewardBreakdown quick = MetaProgressionService.CalculateRewards(result, new MetaSaveData());
            MetaRewardBreakdown normal = MetaProgressionService.CalculateRewards(line1, new MetaSaveData());
            Assert.Less(quick.TotalTickets, normal.TotalTickets);
            Assert.Greater(quick.TotalTickets, 0);
        }

        [Test]
        public void RemoteConfig_HasQuickRunMultiplierDefault()
        {
            Assert.AreEqual(1f, RemoteConfigSnapshot.Default.QuickRunRewardMultiplier, 0.001f);
        }
    }
}
