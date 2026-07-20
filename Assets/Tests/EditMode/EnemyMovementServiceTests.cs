using LastTrain.Data;
using LastTrain.Enemy;
using NUnit.Framework;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class EnemyMovementServiceTests
    {
        private EnemyData _enemyData;

        [SetUp]
        public void SetUp()
        {
            _enemyData = ScriptableObject.CreateInstance<EnemyData>();
            var so = new UnityEditor.SerializedObject(_enemyData);
            so.FindProperty("id").stringValue = "move_test";
            so.FindProperty("moveSpeed").floatValue = 2f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_enemyData);
        }

        [Test]
        public void TickMove_AdvancesTowardTarget()
        {
            var enemy = new EnemyRuntime(_enemyData, 50f, new Vector2(0f, 10f));
            Vector2 target = new Vector2(0f, 0f);

            bool reached = EnemyMovementService.TickMove(enemy, target, 0.5f, 10f, 1f);

            Assert.IsFalse(reached);
            Assert.Less(enemy.Position.y, 10f);
            Assert.AreEqual(0f, enemy.Position.x, 0.001f);
        }

        [Test]
        public void TickMove_ReachesTargetWithinRadius()
        {
            var enemy = new EnemyRuntime(_enemyData, 50f, new Vector2(0f, 4f));
            Vector2 target = Vector2.zero;

            bool reached = EnemyMovementService.TickMove(enemy, target, 1f, 10f, 2f);

            Assert.IsTrue(reached);
        }

        [Test]
        public void TickMove_DeadEnemy_DoesNotMove()
        {
            var enemy = new EnemyRuntime(_enemyData, 10f, new Vector2(0f, 10f));
            enemy.ApplyDamage(100f);
            Vector2 before = enemy.Position;

            bool reached = EnemyMovementService.TickMove(enemy, Vector2.zero, 1f, 10f, 1f);

            Assert.IsFalse(reached);
            Assert.AreEqual(before, enemy.Position);
        }
    }
}
