using System;
using System.Collections.Generic;
using LastTrain.Core;
using LastTrain.Data;
using LastTrain.Difficulty;
using LastTrain.Mission;
using LastTrain.Run;
using LastTrain.Save;
using NUnit.Framework;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class MissionProgressServiceTests
    {
        [TearDown]
        public void TearDown()
        {
            MissionClock.UtcNowProvider = null;
        }

        [Test]
        public void ApplyEvent_IncrementsProgress_AndCompletes()
        {
            MissionClock.UtcNowProvider = () => new DateTime(2026, 7, 22, 12, 0, 0, DateTimeKind.Utc);
            var meta = NewMeta();
            MissionData mission = CreateMission(
                "m_merge",
                MissionPeriod.Daily,
                new MissionCondition(MissionConditionType.MergeCount, 3));

            MissionProgressService.ApplyEvent(meta, new[] { mission }, MissionEventType.Merge, 2);
            Assert.AreEqual(2, meta.missionProgresses[0].progress);
            Assert.IsFalse(meta.missionProgresses[0].completed);

            MissionProgressService.ApplyEvent(meta, new[] { mission }, MissionEventType.Merge, 2);
            Assert.AreEqual(3, meta.missionProgresses[0].progress);
            Assert.IsTrue(meta.missionProgresses[0].completed);
        }

        [Test]
        public void TryClaimReward_GrantsOnce_AndBlocksDoubleClaim()
        {
            MissionClock.UtcNowProvider = () => new DateTime(2026, 7, 22, 12, 0, 0, DateTimeKind.Utc);
            var meta = NewMeta();
            meta.ticketFragments = 0;
            meta.accountXp = 0;
            meta.accountLevel = 1;

            MissionData mission = CreateMission(
                "m_claim",
                MissionPeriod.Daily,
                new MissionCondition(MissionConditionType.ShopPurchaseCount, 1),
                tickets: 12,
                xp: 30);

            MissionProgressService.ApplyEvent(meta, new[] { mission }, MissionEventType.ShopPurchased, 1);
            Assert.IsTrue(MissionProgressService.TryClaimReward(meta, mission, out int tickets, out int xp));
            Assert.AreEqual(12, tickets);
            Assert.AreEqual(30, xp);
            Assert.AreEqual(12, meta.ticketFragments);
            Assert.AreEqual(30, meta.accountXp);
            Assert.IsTrue(meta.missionProgresses[0].claimed);

            Assert.IsFalse(MissionProgressService.TryClaimReward(meta, mission, out _, out _));
            Assert.AreEqual(12, meta.ticketFragments);
        }

        [Test]
        public void EnsurePeriods_ResetsDaily_WhenDateChanges()
        {
            DateTime day1 = new(2026, 7, 22, 10, 0, 0, DateTimeKind.Utc);
            MissionClock.UtcNowProvider = () => day1;
            var meta = NewMeta();
            MissionData mission = CreateMission(
                "m_daily",
                MissionPeriod.Daily,
                new MissionCondition(MissionConditionType.MergeCount, 5));

            MissionProgressService.ApplyEvent(meta, new[] { mission }, MissionEventType.Merge, 4);
            Assert.AreEqual(4, meta.missionProgresses[0].progress);
            Assert.AreEqual("2026-07-22", meta.missionDailyKey);

            DateTime day2 = new(2026, 7, 23, 10, 0, 0, DateTimeKind.Utc);
            MissionClock.UtcNowProvider = () => day2;
            MissionProgressService.EnsurePeriods(meta, new[] { mission });
            Assert.AreEqual("2026-07-23", meta.missionDailyKey);
            Assert.AreEqual(0, meta.missionProgresses[0].progress);
            Assert.IsFalse(meta.missionProgresses[0].completed);
            Assert.IsFalse(meta.missionProgresses[0].claimed);
        }

        [Test]
        public void EnsurePeriods_ResetsWeekly_WhenWeekChanges()
        {
            // 2026-07-20 = Monday of ISO week 30
            DateTime week30 = new(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc);
            MissionClock.UtcNowProvider = () => week30;
            var meta = NewMeta();
            MissionData mission = CreateMission(
                "m_weekly",
                MissionPeriod.Weekly,
                new MissionCondition(MissionConditionType.EliteKillCount, 10));

            MissionProgressService.ApplyEvent(
                meta,
                new[] { mission },
                MissionEventType.EnemyKilled,
                5,
                param: (int)EnemyType.Elite);
            Assert.AreEqual(5, meta.missionProgresses[0].progress);
            Assert.AreEqual("2026-W30", meta.missionWeeklyKey);

            // 2026-07-27 = Monday of ISO week 31
            DateTime week31 = new(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc);
            MissionClock.UtcNowProvider = () => week31;
            MissionProgressService.EnsurePeriods(meta, new[] { mission });
            Assert.AreEqual("2026-W31", meta.missionWeeklyKey);
            Assert.AreEqual(0, meta.missionProgresses[0].progress);
        }

        [Test]
        public void ClockRegression_DoesNotAdvancePeriodKeys()
        {
            DateTime trusted = new(2026, 7, 23, 12, 0, 0, DateTimeKind.Utc);
            MissionClock.UtcNowProvider = () => trusted;
            var meta = NewMeta();
            MissionData mission = CreateMission(
                "m_reg",
                MissionPeriod.Daily,
                new MissionCondition(MissionConditionType.MergeCount, 2));
            MissionProgressService.EnsurePeriods(meta, new[] { mission });
            string dailyKey = meta.missionDailyKey;
            string trustedIso = meta.missionLastTrustedUtc;

            MissionClock.UtcNowProvider = () => trusted.AddHours(-2);
            MissionProgressService.EnsurePeriods(meta, new[] { mission });
            Assert.AreEqual(dailyKey, meta.missionDailyKey);
            Assert.AreEqual(trustedIso, meta.missionLastTrustedUtc);
        }

        [Test]
        public void DailySeed_IsStableForSameDateAndVersion()
        {
            DateTime day = new(2026, 7, 22, 8, 0, 0, DateTimeKind.Utc);
            int a = DailyRunService.ComputeSeed(day, "1.2.3");
            int b = DailyRunService.ComputeSeed(day.AddHours(15), "1.2.3");
            int c = DailyRunService.ComputeSeed(day.AddDays(1), "1.2.3");
            int d = DailyRunService.ComputeSeed(day, "1.2.4");

            Assert.AreEqual(a, b);
            Assert.AreNotEqual(a, c);
            Assert.AreNotEqual(a, d);
            Assert.AreNotEqual(0, a);
        }

        [Test]
        public void CreateDailyRun_SetsSeedAndFlag()
        {
            var config = RunStartConfig.CreateDailyRun(42);
            Assert.IsTrue(config.IsDailyRun);
            Assert.AreEqual(42, config.RandomSeed);
        }

        [Test]
        public void TrySavePreparing_RejectsDailyRun()
        {
            var session = new GameSession();
            RunStartConfig config = RunStartConfig.CreateDailyRun(99);
            session.StartNewRun(config);
            session.RunState.Battle.StartRun();

            Assert.IsTrue(session.RunState.IsDailyRun);
            Assert.IsFalse(RunSaveSystem.TrySavePreparing(session));
        }

        [Test]
        public void ApplyRunResult_CountsClearAndFinalBoss()
        {
            MissionClock.UtcNowProvider = () => new DateTime(2026, 7, 22, 12, 0, 0, DateTimeKind.Utc);
            var meta = NewMeta();
            MissionData clear = CreateMission(
                "m_clear",
                MissionPeriod.Weekly,
                new MissionCondition(MissionConditionType.ClearRouteCount, 2));
            MissionData boss = CreateMission(
                "m_boss",
                MissionPeriod.Weekly,
                new MissionCondition(MissionConditionType.DefeatFinalBoss, 2));

            var result = new RunResult(
                "run-daily",
                "line1",
                isVictory: true,
                RunEndReason.Victory,
                reachedStationIndex: 10,
                completedStationCount: 10,
                enemiesKilled: 50,
                bossesKilled: 1,
                mergeCount: 0,
                highestPassengerStar: 1,
                remainingTrainHp: 40,
                trainMaxHp: 100,
                finalCoins: 0,
                totalCoinsEarned: 0,
                totalCoinsSpent: 0,
                passengersSummoned: 0,
                passengersSold: 0,
                abilityCardsSelected: 0,
                difficultyId: DifficultyIds.Normal);

            MissionProgressService.ApplyRunResult(meta, new List<MissionData> { clear, boss }, result);
            Assert.AreEqual(1, FindProgress(meta, "m_clear"));
            Assert.AreEqual(1, FindProgress(meta, "m_boss"));
        }

        private static MetaSaveData NewMeta()
        {
            var meta = new MetaSaveData();
            meta.EnsureDefaults();
            return meta;
        }

        private static int FindProgress(MetaSaveData meta, string id)
        {
            for (int i = 0; i < meta.missionProgresses.Length; i++)
            {
                if (meta.missionProgresses[i].missionId == id)
                {
                    return meta.missionProgresses[i].progress;
                }
            }

            return -1;
        }

        private static MissionData CreateMission(
            string id,
            MissionPeriod period,
            MissionCondition condition,
            int tickets = 10,
            int xp = 10)
        {
            var data = ScriptableObject.CreateInstance<MissionData>();
            data.EditorSet(id, id, id, period, condition, tickets, xp);
            return data;
        }
    }
}
