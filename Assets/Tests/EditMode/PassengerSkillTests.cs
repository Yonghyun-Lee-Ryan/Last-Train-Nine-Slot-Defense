using LastTrain.Core;
using LastTrain.Data;
using LastTrain.Enemy;
using LastTrain.Passenger;
using LastTrain.Passenger.Skills;
using LastTrain.Run;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class PassengerSkillTests
    {
        private PassengerData _passengerData;
        private EnemyData _enemyData;
        private RunState _runState;

        [SetUp]
        public void SetUp()
        {
            _passengerData = CreatePassenger("skill_knockback", baseAttack: 10f);
            _enemyData = CreateEnemy();
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
        public void Resolver_CreatesDistinctSkillInstancesWithoutControllerSwitch()
        {
            IPassengerSkill knockback = PassengerSkillResolver.Create(PassengerSkillIds.Knockback);
            IPassengerSkill heal = PassengerSkillResolver.Create(PassengerSkillIds.TrainHeal);
            IPassengerSkill turret = PassengerSkillResolver.Create(PassengerSkillIds.TemporaryTurret);
            IPassengerSkill crit = PassengerSkillResolver.Create(PassengerSkillIds.CriticalAreaDamage);
            IPassengerSkill unknown = PassengerSkillResolver.Create("unknown_skill");

            Assert.IsInstanceOf<KnockbackSkill>(knockback);
            Assert.IsInstanceOf<TrainHealSkill>(heal);
            Assert.IsInstanceOf<TemporaryTurretSkill>(turret);
            Assert.IsInstanceOf<CriticalAreaDamageSkill>(crit);
            Assert.AreSame(NullPassengerSkill.Instance, unknown);
        }

        [Test]
        public void Factory_WiresSkillFromPassengerData()
        {
            SetSkillId(_passengerData, PassengerSkillIds.TrainHeal);
            var runtime = PassengerRuntime.Create(_passengerData);
            PassengerController controller = PassengerFactory.CreateController(runtime);

            Assert.AreEqual(PassengerSkillIds.TrainHeal, controller.Skill.SkillId);
        }

        [Test]
        public void Knockback_MovesEnemyTowardSpawnAndDoesNotPassSpawn()
        {
            var runtime = PlaceOnGrid(_passengerData);
            var enemy = new EnemyRuntime(_enemyData, 100f, new Vector2(0f, 200f));
            var skill = new KnockbackSkill();
            Vector2 spawn = new Vector2(0f, 500f);
            Vector2 train = new Vector2(0f, 0f);

            skill.Tick(0f, BuildContext(runtime, enemies: new[] { enemy }, spawn: spawn, train: train));

            Assert.Greater(enemy.Position.y, 200f);
            Assert.LessOrEqual(enemy.Position.y, spawn.y);
        }

        [Test]
        public void TrainHeal_DoesNotExceedMaxHp_AndUsesNurseAbilityPercent()
        {
            SetSkillId(_passengerData, PassengerSkillIds.TrainHeal);
            var runtime = PlaceOnGrid(_passengerData);
            _runState.Train.ApplyDamage(20);
            int before = _runState.Train.CurrentHp;
            var modifiers = new AbilityModifiers { NurseHealPercent = 100f };
            var skill = new TrainHealSkill();

            skill.Tick(0f, BuildContext(runtime, modifiers: modifiers));

            Assert.Greater(_runState.Train.CurrentHp, before);
            Assert.LessOrEqual(_runState.Train.CurrentHp, _runState.Train.MaxHp);

            // 최대까지 채운 뒤 다시 회복해도 MaxHp를 넘지 않는다.
            _runState.Train.RestoreFull();
            skill.Tick(TrainHealSkill.BaseCooldownSeconds, BuildContext(runtime, modifiers: modifiers));
            Assert.AreEqual(_runState.Train.MaxHp, _runState.Train.CurrentHp);
        }

        [Test]
        public void TemporaryTurret_UsesPoolAndExpires()
        {
            SetSkillId(_passengerData, PassengerSkillIds.TemporaryTurret);
            var runtime = PlaceOnGrid(_passengerData);
            var service = new TemporaryTurretService();
            var skill = new TemporaryTurretSkill();
            var enemy = new EnemyRuntime(_enemyData, 100f, Vector2.zero);

            skill.Tick(0f, BuildContext(runtime, enemies: new[] { enemy }, turrets: service));
            Assert.AreEqual(1, service.ActiveCount);

            service.Tick(TemporaryTurretSkill.BaseDurationSeconds + 0.1f, new[] { enemy });
            Assert.AreEqual(0, service.ActiveCount);
            Assert.AreEqual(1, service.AvailableCount);

            skill.Tick(TemporaryTurretSkill.BaseCooldownSeconds, BuildContext(runtime, enemies: new[] { enemy }, turrets: service));
            Assert.AreEqual(1, service.ActiveCount);
            Assert.AreEqual(0, service.AvailableCount);
        }

        [Test]
        public void CriticalArea_DamagesEachEnemyOnce()
        {
            var enemyA = new EnemyRuntime(_enemyData, 100f, new Vector2(0f, 0f), "a");
            var enemyB = new EnemyRuntime(_enemyData, 100f, new Vector2(10f, 0f), "b");
            // 동일 인스턴스 ID가 리스트에 두 번 있어도 1회만
            var enemies = new[] { enemyA, enemyB, enemyA };

            int hitCount = CriticalAreaDamageSkill.ApplyAreaDamageOnce(
                enemies,
                Vector2.zero,
                radius: 50f,
                rawDamage: 10f);

            Assert.AreEqual(2, hitCount);
            Assert.AreEqual(90f, enemyA.CurrentHealth, 0.001f);
            Assert.AreEqual(90f, enemyB.CurrentHealth, 0.001f);
        }

        [Test]
        public void SkillValueMultiplier_ScalesWithStarLevel()
        {
            var runtime = PassengerRuntime.Create(_passengerData, starLevel: 1);
            Assert.AreEqual(1f, runtime.GetEffectiveSkillMultiplier(), 0.001f);

            runtime.SetStarLevel(3);
            Assert.AreEqual(1.5f, runtime.GetEffectiveSkillMultiplier(), 0.001f);
        }

        private PassengerSkillContext BuildContext(
            PassengerRuntime runtime,
            EnemyRuntime[] enemies = null,
            AbilityModifiers modifiers = null,
            Vector2? spawn = null,
            Vector2? train = null,
            ITemporaryTurretSpawner turrets = null)
        {
            return new PassengerSkillContext(
                runtime,
                Vector2.zero,
                300f,
                enemies ?? System.Array.Empty<EnemyRuntime>(),
                _runState.Train,
                modifiers ?? AbilityModifiers.Empty,
                spawn ?? new Vector2(0f, 500f),
                train ?? Vector2.zero,
                turrets,
                new RandomService(1));
        }

        private PassengerRuntime PlaceOnGrid(PassengerData data, int slotIndex = 0, int starLevel = 1)
        {
            var runtime = PassengerRuntime.Create(data, starLevel);
            Assert.IsTrue(_runState.TryPlacePassenger(slotIndex, runtime));
            return runtime;
        }

        private static PassengerData CreatePassenger(string skillId, float baseAttack)
        {
            var data = ScriptableObject.CreateInstance<PassengerData>();
            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = "test_skill_passenger";
            so.FindProperty("displayName").stringValue = "Test";
            so.FindProperty("baseAttack").floatValue = baseAttack;
            so.FindProperty("attackInterval").floatValue = 1f;
            so.FindProperty("range").floatValue = 5f;
            so.FindProperty("skillId").stringValue = skillId;

            SerializedProperty starLevels = so.FindProperty("starLevels");
            starLevels.arraySize = 3;
            WriteStar(starLevels.GetArrayElementAtIndex(0), 1, 1f, 1f, 1f);
            WriteStar(starLevels.GetArrayElementAtIndex(1), 2, 2.2f, 1.05f, 1.2f);
            WriteStar(starLevels.GetArrayElementAtIndex(2), 3, 4.8f, 1.1f, 1.5f);
            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }

        private static void SetSkillId(PassengerData data, string skillId)
        {
            var so = new SerializedObject(data);
            so.FindProperty("skillId").stringValue = skillId;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WriteStar(
            SerializedProperty element,
            int star,
            float attackMul,
            float speedMul,
            float skillMul)
        {
            element.FindPropertyRelative("starLevel").intValue = star;
            element.FindPropertyRelative("attackMultiplier").floatValue = attackMul;
            element.FindPropertyRelative("attackSpeedMultiplier").floatValue = speedMul;
            element.FindPropertyRelative("rangeBonus").floatValue = 0f;
            element.FindPropertyRelative("skillValueMultiplier").floatValue = skillMul;
        }

        private static EnemyData CreateEnemy()
        {
            var data = ScriptableObject.CreateInstance<EnemyData>();
            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = "skill_test_enemy";
            so.FindProperty("moveSpeed").floatValue = 2f;
            so.FindProperty("defense").floatValue = 0f;
            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }
    }
}
