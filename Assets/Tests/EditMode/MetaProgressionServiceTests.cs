using System.IO;
using LastTrain.Data;
using LastTrain.Run;
using LastTrain.Save;
using NUnit.Framework;

namespace LastTrain.Tests.EditMode
{
    public class MetaProgressionServiceTests
    {
        private string _tempDir;
        private string _runPath;
        private string _metaPath;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "LastTrainMetaTests_" + Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDir);
            _runPath = Path.Combine(_tempDir, "RunSaveData.json");
            _metaPath = Path.Combine(_tempDir, "MetaSaveData.json");
            RunSaveSystem.SetServiceForTests(new JsonSaveService(_runPath, _metaPath));
        }

        [TearDown]
        public void TearDown()
        {
            RunSaveSystem.SetServiceForTests(null);
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }

        [Test]
        public void CalculateRewards_SumsStationKillBossHpAndDiscovery()
        {
            var meta = new MetaSaveData();
            meta.EnsureDefaults();

            var result = new RunResult(
                runId: "run-1",
                lineId: "line_default",
                isVictory: true,
                endReason: RunEndReason.Victory,
                reachedStationIndex: 2,
                completedStationCount: 3,
                enemiesKilled: 10,
                bossesKilled: 1,
                mergeCount: 0,
                highestPassengerStar: 2,
                remainingTrainHp: 40,
                trainMaxHp: 100,
                finalCoins: 10,
                totalCoinsEarned: 50,
                totalCoinsSpent: 0,
                passengersSummoned: 2,
                passengersSold: 0,
                abilityCardsSelected: 0,
                discoveredPassengerIds: new[] { "passenger_office_worker" },
                discoveredEnemyIds: new[] { "enemy_normal" },
                discoveredBossIds: new[] { "enemy_boss_drunk_manager" });

            MetaRewardBreakdown breakdown = MetaProgressionService.CalculateRewards(result, meta);

            Assert.AreEqual(
                3 * MetaProgressionDefaults.TicketPerCompletedStation
                + 2 * MetaProgressionDefaults.TicketPerReachedStationIndex,
                breakdown.StationTickets);
            Assert.AreEqual(10 * MetaProgressionDefaults.TicketPerEnemyKill, breakdown.KillTickets);
            Assert.AreEqual(1 * MetaProgressionDefaults.TicketPerBossKill, breakdown.BossTickets);
            Assert.AreEqual(40 * MetaProgressionDefaults.TicketPerRemainingHp, breakdown.RemainingHpTickets);
            Assert.AreEqual(3 * MetaProgressionDefaults.TicketPerNewDiscovery, breakdown.DiscoveryTickets);
            Assert.Greater(breakdown.TotalTickets, 0);
        }

        [Test]
        public void TryApplyRunResult_SameRunId_DoesNotDoubleGrant()
        {
            var meta = new MetaSaveData();
            meta.EnsureDefaults();

            RunResult result = CreateMinimalResult("run-dup", enemiesKilled: 5, bossesKilled: 0);

            MetaApplyResult first = MetaProgressionService.TryApplyRunResult(meta, result);
            int ticketsAfterFirst = meta.ticketFragments;

            MetaApplyResult second = MetaProgressionService.TryApplyRunResult(meta, result);

            Assert.IsTrue(first.Applied);
            Assert.IsFalse(first.WasDuplicate);
            Assert.IsFalse(second.Applied);
            Assert.IsTrue(second.WasDuplicate);
            Assert.AreEqual(ticketsAfterFirst, meta.ticketFragments);
            Assert.AreEqual(1, meta.rewardedRunIds.Length);
        }

        [Test]
        public void EnsureDefaults_SeedsDefaultUnlockedPassengers()
        {
            var meta = new MetaSaveData();
            meta.EnsureDefaults();

            Assert.AreEqual(
                MetaProgressionDefaults.DefaultUnlockedPassengerIds.Length,
                meta.unlockedPassengerIds.Length);
            Assert.IsTrue(
                MetaProgressionService.IsPassengerUnlocked(meta, "passenger_office_worker"));
            Assert.IsFalse(
                MetaProgressionService.IsPassengerUnlocked(meta, "passenger_developer"));
        }

        [Test]
        public void TryApplyRunResult_AccountLevelUnlocksDeveloper()
        {
            var meta = new MetaSaveData();
            meta.EnsureDefaults();

            // 충분히 큰 보상으로 레벨 2 이상 도달
            RunResult result = CreateMinimalResult(
                "run-level",
                enemiesKilled: 0,
                bossesKilled: 2,
                remainingHp: 0,
                completedStations: 10,
                reachedStation: 10);

            MetaApplyResult apply = MetaProgressionService.TryApplyRunResult(meta, result);

            Assert.IsTrue(apply.Applied);
            Assert.GreaterOrEqual(meta.accountLevel, MetaProgressionDefaults.DeveloperUnlockAccountLevel);
            Assert.IsTrue(
                MetaProgressionService.IsPassengerUnlocked(
                    meta,
                    MetaProgressionDefaults.PassengerDeveloperId));
        }

        [Test]
        public void MetaSaveSystem_RoundTrip_PersistsTicketFragments()
        {
            MetaSaveData meta = MetaSaveSystem.LoadOrCreate();
            meta.ticketFragments = 42;
            Assert.IsTrue(MetaSaveSystem.Save(meta));

            // Force reload via new service instance path still same files
            MetaSaveData loaded = MetaSaveSystem.LoadOrCreate();
            Assert.AreEqual(42, loaded.ticketFragments);
        }

        [Test]
        public void FilterUnlockedPassengers_ExcludesLocked()
        {
            PassengerData unlocked = CreatePassenger("passenger_office_worker", startsUnlocked: true);
            PassengerData locked = CreatePassenger("passenger_developer", startsUnlocked: false);

            MetaSaveData meta = MetaSaveSystem.LoadOrCreate();
            meta.unlockedPassengerIds = new[] { "passenger_office_worker" };
            MetaSaveSystem.Save(meta);

            var filtered = MetaSaveSystem.FilterUnlockedPassengers(
                new[] { unlocked, locked });

            Assert.AreEqual(1, filtered.Count);
            Assert.AreEqual("passenger_office_worker", filtered[0].Id);
        }

        private static RunResult CreateMinimalResult(
            string runId,
            int enemiesKilled,
            int bossesKilled,
            int remainingHp = 10,
            int completedStations = 1,
            int reachedStation = 1)
        {
            return new RunResult(
                runId,
                "line_default",
                isVictory: false,
                endReason: RunEndReason.Defeat,
                reachedStationIndex: reachedStation,
                completedStationCount: completedStations,
                enemiesKilled: enemiesKilled,
                bossesKilled: bossesKilled,
                mergeCount: 0,
                highestPassengerStar: 1,
                remainingTrainHp: remainingHp,
                trainMaxHp: 100,
                finalCoins: 0,
                totalCoinsEarned: 0,
                totalCoinsSpent: 0,
                passengersSummoned: 0,
                passengersSold: 0,
                abilityCardsSelected: 0);
        }

        private static PassengerData CreatePassenger(string id, bool startsUnlocked)
        {
            PassengerData data = UnityEngine.ScriptableObject.CreateInstance<PassengerData>();
            var so = new UnityEditor.SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = id;
            so.FindProperty("baseAttack").floatValue = 10f;
            so.FindProperty("attackInterval").floatValue = 1f;
            so.FindProperty("range").floatValue = 5f;
            so.FindProperty("startsUnlocked").boolValue = startsUnlocked;
            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }
    }
}
