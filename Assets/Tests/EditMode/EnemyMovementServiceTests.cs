using LastTrain.Data;
using LastTrain.Battle;
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

            // moveSpeed=2, scale=10 → 20u/s, 0.4s = 8u (아직 도달하지 않음)
            bool reached = EnemyMovementService.TickMove(enemy, target, 0.4f, 10f, 1f);

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

        [Test]
        public void MvpTuning_FastEnemyHasAtLeastFourSecondsOfVisibleTravel()
        {
            const float fastEnemyDataSpeed = 3.5f;
            float pathLength = BattleConstants.GetEnemyPathLength();
            float travelDistance = pathLength - 32f;
            float travelSeconds = travelDistance
                                  / (fastEnemyDataSpeed * BattleConstants.MoveSpeedToWorldScale);

            Assert.GreaterOrEqual(travelSeconds, 4f);
        }

        [Test]
        public void MvpTuning_BackLeftSeatCannotSnipeSpawn()
        {
            const float maxPassengerDataRange = 6f;
            float maxRange = maxPassengerDataRange * BattleConstants.RangeToWorldScale;
            var backLeftSeat = new Vector2(-232f, -532f);
            float spawnDistance = Vector2.Distance(
                backLeftSeat,
                BattleConstants.SpawnAnchoredPosition);

            Assert.Less(maxRange, spawnDistance);
        }

        [Test]
        public void MvpLayout_EnemyPathIsFourLaneZigzag()
        {
            Assert.AreEqual(4, BattleConstants.EnemyPathLaneYs.Length);
            Assert.AreEqual(6, BattleConstants.EnemyWaypointAnchoredPositions.Length);
            Assert.AreEqual(BattleConstants.EnemyPathRightX, BattleConstants.SpawnAnchoredPosition.x, 0.001f);
            Assert.AreEqual(BattleConstants.EnemyPathLeftX, BattleConstants.EnemyWaypointAnchoredPositions[0].x, 0.001f);
            Assert.AreEqual(BattleConstants.EnemyPathRightX, BattleConstants.EnemyWaypointAnchoredPositions[2].x, 0.001f);
            Assert.Greater(BattleConstants.GetEnemyPathLength(), 3000f);
        }
    }
}
