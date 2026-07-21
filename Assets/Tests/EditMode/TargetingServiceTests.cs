using LastTrain.Battle;
using LastTrain.Data;
using LastTrain.Enemy;
using LastTrain.Passenger;
using NUnit.Framework;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class TargetingServiceTests
    {
        private EnemyData _normalData;
        private EnemyData _fastData;
        private EnemyData _bossData;

        [SetUp]
        public void SetUp()
        {
            _normalData = CreateEnemy("normal", EnemyType.Normal, moveSpeed: 2f);
            _fastData = CreateEnemy("fast", EnemyType.Normal, moveSpeed: 5f);
            _bossData = CreateEnemy("boss", EnemyType.Boss, moveSpeed: 1f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_normalData);
            Object.DestroyImmediate(_fastData);
            Object.DestroyImmediate(_bossData);
        }

        [Test]
        public void SelectTarget_Nearest_PicksClosestEnemy()
        {
            var near = new EnemyRuntime(_normalData, 50f, new Vector2(2f, 0f));
            var far = new EnemyRuntime(_normalData, 50f, new Vector2(8f, 0f));
            var enemies = new[] { near, far };

            EnemyRuntime target = TargetingService.SelectTarget(
                enemies, Vector2.zero, 10f, TargetPriority.Nearest);

            Assert.AreSame(near, target);
        }

        [Test]
        public void SelectTarget_Fastest_PicksHighestMoveSpeed()
        {
            var slow = new EnemyRuntime(_normalData, 50f, new Vector2(3f, 0f));
            var fast = new EnemyRuntime(_fastData, 50f, new Vector2(4f, 0f));
            var enemies = new[] { slow, fast };

            EnemyRuntime target = TargetingService.SelectTarget(
                enemies, Vector2.zero, 10f, TargetPriority.Fastest);

            Assert.AreSame(fast, target);
        }

        [Test]
        public void SelectTarget_LowestHealth_PicksWeakestEnemy()
        {
            var healthy = new EnemyRuntime(_normalData, 100f, new Vector2(3f, 0f));
            var weak = new EnemyRuntime(_normalData, 20f, new Vector2(4f, 0f));
            weak.ApplyDamage(10f);
            var enemies = new[] { healthy, weak };

            EnemyRuntime target = TargetingService.SelectTarget(
                enemies, Vector2.zero, 10f, TargetPriority.LowestHealth);

            Assert.AreSame(weak, target);
        }

        [Test]
        public void SelectTarget_BossFirst_PicksBossOverNormal()
        {
            var normal = new EnemyRuntime(_normalData, 50f, new Vector2(1f, 0f));
            var boss = new EnemyRuntime(_bossData, 200f, new Vector2(5f, 0f));
            var enemies = new[] { normal, boss };

            EnemyRuntime target = TargetingService.SelectTarget(
                enemies, Vector2.zero, 10f, TargetPriority.BossFirst);

            Assert.AreSame(boss, target);
        }

        [Test]
        public void SelectTarget_OutOfRange_ReturnsNull()
        {
            var far = new EnemyRuntime(_normalData, 50f, new Vector2(100f, 0f));
            var enemies = new[] { far };

            EnemyRuntime target = TargetingService.SelectTarget(
                enemies, Vector2.zero, 5f, TargetPriority.Nearest);

            Assert.IsNull(target);
        }

        [Test]
        public void SelectTarget_SpawnProtectedEnemy_ReturnsNull()
        {
            var protectedEnemy = new EnemyRuntime(_normalData, 50f, new Vector2(2f, 0f));
            protectedEnemy.SetTargetable(false);

            EnemyRuntime target = TargetingService.SelectTarget(
                new[] { protectedEnemy },
                Vector2.zero,
                10f,
                TargetPriority.Nearest);

            Assert.IsNull(target);
        }

        private static EnemyData CreateEnemy(string id, EnemyType type, float moveSpeed)
        {
            var data = ScriptableObject.CreateInstance<EnemyData>();
            var so = new UnityEditor.SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = id;
            so.FindProperty("moveSpeed").floatValue = moveSpeed;
            so.FindProperty("enemyType").enumValueIndex = (int)type;
            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }
    }
}
