using System.Collections.Generic;
using LastTrain.Data;
using LastTrain.Wave;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class WaveSpawnSchedulerTests
    {
        private EnemyData _enemyData;

        [SetUp]
        public void SetUp()
        {
            _enemyData = ScriptableObject.CreateInstance<EnemyData>();
            var so = new SerializedObject(_enemyData);
            so.FindProperty("id").stringValue = "spawn_test_enemy";
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_enemyData);
        }

        [Test]
        public void Reset_BuildsSortedSpawnQueue()
        {
            // 스폰 시각: t=0, t=0.3, t=0.5 (delayBeforeStart=0)
            WaveData wave = CreateWave(
                delayBeforeStart: 0f,
                enemy: _enemyData,
                countA: 2,
                intervalA: 0.5f,
                delayA: 0f,
                countB: 1,
                intervalB: 0f,
                delayB: 0.3f);

            try
            {
                Assert.AreEqual(3, wave.GetTotalEnemyCount());

                var scheduler = new WaveSpawnScheduler();
                scheduler.Reset(wave);
                Assert.AreEqual(3, scheduler.TotalPlanned);

                var output = new List<EnemyData>();

                Assert.AreEqual(1, scheduler.Tick(0.01f, output));
                Assert.AreEqual(1, output.Count);

                Assert.AreEqual(1, scheduler.Tick(0.31f, output));
                Assert.AreEqual(1, output.Count);

                Assert.AreEqual(1, scheduler.Tick(0.21f, output));
                Assert.AreEqual(1, output.Count);
                Assert.AreEqual(0, scheduler.RemainingScheduled);
                Assert.AreEqual(3, scheduler.SpawnedCount);
            }
            finally
            {
                Object.DestroyImmediate(wave);
            }
        }

        private static WaveData CreateWave(
            float delayBeforeStart,
            EnemyData enemy,
            int countA,
            float intervalA,
            float delayA,
            int countB,
            float intervalB,
            float delayB)
        {
            var wave = ScriptableObject.CreateInstance<WaveData>();
            var so = new SerializedObject(wave);
            so.FindProperty("id").stringValue = "wave_test";
            so.FindProperty("delayBeforeStart").floatValue = delayBeforeStart;

            SerializedProperty spawnsProp = so.FindProperty("spawns");
            spawnsProp.arraySize = 2;

            WriteSpawn(spawnsProp.GetArrayElementAtIndex(0), enemy, countA, intervalA, delayA);
            WriteSpawn(spawnsProp.GetArrayElementAtIndex(1), enemy, countB, intervalB, delayB);

            so.ApplyModifiedPropertiesWithoutUndo();
            return wave;
        }

        private static void WriteSpawn(
            SerializedProperty element,
            EnemyData enemy,
            int count,
            float interval,
            float delay)
        {
            element.FindPropertyRelative("enemy").objectReferenceValue = enemy;
            element.FindPropertyRelative("count").intValue = count;
            element.FindPropertyRelative("spawnInterval").floatValue = interval;
            element.FindPropertyRelative("spawnDelay").floatValue = delay;
        }
    }
}
