using System.Collections.Generic;
using LastTrain.Battle;
using LastTrain.Data;
using LastTrain.Run;
using LastTrain.Wave;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class StationManagerTests
    {
        private RunState _runState;
        private EnemyData _enemyData;
        private RecordingBattleContext _battleContext;

        [SetUp]
        public void SetUp()
        {
            _runState = new RunState();
            _runState.Initialize(RunStartConfig.CreateDefault());
            _runState.Battle.StartRun();
            _enemyData = ScriptableObject.CreateInstance<EnemyData>();
            var enemySo = new SerializedObject(_enemyData);
            enemySo.FindProperty("id").stringValue = "station_test_enemy";
            enemySo.ApplyModifiedPropertiesWithoutUndo();
            _battleContext = new RecordingBattleContext();
        }

        [TearDown]
        public void TearDown()
        {
            _runState?.Dispose();
            Object.DestroyImmediate(_enemyData);
        }

        [Test]
        public void TryStartNextWave_RunsWavesInOrder()
        {
            StationData station = CreateStation(
                "station_test",
                1,
                CreateWave("wave_a", _enemyData, count: 1, interval: 0f),
                CreateWave("wave_b", _enemyData, count: 1, interval: 0f));

            var manager = new StationManager(_ => null);
            manager.Initialize(_runState, station);

            Assert.IsTrue(manager.TryStartNextWave());
            Assert.AreEqual(RunPhase.Fighting, _runState.Battle.CurrentPhase);
            Assert.AreEqual(0, manager.CurrentWaveIndex);

            manager.Tick(1f, _battleContext);
            Assert.AreEqual(1, _battleContext.SpawnedCount);

            _battleContext.AliveCount = 0;
            manager.Tick(0f, _battleContext);
            Assert.AreEqual(1, manager.CurrentWaveIndex);

            manager.Tick(0.1f, _battleContext);
            Assert.AreEqual(2, _battleContext.SpawnedCount);
        }

        [Test]
        public void CompleteStation_FiresEventAndGrantsReward()
        {
            StationData station = CreateStation(
                "station_reward",
                1,
                CreateWave("wave_only", _enemyData, count: 1, interval: 0f));
            var rewardSo = new SerializedObject(station);
            rewardSo.FindProperty("rewardCoins").intValue = 20;
            rewardSo.ApplyModifiedPropertiesWithoutUndo();

            StationData completed = null;
            var manager = new StationManager(index => index == 2
                ? CreateStation(
                    "station_2",
                    2,
                    CreateWave("wave_s2", _enemyData, count: 1, interval: 0f))
                : null);
            manager.StationCompleted += s => completed = s;
            manager.Initialize(_runState, station);
            manager.TryStartNextWave();

            manager.Tick(1f, _battleContext);
            _battleContext.AliveCount = 0;
            manager.Tick(0f, _battleContext);

            Assert.AreSame(station, completed);
            Assert.AreEqual(70, _runState.Currency.CurrentCoins);
            Assert.AreEqual(RunPhase.Preparing, _runState.Battle.CurrentPhase);
            Assert.AreEqual(2, _runState.Station.CurrentStationIndex);
        }

        [Test]
        public void CompleteStation_WithAbilityChoice_WaitsForReward()
        {
            StationData station = CreateStation(
                "station_ability",
                1,
                CreateWave("wave_ability", _enemyData, count: 1, interval: 0f));
            var rewardSo = new SerializedObject(station);
            rewardSo.FindProperty("grantsAbilityChoice").boolValue = true;
            rewardSo.ApplyModifiedPropertiesWithoutUndo();

            StationData rewardStation = null;
            var manager = new StationManager(index => index == 2
                ? CreateStation(
                    "station_2",
                    2,
                    CreateWave("wave_s2", _enemyData, count: 1, interval: 0f))
                : null);
            manager.AbilityRewardRequested += s => rewardStation = s;
            manager.Initialize(_runState, station);
            manager.TryStartNextWave();

            manager.Tick(1f, _battleContext);
            _battleContext.AliveCount = 0;
            manager.Tick(0f, _battleContext);

            Assert.AreSame(station, rewardStation);
            Assert.IsTrue(manager.IsWaitingForAbilityReward);
            Assert.AreEqual(RunPhase.RewardSelecting, _runState.Battle.CurrentPhase);
            Assert.AreEqual(1, _runState.Station.CurrentStationIndex);

            Assert.IsTrue(manager.ContinueAfterAbilityReward());
            Assert.AreEqual(RunPhase.Preparing, _runState.Battle.CurrentPhase);
            Assert.AreEqual(2, _runState.Station.CurrentStationIndex);
        }

        [Test]
        public void Cancel_StopsFurtherSpawns()
        {
            StationData station = CreateStation(
                "station_cancel",
                1,
                CreateWave("wave_cancel", _enemyData, count: 3, interval: 0.1f));

            var manager = new StationManager(_ => null);
            manager.Initialize(_runState, station);
            manager.TryStartNextWave();

            manager.Tick(0.05f, _battleContext);
            manager.Cancel();
            int spawnedBefore = _battleContext.SpawnedCount;

            manager.Tick(1f, _battleContext);
            Assert.AreEqual(spawnedBefore, _battleContext.SpawnedCount);
        }

        [Test]
        public void TryAdvanceToNextStation_WhenNoNextStation_RequestsVictoryAndCountsCompleted()
        {
            StationData station = CreateStation(
                "station_final",
                1,
                CreateWave("wave_final", _enemyData, count: 1, interval: 0f));

            bool victoryRequested = false;
            var manager = new StationManager(_ => null);
            manager.RunVictoryRequested += () => victoryRequested = true;
            manager.Initialize(_runState, station);

            Assert.IsTrue(manager.TryStartNextWave());

            manager.Tick(1f, _battleContext);
            _battleContext.AliveCount = 0;
            manager.Tick(0f, _battleContext);

            Assert.IsTrue(victoryRequested);
            Assert.AreEqual(1, _runState.Station.CompletedStationCount);
        }

        private static StationData CreateStation(string id, int stationIndex, params WaveData[] waves)
        {
            var station = ScriptableObject.CreateInstance<StationData>();
            var so = new SerializedObject(station);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = id;
            so.FindProperty("stationIndex").intValue = stationIndex;

            SerializedProperty wavesProp = so.FindProperty("waves");
            wavesProp.arraySize = waves.Length;
            for (int i = 0; i < waves.Length; i++)
            {
                wavesProp.GetArrayElementAtIndex(i).objectReferenceValue = waves[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return station;
        }

        private static WaveData CreateWave(string id, EnemyData enemy, int count, float interval)
        {
            var wave = ScriptableObject.CreateInstance<WaveData>();
            var so = new SerializedObject(wave);
            so.FindProperty("id").stringValue = id;

            SerializedProperty spawns = so.FindProperty("spawns");
            spawns.arraySize = 1;
            SerializedProperty element = spawns.GetArrayElementAtIndex(0);
            element.FindPropertyRelative("enemy").objectReferenceValue = enemy;
            element.FindPropertyRelative("count").intValue = count;
            element.FindPropertyRelative("spawnInterval").floatValue = interval;
            element.FindPropertyRelative("spawnDelay").floatValue = 0f;
            so.ApplyModifiedPropertiesWithoutUndo();
            return wave;
        }

        private sealed class RecordingBattleContext : IBattleFlowContext
        {
            public int SpawnedCount { get; private set; }
            public int AliveCount { get; set; }

            public bool TrySpawnEnemy(EnemyData enemyData)
            {
                SpawnedCount++;
                AliveCount++;
                return true;
            }

            public int GetAliveEnemyCount()
            {
                return AliveCount;
            }
        }
    }
}
