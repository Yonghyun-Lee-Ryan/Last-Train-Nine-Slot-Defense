using LastTrain.Data;
using LastTrain.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class HeadlessCombatSimulatorTests
    {
        private GameDatabase _database;

        [SetUp]
        public void SetUp()
        {
            _database = GameDatabaseLocator.Load();
            Assume.That(_database, Is.Not.Null, "GameDatabase asset required");
        }

        [Test]
        public void RunOnce_SameSeed_IsDeterministic()
        {
            var config = CreateMinimalConfig(iterations: 1);
            var sim = new HeadlessCombatSimulator();

            BattleSimulationRunResult a = sim.RunOnce(config, _database, seed: 777);
            BattleSimulationRunResult b = sim.RunOnce(config, _database, seed: 777);

            Assert.AreEqual(a.IsVictory, b.IsVictory);
            Assert.AreEqual(a.RemainingTrainHp, b.RemainingTrainHp);
            Assert.AreEqual(a.EnemiesKilled, b.EnemiesKilled);
            Assert.AreEqual(a.SimulatedSeconds, b.SimulatedSeconds, 0.001f);
        }

        [Test]
        public void RunBatch_ComputesWinRateAndAverages()
        {
            var config = CreateMinimalConfig(iterations: 5);
            var sim = new HeadlessCombatSimulator();
            BattleSimulationAggregate aggregate = sim.RunBatch(config, _database);

            Assert.AreEqual(5, aggregate.Iterations);
            Assert.AreEqual(5, aggregate.Runs.Count);
            Assert.GreaterOrEqual(aggregate.WinRate, 0f);
            Assert.LessOrEqual(aggregate.WinRate, 1f);
            Assert.GreaterOrEqual(aggregate.AvgSimulatedSeconds, 0f);
        }

        [Test]
        public void SimulationCsvWriter_WritesFile()
        {
            var config = CreateMinimalConfig(iterations: 2);
            var sim = new HeadlessCombatSimulator();
            BattleSimulationAggregate aggregate = sim.RunBatch(config, _database);

            string dir = System.IO.Path.Combine(Application.temporaryCachePath, "LastTrainSimTests");
            string path = SimulationCsvWriter.Write(aggregate, dir, "test_sim.csv");

            Assert.IsTrue(System.IO.File.Exists(path));
            string text = System.IO.File.ReadAllText(path);
            StringAssert.Contains("win_rate", text);
            StringAssert.Contains("run_index", text);
        }

        [Test]
        public void RunOnce_DoesNotPlayPastMaxStationIndex()
        {
            var config = CreateMinimalConfig(iterations: 1);
            config.maxStationIndex = 1;
            config.maxSimulatedSeconds = 90f;
            var sim = new HeadlessCombatSimulator();
            BattleSimulationRunResult run = sim.RunOnce(config, _database, seed: 19);

            Assert.Greater(run.SimulatedSeconds, 0.01f);
            Assert.LessOrEqual(run.ReachedStationIndex, 1);
        }

        private static BattleSimulationConfig CreateMinimalConfig(int iterations)
        {
            var slots = new BattleSimulationSlotConfig[9];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = new BattleSimulationSlotConfig();
            }

            slots[0] = new BattleSimulationSlotConfig
            {
                passengerId = "passenger_office_worker",
                starLevel = 1,
            };
            slots[1] = new BattleSimulationSlotConfig
            {
                passengerId = "passenger_delivery",
                starLevel = 1,
            };
            slots[2] = new BattleSimulationSlotConfig
            {
                passengerId = "passenger_trainer",
                starLevel = 1,
            };

            return new BattleSimulationConfig
            {
                baseSeed = 11,
                iterations = iterations,
                deltaTime = 0.2f,
                maxSimulatedSeconds = 60f,
                startingStationIndex = 1,
                maxStationIndex = 1,
                difficultyMultiplier = 0.5f,
                initialTrainHp = 200,
                initialCoins = 50,
                slots = slots,
                abilityIds = System.Array.Empty<string>(),
                autoContinueAbilityRewards = true,
            };
        }
    }
}
