using LastTrain.Data;
using LastTrain.Enemy;
using NUnit.Framework;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class EnemyKnockbackTests
    {
        private EnemyData _enemyData;

        [SetUp]
        public void SetUp()
        {
            _enemyData = ScriptableObject.CreateInstance<EnemyData>();
            var so = new UnityEditor.SerializedObject(_enemyData);
            so.FindProperty("id").stringValue = "kb_enemy";
            so.FindProperty("moveSpeed").floatValue = 2f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_enemyData);
        }

        [Test]
        public void ApplyKnockback_PushesTowardSpawnAlongPath()
        {
            var enemy = new EnemyRuntime(_enemyData, 50f, new Vector2(0f, 100f));
            Vector2 spawn = new Vector2(0f, 400f);
            Vector2 train = Vector2.zero;

            EnemyMovementService.ApplyKnockback(enemy, spawn, train, distance: 50f);

            Assert.AreEqual(0f, enemy.Position.x, 0.001f);
            Assert.AreEqual(150f, enemy.Position.y, 0.001f);
        }

        [Test]
        public void ApplyKnockback_ClampsAtSpawn()
        {
            var enemy = new EnemyRuntime(_enemyData, 50f, new Vector2(0f, 380f));
            Vector2 spawn = new Vector2(0f, 400f);
            Vector2 train = Vector2.zero;

            EnemyMovementService.ApplyKnockback(enemy, spawn, train, distance: 100f);

            Assert.AreEqual(spawn.y, enemy.Position.y, 0.001f);
        }

        [Test]
        public void ApplyKnockback_UsesCurrentBentRouteSegment()
        {
            var enemy = new EnemyRuntime(_enemyData, 50f, new Vector2(50f, 50f));
            enemy.SetRouteSegment(new Vector2(0f, 100f), new Vector2(100f, 0f));

            EnemyMovementService.ApplyKnockback(
                enemy,
                new Vector2(100f, 200f),
                Vector2.zero,
                distance: 20f);

            Assert.Less(enemy.Position.x, 50f);
            Assert.Greater(enemy.Position.y, 50f);
            Assert.AreEqual(100f, enemy.Position.x + enemy.Position.y, 0.001f);
        }
    }
}
