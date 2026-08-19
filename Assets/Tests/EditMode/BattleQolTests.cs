using LastTrain.Battle;
using LastTrain.Data;
using LastTrain.Passenger;
using LastTrain.Run;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class BattleQolTests
    {
        private RunState _runState;
        private PassengerData _office;

        [SetUp]
        public void SetUp()
        {
            MergeUndoService.Clear();
            _office = CreatePassenger("office", maxStars: 3);
            _runState = new RunState();
            _runState.Initialize(RunStartConfig.CreateDefault());
            _runState.Battle.StartRun();
        }

        [TearDown]
        public void TearDown()
        {
            MergeUndoService.Clear();
            _runState?.Dispose();
            Object.DestroyImmediate(_office);
            PlayerPrefs.DeleteKey("lasttrain.settings.battleSpeed");
        }

        [Test]
        public void BattleSpeedPreset_CyclesOneTwoThree()
        {
            Assert.AreEqual(2, BattleSpeedPreset.Cycle(1));
            Assert.AreEqual(3, BattleSpeedPreset.Cycle(2));
            Assert.AreEqual(1, BattleSpeedPreset.Cycle(3));
            Assert.AreEqual(3f, BattleSpeedPreset.ToTimeScale(3));
        }

        [Test]
        public void GameSettings_BattleSpeed_PersistsAcrossInstances()
        {
            var first = new LastTrain.Release.GameSettingsService();
            first.Load();
            first.SetBattleSpeed(3);

            var second = new LastTrain.Release.GameSettingsService();
            second.Load();
            Assert.AreEqual(3, second.BattleSpeed);
        }

        [Test]
        public void TryUndo_PreparingMerge_RestoresBothPassengers()
        {
            var source = PassengerRuntime.Create(_office, starLevel: 1);
            var target = PassengerRuntime.Create(_office, starLevel: 1);
            _runState.TryPlacePassenger(0, source);
            _runState.TryPlacePassenger(1, target);

            Assert.IsTrue(MergeService.TryMerge(_runState, 0, 1, out _));
            Assert.IsNull(_runState.GetPassengerAtSlot(0));
            Assert.AreEqual(2, _runState.GetPassengerAtSlot(1).StarLevel);
            Assert.AreEqual(1, _runState.History.MergeCount);
            Assert.IsTrue(MergeUndoService.CanUndo(_runState));

            Assert.IsTrue(MergeUndoService.TryUndo(_runState));
            Assert.IsNotNull(_runState.GetPassengerAtSlot(0));
            Assert.AreEqual(1, _runState.GetPassengerAtSlot(0).StarLevel);
            Assert.AreEqual(1, _runState.GetPassengerAtSlot(1).StarLevel);
            Assert.AreEqual(0, _runState.History.MergeCount);
            Assert.IsFalse(MergeUndoService.CanUndo(_runState));
        }

        [Test]
        public void TryUndo_DuringCombat_ReturnsFalse()
        {
            var source = PassengerRuntime.Create(_office, starLevel: 1);
            var target = PassengerRuntime.Create(_office, starLevel: 1);
            _runState.TryPlacePassenger(0, source);
            _runState.TryPlacePassenger(1, target);
            Assert.IsTrue(MergeService.TryMerge(_runState, 0, 1, out _));

            _runState.Battle.SetPhase(RunPhase.Fighting);
            Assert.IsFalse(MergeUndoService.CanUndo(_runState));
            Assert.IsFalse(MergeUndoService.TryUndo(_runState));
            Assert.AreEqual(2, _runState.GetPassengerAtSlot(1).StarLevel);
        }

        [Test]
        public void TryUndo_SecondCall_ReturnsFalse()
        {
            var source = PassengerRuntime.Create(_office, starLevel: 1);
            var target = PassengerRuntime.Create(_office, starLevel: 1);
            _runState.TryPlacePassenger(0, source);
            _runState.TryPlacePassenger(1, target);
            MergeService.TryMerge(_runState, 0, 1, out _);
            Assert.IsTrue(MergeUndoService.TryUndo(_runState));
            Assert.IsFalse(MergeUndoService.TryUndo(_runState));
        }

        private static PassengerData CreatePassenger(string id, int maxStars)
        {
            var data = ScriptableObject.CreateInstance<PassengerData>();
            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = id;
            so.FindProperty("baseAttack").floatValue = 10f;
            SerializedProperty stars = so.FindProperty("starLevels");
            stars.arraySize = maxStars;
            for (int i = 0; i < maxStars; i++)
            {
                SerializedProperty element = stars.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("starLevel").intValue = i + 1;
                element.FindPropertyRelative("attackMultiplier").floatValue = 1f + i;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }
    }
}
