using LastTrain.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class PassengerDataTests
    {
        private PassengerData _passenger;

        [SetUp]
        public void SetUp()
        {
            _passenger = ScriptableObject.CreateInstance<PassengerData>();

            var so = new SerializedObject(_passenger);
            so.FindProperty("id").stringValue = "passenger_test";
            so.FindProperty("displayName").stringValue = "테스트 승객";
            so.FindProperty("baseAttack").floatValue = 10f;
            so.FindProperty("attackInterval").floatValue = 1f;
            so.FindProperty("range").floatValue = 5f;

            SerializedProperty starLevels = so.FindProperty("starLevels");
            starLevels.arraySize = 3;
            SetStarLevel(starLevels.GetArrayElementAtIndex(0), 1, 1.0f, 1.0f);
            SetStarLevel(starLevels.GetArrayElementAtIndex(1), 2, 2.2f, 1.05f);
            SetStarLevel(starLevels.GetArrayElementAtIndex(2), 3, 4.8f, 1.1f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            if (_passenger != null)
            {
                Object.DestroyImmediate(_passenger);
            }
        }

        [Test]
        public void GetAttackAtStar_UsesReadmeMultipliers()
        {
            Assert.AreEqual(10f, _passenger.GetAttackAtStar(1), 0.0001f);
            Assert.AreEqual(22f, _passenger.GetAttackAtStar(2), 0.0001f);
            Assert.AreEqual(48f, _passenger.GetAttackAtStar(3), 0.0001f);
        }

        [Test]
        public void GetAttackIntervalAtStar_FasterAtHigherStar()
        {
            float star1 = _passenger.GetAttackIntervalAtStar(1);
            float star3 = _passenger.GetAttackIntervalAtStar(3);

            Assert.AreEqual(1f, star1, 0.0001f);
            Assert.Less(star3, star1);
        }

        [Test]
        public void TryGetStarData_InvalidStar_ReturnsFalse()
        {
            Assert.IsFalse(_passenger.TryGetStarData(0, out _));
            Assert.IsFalse(_passenger.TryGetStarData(4, out _));
        }

        [Test]
        public void GetSellPrice_MatchesMvpDefaults()
        {
            var so = new SerializedObject(_passenger);
            so.FindProperty("sellPriceStar1").intValue = 5;
            so.FindProperty("sellPriceStar2").intValue = 12;
            so.FindProperty("sellPriceStar3").intValue = 28;
            so.ApplyModifiedPropertiesWithoutUndo();

            Assert.AreEqual(5, _passenger.GetSellPrice(1));
            Assert.AreEqual(12, _passenger.GetSellPrice(2));
            Assert.AreEqual(28, _passenger.GetSellPrice(3));
        }

        private static void SetStarLevel(
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
