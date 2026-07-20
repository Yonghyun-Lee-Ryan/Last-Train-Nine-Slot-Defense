using LastTrain.Battle;
using LastTrain.Data;
using LastTrain.Enemy;
using LastTrain.Run;
using NUnit.Framework;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class TrainDamageServiceTests
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
            so.FindProperty("id").stringValue = "train_damage_test";
            so.FindProperty("trainDamage").floatValue = 12f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            _runState?.Dispose();
            Object.DestroyImmediate(_enemyData);
        }

        [Test]
        public void TryApplyTrainDamage_ReducesTrainHpOnce()
        {
            var enemy = new EnemyRuntime(_enemyData, 50f, Vector2.zero);

            bool applied = TrainDamageService.TryApplyTrainDamage(_runState, enemy);

            Assert.IsTrue(applied);
            Assert.AreEqual(88, _runState.Train.CurrentHp);
            Assert.AreEqual(EnemyResolution.ReachedTrain, enemy.Resolution);
        }

        [Test]
        public void TryApplyTrainDamage_SecondCall_IsIgnored()
        {
            var enemy = new EnemyRuntime(_enemyData, 50f, Vector2.zero);

            TrainDamageService.TryApplyTrainDamage(_runState, enemy);
            bool appliedAgain = TrainDamageService.TryApplyTrainDamage(_runState, enemy);

            Assert.IsFalse(appliedAgain);
            Assert.AreEqual(88, _runState.Train.CurrentHp);
        }
    }
}
