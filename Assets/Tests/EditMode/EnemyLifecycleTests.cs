using LastTrain.Battle;
using LastTrain.Data;
using LastTrain.Enemy;
using LastTrain.Run;
using NUnit.Framework;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class EnemyLifecycleTests
    {
        private RunState _runState;
        private EnemyData _enemyData;

        [SetUp]
        public void SetUp()
        {
            _runState = new RunState();
            _runState.Initialize(RunStartConfig.CreateDefault());

            _enemyData = ScriptableObject.CreateInstance<EnemyData>();
            var so = new UnityEditor.SerializedObject(_enemyData);
            so.FindProperty("id").stringValue = "lifecycle_test";
            so.FindProperty("trainDamage").floatValue = 8f;
            so.FindProperty("coinReward").intValue = 4;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            _runState?.Dispose();
            Object.DestroyImmediate(_enemyData);
        }

        [Test]
        public void TryResolve_OnlyRunsOnce()
        {
            var enemy = new EnemyRuntime(_enemyData, 50f, Vector2.zero);
            int diedCount = 0;
            enemy.Died += _ => diedCount++;

            Assert.IsTrue(enemy.TryResolve(EnemyResolution.Killed));
            Assert.IsFalse(enemy.TryResolve(EnemyResolution.ReachedTrain));
            Assert.AreEqual(1, diedCount);
            Assert.AreEqual(EnemyResolution.Killed, enemy.Resolution);
        }

        [Test]
        public void KillAndTrainReach_DoNotDoubleProcess()
        {
            var enemy = new EnemyRuntime(_enemyData, 10f, Vector2.zero);

            DamageService.ApplyDamage(enemy, 100f);
            bool trainApplied = TrainDamageService.TryApplyTrainDamage(_runState, enemy);
            bool rewardGranted = EnemyRewardService.TryGrantKillReward(_runState, enemy);

            Assert.IsFalse(trainApplied);
            Assert.IsTrue(rewardGranted);
            Assert.AreEqual(4, _runState.Currency.CurrentCoins);
            Assert.AreEqual(100, _runState.Train.CurrentHp);
            Assert.AreEqual(1, _runState.History.EnemiesKilled);
        }

        [Test]
        public void TrainReachThenKill_DoNotDoubleProcess()
        {
            var enemy = new EnemyRuntime(_enemyData, 50f, Vector2.zero);

            TrainDamageService.TryApplyTrainDamage(_runState, enemy);
            DamageService.ApplyDamage(enemy, 100f);
            bool rewardGranted = EnemyRewardService.TryGrantKillReward(_runState, enemy);

            Assert.IsFalse(rewardGranted);
            Assert.AreEqual(0, _runState.Currency.CurrentCoins);
            Assert.AreEqual(92, _runState.Train.CurrentHp);
            Assert.AreEqual(0, _runState.History.EnemiesKilled);
        }
    }
}
