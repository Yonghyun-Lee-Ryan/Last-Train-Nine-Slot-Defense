using LastTrain.Core;
using LastTrain.Data;
using LastTrain.Event;
using LastTrain.Passenger;
using LastTrain.Relic;
using LastTrain.Run;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class EventServiceTests
    {
        private RunState _runState;
        private GameDatabase _database;
        private EventService _eventService;
        private StationData _station;
        private PassengerData _passenger;

        [SetUp]
        public void SetUp()
        {
            _runState = new RunState();
            _runState.Initialize(RunStartConfig.CreateDefault());
            _runState.Battle.StartRun();
            _database = CreateDatabase(out _passenger, out EventData eventData);
            _eventService = new EventService(
                _runState,
                _database,
                new RelicManager(_runState, _database),
                new RandomService(77));
            _station = CreateStation("event_station", 4);
        }

        [TearDown]
        public void TearDown()
        {
            _runState?.Dispose();
            Object.DestroyImmediate(_database);
            Object.DestroyImmediate(_station);
            Object.DestroyImmediate(_passenger);
        }

        [Test]
        public void ConditionalChoice_IsHiddenWithoutPassenger()
        {
            Assert.IsTrue(_eventService.TryOpenEvent(_station));
            EventData eventData = _eventService.GetCurrentEvent();
            Assert.IsFalse(_eventService.IsChoiceVisible(eventData.Choices[1]));
        }

        [Test]
        public void SelectChoice_AppliesOnce_AndMarksResolved()
        {
            _runState.TryPlacePassenger(0, PassengerRuntime.Create(_passenger));
            Assert.IsTrue(_eventService.TryOpenEvent(_station));

            int coinsBefore = _runState.Currency.CurrentCoins;
            EventChoiceResult result = _eventService.TrySelectChoice(0);
            Assert.AreEqual(EventChoiceResult.Success, result);
            Assert.Greater(_runState.Currency.CurrentCoins, coinsBefore);
            Assert.IsTrue(_runState.Events.IsResolved);

            EventChoiceResult again = _eventService.TrySelectChoice(0);
            Assert.AreEqual(EventChoiceResult.AlreadyResolved, again);
        }

        [Test]
        public void GrantPassenger_UsesFirstEmptySlot_WhenSlotZeroOccupied()
        {
            _runState.TryPlacePassenger(0, PassengerRuntime.Create(_passenger));
            Assert.IsTrue(_eventService.TryOpenEvent(_station));
            Assert.IsTrue(_eventService.IsChoiceVisible(_eventService.GetCurrentEvent().Choices[1]));

            EventChoiceResult result = _eventService.TrySelectChoice(1);
            Assert.AreEqual(EventChoiceResult.Success, result);
            Assert.IsNotNull(_runState.GetPassengerAtSlot(1));
            Assert.AreEqual("passenger_police", _runState.GetPassengerAtSlot(1).Data.Id);
            Assert.AreEqual(0, _runState.PendingPassengers.Count);
        }

        [Test]
        public void GrantPassenger_WhenGridFull_QueuesUntilSlotFreedBySell()
        {
            for (int i = 0; i < RunState.GridSlotCount; i++)
            {
                Assert.IsTrue(_runState.TryPlacePassenger(i, PassengerRuntime.Create(_passenger)));
            }

            Assert.IsTrue(_eventService.TryOpenEvent(_station));
            EventChoiceResult result = _eventService.TrySelectChoice(1);
            Assert.AreEqual(EventChoiceResult.Success, result);
            Assert.AreEqual(1, _runState.PendingPassengers.Count);
            Assert.AreEqual(RunState.GridSlotCount, CountOccupiedSlots(_runState));

            Assert.IsTrue(PassengerSellService.TrySell(_runState, 3, out _));
            Assert.AreEqual(0, _runState.PendingPassengers.Count);
            Assert.IsNotNull(_runState.GetPassengerAtSlot(3));
            Assert.AreEqual("passenger_police", _runState.GetPassengerAtSlot(3).Data.Id);
        }

        private static int CountOccupiedSlots(RunState runState)
        {
            int count = 0;
            for (int i = 0; i < RunState.GridSlotCount; i++)
            {
                if (runState.GetPassengerAtSlot(i) != null)
                {
                    count++;
                }
            }

            return count;
        }

        private static GameDatabase CreateDatabase(out PassengerData passenger, out EventData eventData)
        {
            passenger = ScriptableObject.CreateInstance<PassengerData>();
            var passengerSo = new SerializedObject(passenger);
            passengerSo.FindProperty("id").stringValue = "passenger_police";
            passengerSo.FindProperty("displayName").stringValue = "경찰관";
            passengerSo.FindProperty("baseAttack").floatValue = 10f;
            passengerSo.FindProperty("attackInterval").floatValue = 1f;
            passengerSo.FindProperty("range").floatValue = 5f;
            passengerSo.ApplyModifiedPropertiesWithoutUndo();

            eventData = ScriptableObject.CreateInstance<EventData>();
            var eventSo = new SerializedObject(eventData);
            eventSo.FindProperty("id").stringValue = "event_test";
            eventSo.FindProperty("displayName").stringValue = "테스트 이벤트";
            eventSo.FindProperty("description").stringValue = "설명";
            SerializedProperty choices = eventSo.FindProperty("choices");
            choices.arraySize = 2;

            WriteChoice(choices.GetArrayElementAtIndex(0), "coin", "코인 받기", EventEffectType.AddCoins, 20f);
            WriteChoice(
                choices.GetArrayElementAtIndex(1),
                "police",
                "경찰 특별 선택",
                EventEffectType.GrantPassenger,
                0f,
                EventConditionType.RequiresPassenger,
                "passenger_police");
            eventSo.ApplyModifiedPropertiesWithoutUndo();

            var database = ScriptableObject.CreateInstance<GameDatabase>();
            var dbSo = new SerializedObject(database);
            dbSo.FindProperty("passengers").arraySize = 1;
            dbSo.FindProperty("passengers").GetArrayElementAtIndex(0).objectReferenceValue = passenger;
            dbSo.FindProperty("events").arraySize = 1;
            dbSo.FindProperty("events").GetArrayElementAtIndex(0).objectReferenceValue = eventData;
            dbSo.ApplyModifiedPropertiesWithoutUndo();
            return database;
        }

        private static void WriteChoice(
            SerializedProperty element,
            string id,
            string text,
            EventEffectType effectType,
            float value,
            EventConditionType conditionType = EventConditionType.None,
            string conditionTarget = "")
        {
            element.FindPropertyRelative("choiceId").stringValue = id;
            element.FindPropertyRelative("text").stringValue = text;
            SerializedProperty effects = element.FindPropertyRelative("effects");
            effects.arraySize = 1;
            effects.GetArrayElementAtIndex(0).FindPropertyRelative("effectType").enumValueIndex = (int)effectType;
            effects.GetArrayElementAtIndex(0).FindPropertyRelative("value").floatValue = value;
            effects.GetArrayElementAtIndex(0).FindPropertyRelative("targetId").stringValue =
                effectType == EventEffectType.GrantPassenger ? conditionTarget : string.Empty;

            SerializedProperty conditions = element.FindPropertyRelative("conditions");
            if (conditionType == EventConditionType.None)
            {
                conditions.arraySize = 0;
                return;
            }

            conditions.arraySize = 1;
            conditions.GetArrayElementAtIndex(0).FindPropertyRelative("conditionType").enumValueIndex = (int)conditionType;
            conditions.GetArrayElementAtIndex(0).FindPropertyRelative("targetId").stringValue = conditionTarget;
        }

        private static StationData CreateStation(string id, int index)
        {
            var station = ScriptableObject.CreateInstance<StationData>();
            var so = new SerializedObject(station);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("stationIndex").intValue = index;
            so.ApplyModifiedPropertiesWithoutUndo();
            return station;
        }
    }
}
