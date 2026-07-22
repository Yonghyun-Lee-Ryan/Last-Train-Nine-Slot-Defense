using System.Collections.Generic;
using LastTrain.Data;
using LastTrain.Difficulty;
using LastTrain.Enemy;
using LastTrain.Run;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class EnemySpecialAbilityTests
    {
        private EnemyData _normalData;
        private EnemyData _splitMinionData;
        private RunState _runState;
        private RecordingSpawner _spawner;

        [SetUp]
        public void SetUp()
        {
            _normalData = CreateEnemy("normal", EnemyType.Normal, 50f, 2f);
            _splitMinionData = CreateEnemy("split_minion", EnemyType.Fast, 20f, 3f);
            _runState = new RunState();
            _runState.Initialize(RunStartConfig.CreateDefault());
            _spawner = new RecordingSpawner();
        }

        [TearDown]
        public void TearDown()
        {
            _runState?.Dispose();
            Object.DestroyImmediate(_normalData);
            Object.DestroyImmediate(_splitMinionData);
        }

        [Test]
        public void SplitOnDeath_SpawnsTwoMinions()
        {
            var owner = new EnemyRuntime(_normalData, 50f, Vector2.zero);
            var ability = new SplitOnDeathAbility();
            var context = new EnemyAbilityContext(
                owner,
                _runState,
                _spawner,
                _normalData,
                owner.Position,
                System.Array.Empty<EnemyRuntime>(),
                _splitMinionData);

            ability.OnOwnerDied(context);
            Assert.AreEqual(SplitOnDeathAbility.SplitCount, _spawner.SpawnCount);
        }

        [Test]
        public void NearbyBuff_RemovesSpeedBonusOnDeath()
        {
            var watcher = new EnemyRuntime(_normalData, 50f, Vector2.zero, "watcher");
            var ally = new EnemyRuntime(_normalData, 50f, new Vector2(10f, 0f), "ally");
            ally.MoveSpeedMultiplier = 1f;
            var enemies = new List<EnemyRuntime> { watcher, ally };
            var ability = new NearbyEnemyBuffAbility();
            var context = new EnemyAbilityContext(
                watcher,
                _runState,
                _spawner,
                _normalData,
                watcher.Position,
                enemies);

            ability.OnAttach(context);
            ability.Tick(0f, context);
            Assert.Greater(ally.MoveSpeedMultiplier, 1f);

            ability.OnOwnerDied(context);
            Assert.AreEqual(1f, ally.MoveSpeedMultiplier, 0.001f);
        }

        [Test]
        public void SeatBlock_PreventsPassengerAttack()
        {
            Assert.IsTrue(_runState.TryPlacePassenger(0, PassengerRuntime.Create(CreatePassenger())));
            PassengerRuntime passenger = _runState.GetPassengerAtSlot(0);
            var blocker = new EnemyRuntime(_normalData, 50f, Vector2.zero);
            var ability = new SeatBlockAbility();
            var context = new EnemyAbilityContext(blocker, _runState, _spawner, _normalData, blocker.Position);

            ability.OnAttach(context);
            ability.Tick(SeatBlockAbility.CastCooldownSeconds, context);

            Assert.IsTrue(passenger.IsAttackBlocked);
            passenger.TickAttackBlock(SeatBlockAbility.BlockDurationSeconds);
            Assert.IsFalse(passenger.IsAttackBlocked);
        }

        [Test]
        public void PeriodicShield_HealsBoss()
        {
            var boss = new EnemyRuntime(_normalData, 100f, Vector2.zero);
            boss.ApplyDamage(40f);
            var ability = new PeriodicShieldAbility();
            var context = new EnemyAbilityContext(boss, _runState, _spawner, _normalData, boss.Position);

            ability.OnAttach(context);
            ability.Tick(PeriodicShieldAbility.CooldownSeconds, context);

            Assert.Greater(boss.CurrentHealth, 60f);
        }

        [Test]
        public void Blackout_OnlyActivatesOnHighDifficulty()
        {
            Assert.IsTrue(_runState.TryPlacePassenger(0, PassengerRuntime.Create(CreatePassenger())));
            PassengerRuntime passenger = _runState.GetPassengerAtSlot(0);
            float baseInterval = passenger.GetEffectiveAttackInterval();

            var boss = new EnemyRuntime(_normalData, 100f, Vector2.zero);
            var ability = new BlackoutAbility();
            var context = new EnemyAbilityContext(boss, _runState, _spawner, _normalData, boss.Position);

            ability.OnAttach(context);
            ability.Tick(BlackoutAbility.CastCooldownSeconds, context);
            Assert.AreEqual(baseInterval, passenger.GetEffectiveAttackInterval(), 0.001f);

            _runState.RestoreDifficulty(DifficultyIds.Express);
            ability = new BlackoutAbility();
            ability.OnAttach(context);
            ability.Tick(BlackoutAbility.CastCooldownSeconds, context);
            Assert.Greater(passenger.GetEffectiveAttackInterval(), baseInterval);
        }

        [Test]
        public void Phase_FinalBoss_OpensDoorAtSixtyPercent()
        {
            var phase = new BossPhaseController();
            phase.Configure(BossPhaseThresholds.DefaultFinalBoss);

            phase.NotifyHealth(61f, 100f);
            Assert.AreEqual(BossPhase.Normal, phase.Current);

            phase.NotifyHealth(60f, 100f);
            Assert.AreEqual(BossPhase.DoorOpen, phase.Current);

            phase.NotifyHealth(30f, 100f);
            Assert.AreEqual(BossPhase.Enraged, phase.Current);
        }

        private static EnemyData CreateEnemy(string id, EnemyType type, float health, float moveSpeed)
        {
            var data = ScriptableObject.CreateInstance<EnemyData>();
            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = id;
            so.FindProperty("enemyType").enumValueIndex = (int)type;
            so.FindProperty("baseHealth").floatValue = health;
            so.FindProperty("moveSpeed").floatValue = moveSpeed;
            so.FindProperty("trainDamage").floatValue = 5f;
            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }

        private static PassengerData CreatePassenger()
        {
            var data = ScriptableObject.CreateInstance<PassengerData>();
            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = "p";
            so.FindProperty("displayName").stringValue = "p";
            so.FindProperty("baseAttack").floatValue = 10f;
            so.FindProperty("attackInterval").floatValue = 1f;
            so.FindProperty("range").floatValue = 5f;
            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }

        private sealed class RecordingSpawner : IEnemySpawner
        {
            public int SpawnCount { get; private set; }

            public bool TrySpawn(EnemyData data, Vector2? position = null)
            {
                if (data == null)
                {
                    return false;
                }

                SpawnCount++;
                return true;
            }
        }
    }
}
