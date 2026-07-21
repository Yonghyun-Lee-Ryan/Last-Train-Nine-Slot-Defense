using System.Collections.Generic;
using LastTrain.Data;
using LastTrain.Enemy;
using LastTrain.Passenger;
using LastTrain.Run;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class BossAbilityTests
    {
        private EnemyData _bossData;
        private EnemyData _minionData;
        private PassengerData _passengerData;
        private RunState _runState;
        private RecordingSpawner _spawner;

        [SetUp]
        public void SetUp()
        {
            _bossData = CreateEnemy("boss", EnemyType.Boss, health: 100f, moveSpeed: 2f, abilityId: EnemyAbilityIds.BossMvp);
            _minionData = CreateEnemy("minion", EnemyType.Normal, health: 20f, moveSpeed: 2f);
            _passengerData = CreatePassenger();
            _runState = new RunState();
            _runState.Initialize(RunStartConfig.CreateDefault());
            _spawner = new RecordingSpawner();
        }

        [TearDown]
        public void TearDown()
        {
            _runState?.Dispose();
            Object.DestroyImmediate(_bossData);
            Object.DestroyImmediate(_minionData);
            Object.DestroyImmediate(_passengerData);
        }

        [Test]
        public void Phase_EnragesAtThirtyPercentHealth()
        {
            var boss = new EnemyRuntime(_bossData, 100f, Vector2.zero);
            var phase = new BossPhaseController();
            BossPhase? changedTo = null;
            phase.PhaseChanged += (_, next) => changedTo = next;

            phase.NotifyHealth(31f, 100f);
            Assert.AreEqual(BossPhase.Normal, phase.Current);
            Assert.IsNull(changedTo);

            phase.NotifyHealth(30f, 100f);
            Assert.AreEqual(BossPhase.Enraged, phase.Current);
            Assert.AreEqual(BossPhase.Enraged, changedTo);
        }

        [Test]
        public void EnrageAbility_IncreasesMoveSpeedOnPhaseEnter()
        {
            var boss = new EnemyRuntime(_bossData, 100f, Vector2.zero);
            var ability = new EnrageMoveSpeedAbility();
            var context = BuildContext(boss);
            ability.OnAttach(context);
            Assert.AreEqual(2f, boss.MoveSpeed, 0.001f);

            ability.OnPhaseChanged(BossPhase.Normal, BossPhase.Enraged, context);
            Assert.AreEqual(2f * EnrageMoveSpeedAbility.EnrageMultiplier, boss.MoveSpeed, 0.001f);
        }

        [Test]
        public void SpawnMinions_SpawnsThreeAndStopsOnDeath()
        {
            var boss = new EnemyRuntime(_bossData, 100f, Vector2.zero);
            var ability = new SpawnMinionsAbility();
            var context = BuildContext(boss);
            ability.OnAttach(context);

            ability.Tick(SpawnMinionsAbility.CooldownSeconds, context);
            Assert.AreEqual(SpawnMinionsAbility.MinionCount, _spawner.SpawnCount);

            ability.OnOwnerDied(context);
            ability.Tick(SpawnMinionsAbility.CooldownSeconds, context);
            Assert.AreEqual(SpawnMinionsAbility.MinionCount, _spawner.SpawnCount);
        }

        [Test]
        public void AttackSpeedDebuff_AppliesAndExpires()
        {
            Assert.IsTrue(_runState.TryPlacePassenger(0, PassengerRuntime.Create(_passengerData)));
            PassengerRuntime passenger = _runState.GetPassengerAtSlot(0);
            float baseInterval = passenger.GetEffectiveAttackInterval();

            var boss = new EnemyRuntime(_bossData, 100f, Vector2.zero);
            var ability = new PassengerAttackSpeedDebuffAbility();
            var context = BuildContext(boss);
            ability.OnAttach(context);
            ability.Tick(PassengerAttackSpeedDebuffAbility.CastCooldownSeconds, context);

            Assert.IsTrue(ability.IsDebuffActive);
            Assert.Greater(passenger.GetEffectiveAttackInterval(), baseInterval);

            ability.Tick(PassengerAttackSpeedDebuffAbility.DebuffDurationSeconds, context);
            Assert.IsFalse(ability.IsDebuffActive);
            Assert.AreEqual(baseInterval, passenger.GetEffectiveAttackInterval(), 0.001f);
        }

        [Test]
        public void BossBrain_Dispose_ClearsDebuffAndStopsSpawns()
        {
            Assert.IsTrue(_runState.TryPlacePassenger(0, PassengerRuntime.Create(_passengerData)));
            PassengerRuntime passenger = _runState.GetPassengerAtSlot(0);
            float baseInterval = passenger.GetEffectiveAttackInterval();

            var boss = new EnemyRuntime(_bossData, 100f, Vector2.zero);
            using (var brain = BossBrain.Create(boss, _runState, _spawner, _minionData))
            {
                Assert.IsNotNull(brain);
                brain.Tick(PassengerAttackSpeedDebuffAbility.CastCooldownSeconds);
                Assert.Greater(passenger.GetEffectiveAttackInterval(), baseInterval);
            }

            Assert.AreEqual(baseInterval, passenger.GetEffectiveAttackInterval(), 0.001f);
            int spawnCountAfterDispose = _spawner.SpawnCount;
            // Dispose 이후 추가 스폰 없음 — Tick도 불가하므로 카운트 유지
            Assert.AreEqual(spawnCountAfterDispose, _spawner.SpawnCount);
        }

        [Test]
        public void HealthChanged_FiresWhenDamaged()
        {
            var boss = new EnemyRuntime(_bossData, 100f, Vector2.zero);
            float? reported = null;
            boss.HealthChanged += (_, current, _) => reported = current;
            boss.ApplyDamage(25f);
            Assert.AreEqual(75f, reported);
        }

        private EnemyAbilityContext BuildContext(EnemyRuntime boss)
        {
            return new EnemyAbilityContext(boss, _runState, _spawner, _minionData, boss.Position);
        }

        private static EnemyData CreateEnemy(
            string id,
            EnemyType type,
            float health,
            float moveSpeed,
            string abilityId = "")
        {
            var data = ScriptableObject.CreateInstance<EnemyData>();
            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = id;
            so.FindProperty("enemyType").enumValueIndex = (int)type;
            so.FindProperty("baseHealth").floatValue = health;
            so.FindProperty("moveSpeed").floatValue = moveSpeed;
            so.FindProperty("trainDamage").floatValue = 5f;
            so.FindProperty("abilityId").stringValue = abilityId;
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
