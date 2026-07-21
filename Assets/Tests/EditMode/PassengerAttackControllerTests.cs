using LastTrain.Battle;
using LastTrain.Data;
using LastTrain.Enemy;
using LastTrain.Passenger;
using LastTrain.Run;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class PassengerAttackControllerTests
    {
        private PassengerData _passengerData;
        private EnemyData _enemyData;
        private RecordingLauncher _launcher;
        private RunState _runState;

        [SetUp]
        public void SetUp()
        {
            _passengerData = CreatePassenger(baseAttack: 20f, attackInterval: 1f, range: 10f);
            _enemyData = CreateEnemy();
            _launcher = new RecordingLauncher();
            _runState = new RunState();
            _runState.Initialize(RunStartConfig.CreateDefault());
        }

        [TearDown]
        public void TearDown()
        {
            _runState?.Dispose();
            Object.DestroyImmediate(_passengerData);
            Object.DestroyImmediate(_enemyData);
        }

        [Test]
        public void Tick_AttacksWithEffectiveDamageAndSetsCooldown()
        {
            var runtime = PlaceOnGrid(_passengerData, starLevel: 2, slotIndex: 0);
            var enemy = new EnemyRuntime(_enemyData, 100f, new Vector2(3f, 0f));
            var enemies = new[] { enemy };
            var controller = new PassengerAttackController();

            bool attacked = controller.Tick(0f, runtime, Vector2.zero, 10f, enemies, _launcher);

            Assert.IsTrue(attacked);
            Assert.AreEqual(44f, _launcher.LastDamage, 0.001f);
            Assert.AreEqual(0.952f, runtime.GetEffectiveAttackInterval(), 0.01f);
            Assert.AreEqual(runtime.GetEffectiveAttackInterval(), runtime.AttackCooldownRemaining, 0.001f);
        }

        [Test]
        public void Tick_RespectsAttackInterval()
        {
            var runtime = PlaceOnGrid(_passengerData, slotIndex: 0);
            var enemy = new EnemyRuntime(_enemyData, 100f, new Vector2(2f, 0f));
            var enemies = new[] { enemy };
            var controller = new PassengerAttackController();

            controller.Tick(0f, runtime, Vector2.zero, 10f, enemies, _launcher);
            Assert.AreEqual(1, _launcher.LaunchCount);

            controller.Tick(0.5f, runtime, Vector2.zero, 10f, enemies, _launcher);
            Assert.AreEqual(1, _launcher.LaunchCount);

            controller.Tick(0.5f, runtime, Vector2.zero, 10f, enemies, _launcher);
            Assert.AreEqual(2, _launcher.LaunchCount);
        }

        [Test]
        public void Tick_NotOnGrid_DoesNotAttack()
        {
            var runtime = PassengerRuntime.Create(_passengerData);
            var enemy = new EnemyRuntime(_enemyData, 100f, new Vector2(2f, 0f));
            var controller = new PassengerAttackController();

            bool attacked = controller.Tick(0f, runtime, Vector2.zero, 10f, new[] { enemy }, _launcher);

            Assert.IsFalse(attacked);
            Assert.AreEqual(0, _launcher.LaunchCount);
        }

        [Test]
        public void Tick_NoTargetInRange_DoesNotAttack()
        {
            var runtime = PlaceOnGrid(_passengerData, slotIndex: 0);
            var enemy = new EnemyRuntime(_enemyData, 100f, new Vector2(100f, 0f));
            var controller = new PassengerAttackController();

            bool attacked = controller.Tick(0f, runtime, Vector2.zero, 5f, new[] { enemy }, _launcher);

            Assert.IsFalse(attacked);
            Assert.AreEqual(0, _launcher.LaunchCount);
        }

        private PassengerRuntime PlaceOnGrid(PassengerData data, int slotIndex = 0, int starLevel = 1)
        {
            var runtime = PassengerRuntime.Create(data, starLevel);
            Assert.IsTrue(_runState.TryPlacePassenger(slotIndex, runtime));
            return runtime;
        }

        private static PassengerData CreatePassenger(float baseAttack, float attackInterval, float range)
        {
            var data = ScriptableObject.CreateInstance<PassengerData>();
            var so = new UnityEditor.SerializedObject(data);
            so.FindProperty("id").stringValue = "test_passenger";
            so.FindProperty("displayName").stringValue = "Test";
            so.FindProperty("baseAttack").floatValue = baseAttack;
            so.FindProperty("attackInterval").floatValue = attackInterval;
            so.FindProperty("range").floatValue = range;

            SerializedProperty starLevels = so.FindProperty("starLevels");
            starLevels.arraySize = 3;
            WriteStar(starLevels.GetArrayElementAtIndex(0), 1, 1f, 1f);
            WriteStar(starLevels.GetArrayElementAtIndex(1), 2, 2.2f, 1.05f);
            WriteStar(starLevels.GetArrayElementAtIndex(2), 3, 4.8f, 1.1f);
            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }

        private static void WriteStar(
            SerializedProperty element,
            int starLevel,
            float attackMultiplier,
            float attackSpeedMultiplier)
        {
            element.FindPropertyRelative("starLevel").intValue = starLevel;
            element.FindPropertyRelative("attackMultiplier").floatValue = attackMultiplier;
            element.FindPropertyRelative("attackSpeedMultiplier").floatValue = attackSpeedMultiplier;
        }

        private static EnemyData CreateEnemy()
        {
            var data = ScriptableObject.CreateInstance<EnemyData>();
            var so = new UnityEditor.SerializedObject(data);
            so.FindProperty("id").stringValue = "test_enemy";
            so.FindProperty("displayName").stringValue = "Enemy";
            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }

        private sealed class RecordingLauncher : IProjectileLauncher
        {
            public int LaunchCount { get; private set; }
            public float LastDamage { get; private set; }

            public void Launch(Vector2 origin, EnemyRuntime target, float damage, string passengerId = null)
            {
                LaunchCount++;
                LastDamage = damage;
            }
        }
    }
}
