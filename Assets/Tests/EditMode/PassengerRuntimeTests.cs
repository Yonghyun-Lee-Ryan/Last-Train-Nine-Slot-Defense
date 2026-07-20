using LastTrain.Data;
using LastTrain.Run;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class PassengerRuntimeTests
    {
        private PassengerData _data;

        [SetUp]
        public void SetUp()
        {
            _data = ScriptableObject.CreateInstance<PassengerData>();
            var so = new SerializedObject(_data);
            so.FindProperty("id").stringValue = "passenger_test";
            so.FindProperty("displayName").stringValue = "테스트";
            so.FindProperty("baseAttack").floatValue = 10f;
            so.FindProperty("attackInterval").floatValue = 1f;

            SerializedProperty starLevels = so.FindProperty("starLevels");
            starLevels.arraySize = 3;
            WriteStar(starLevels.GetArrayElementAtIndex(0), 1, 1f, 1f);
            WriteStar(starLevels.GetArrayElementAtIndex(1), 2, 2.2f, 1.05f);
            WriteStar(starLevels.GetArrayElementAtIndex(2), 3, 4.8f, 1.1f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_data);
        }

        [Test]
        public void GetEffectiveAttack_AppliesStarAndBuff()
        {
            var passenger = PassengerRuntime.Create(_data, starLevel: 2);
            passenger.AddBuff(new RuntimeBuff("test_buff", attackPercentBonus: 20f));

            float attack = passenger.GetEffectiveAttack();

            Assert.AreEqual(22f * 1.2f, attack, 0.0001f);
        }

        [Test]
        public void TryUpgradeStar_StopsAtMaxStar()
        {
            var passenger = PassengerRuntime.Create(_data, starLevel: 3);

            Assert.IsFalse(passenger.TryUpgradeStar());
            Assert.AreEqual(3, passenger.StarLevel);
        }

        [Test]
        public void TickAttackCooldown_ReducesRemaining()
        {
            var passenger = PassengerRuntime.Create(_data);
            passenger.SetAttackCooldownRemaining(1f);

            passenger.TickAttackCooldown(0.4f);

            Assert.AreEqual(0.6f, passenger.AttackCooldownRemaining, 0.0001f);
            Assert.IsFalse(passenger.IsAttackReady);
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
    }
}
