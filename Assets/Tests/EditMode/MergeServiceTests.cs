using LastTrain.Data;
using LastTrain.Passenger;
using LastTrain.Run;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class MergeServiceTests
    {
        private RunState _runState;
        private PassengerData _office;
        private PassengerData _nurse;

        [SetUp]
        public void SetUp()
        {
            _office = CreatePassenger("office", maxStars: 3);
            _nurse = CreatePassenger("nurse", maxStars: 3);
            _runState = new RunState();
            _runState.Initialize(RunStartConfig.CreateDefault());
        }

        [TearDown]
        public void TearDown()
        {
            _runState?.Dispose();
            Object.DestroyImmediate(_office);
            Object.DestroyImmediate(_nurse);
        }

        [Test]
        public void CanMerge_SameIdAndStar_ReturnsTrue()
        {
            var a = PassengerRuntime.Create(_office, starLevel: 1);
            var b = PassengerRuntime.Create(_office, starLevel: 1);

            Assert.IsTrue(MergeService.CanMerge(a, b));
        }

        [Test]
        public void CanMerge_DifferentId_ReturnsFalse()
        {
            var a = PassengerRuntime.Create(_office, starLevel: 1);
            var b = PassengerRuntime.Create(_nurse, starLevel: 1);

            Assert.IsFalse(MergeService.CanMerge(a, b));
        }

        [Test]
        public void CanMerge_DifferentStar_ReturnsFalse()
        {
            var a = PassengerRuntime.Create(_office, starLevel: 1);
            var b = PassengerRuntime.Create(_office, starLevel: 2);

            Assert.IsFalse(MergeService.CanMerge(a, b));
        }

        [Test]
        public void CanMerge_MaxStar_ReturnsFalse()
        {
            var a = PassengerRuntime.Create(_office, starLevel: 3);
            var b = PassengerRuntime.Create(_office, starLevel: 3);

            Assert.IsFalse(MergeService.CanMerge(a, b));
        }

        [Test]
        public void TryMerge_UpgradesTargetAndRemovesSource()
        {
            var source = PassengerRuntime.Create(_office, starLevel: 1);
            var target = PassengerRuntime.Create(_office, starLevel: 1);
            _runState.TryPlacePassenger(0, source);
            _runState.TryPlacePassenger(4, target);
            float attackBefore = target.GetEffectiveAttack();

            bool merged = MergeService.TryMerge(_runState, 0, 4, out MergeResult result);

            Assert.IsTrue(merged);
            Assert.IsNull(_runState.GetPassengerAtSlot(0));
            Assert.AreSame(target, _runState.GetPassengerAtSlot(4));
            Assert.AreEqual(2, target.StarLevel);
            Assert.AreEqual(2, result.ResultingStarLevel);
            Assert.AreEqual(1, _runState.History.MergeCount);
            Assert.AreEqual(1, _runState.AllPassengers.Count);
            Assert.Greater(target.GetEffectiveAttack(), attackBefore);
        }

        [Test]
        public void TryMerge_MaxStar_Fails()
        {
            var source = PassengerRuntime.Create(_office, starLevel: 3);
            var target = PassengerRuntime.Create(_office, starLevel: 3);
            _runState.TryPlacePassenger(0, source);
            _runState.TryPlacePassenger(4, target);

            bool merged = MergeService.TryMerge(_runState, 0, 4, out _);

            Assert.IsFalse(merged);
            Assert.AreEqual(3, source.StarLevel);
            Assert.AreEqual(3, target.StarLevel);
            Assert.AreEqual(0, _runState.History.MergeCount);
            Assert.AreEqual(2, _runState.AllPassengers.Count);
        }

        private static PassengerData CreatePassenger(string id, int maxStars)
        {
            var data = ScriptableObject.CreateInstance<PassengerData>();
            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = id;
            so.FindProperty("baseAttack").floatValue = 10f;
            so.FindProperty("attackInterval").floatValue = 1f;

            SerializedProperty starLevels = so.FindProperty("starLevels");
            starLevels.arraySize = maxStars;
            for (int i = 0; i < maxStars; i++)
            {
                SerializedProperty element = starLevels.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("starLevel").intValue = i + 1;
                element.FindPropertyRelative("attackMultiplier").floatValue = 1f + i * 1.2f;
                element.FindPropertyRelative("attackSpeedMultiplier").floatValue = 1f + i * 0.05f;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }
    }
}
