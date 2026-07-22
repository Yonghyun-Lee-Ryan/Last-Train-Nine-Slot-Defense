using System.Collections.Generic;
using LastTrain.Battle;
using LastTrain.Core;
using LastTrain.Data;
using LastTrain.Event;
using LastTrain.Relic;
using LastTrain.Run;
using LastTrain.Shop;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class StationHandlerTests
    {
        private RunState _runState;
        private NonCombatStationServices _services;
        private GameDatabase _database;

        [SetUp]
        public void SetUp()
        {
            _runState = new RunState();
            _runState.Initialize(RunStartConfig.CreateDefault());
            _runState.Battle.StartRun();
            _database = CreateMinimalDatabase();
            var relicManager = new RelicManager(_runState, _database);
            _services = new NonCombatStationServices(
                new ShopService(_runState, _database, relicManager, new RandomService(1)),
                new EventService(_runState, _database, relicManager, new RandomService(2)),
                relicManager);
        }

        [TearDown]
        public void TearDown()
        {
            _runState?.Dispose();
            Object.DestroyImmediate(_database);
        }

        [Test]
        public void EventHandler_OpensEventWithoutCompleting()
        {
            StationData station = CreateStation("event_station", StationType.Event, 4, rewardCoins: 22);
            int completedCount = 0;
            var context = new StationHandlerContext(_runState, station, () => completedCount++, _services);

            Assert.IsTrue(EventStationHandler.Instance.TryActivate(context));
            Assert.AreEqual(0, completedCount);
            Assert.AreEqual(RunPhase.EventOpen, _runState.Battle.CurrentPhase);
            Object.DestroyImmediate(station);
        }

        [Test]
        public void ShopHandler_OpensShopWithoutCompleting()
        {
            StationData station = CreateStation("shop_station", StationType.Shop, 8, rewardCoins: 18);
            int completedCount = 0;
            var context = new StationHandlerContext(_runState, station, () => completedCount++, _services);

            Assert.IsTrue(ShopStationHandler.Instance.TryActivate(context));
            Assert.AreEqual(0, completedCount);
            Assert.AreEqual(RunPhase.ShopOpen, _runState.Battle.CurrentPhase);
            Object.DestroyImmediate(station);
        }

        [Test]
        public void RestHandler_HealsTrainWithoutWaveManager()
        {
            _runState.Train.ApplyDamage(20);
            int hpBefore = _runState.Train.CurrentHp;

            StationData station = CreateStation("rest_station", StationType.Rest, 1, rewardCoins: 0);
            int completedCount = 0;
            var context = new StationHandlerContext(_runState, station, () => completedCount++);

            Assert.IsTrue(RestStationHandler.Instance.TryActivate(context));
            Assert.Greater(_runState.Train.CurrentHp, hpBefore);
            Assert.AreEqual(1, completedCount);
            Object.DestroyImmediate(station);
        }

        private static GameDatabase CreateMinimalDatabase()
        {
            var eventData = ScriptableObject.CreateInstance<EventData>();
            var eventSo = new SerializedObject(eventData);
            eventSo.FindProperty("id").stringValue = "event_min";
            eventSo.FindProperty("displayName").stringValue = "min";
            eventSo.FindProperty("description").stringValue = "min";
            SerializedProperty choices = eventSo.FindProperty("choices");
            choices.arraySize = 1;
            choices.GetArrayElementAtIndex(0).FindPropertyRelative("choiceId").stringValue = "a";
            choices.GetArrayElementAtIndex(0).FindPropertyRelative("text").stringValue = "ok";
            eventSo.ApplyModifiedPropertiesWithoutUndo();

            var database = ScriptableObject.CreateInstance<GameDatabase>();
            var dbSo = new SerializedObject(database);
            dbSo.FindProperty("events").arraySize = 1;
            dbSo.FindProperty("events").GetArrayElementAtIndex(0).objectReferenceValue = eventData;
            dbSo.ApplyModifiedPropertiesWithoutUndo();
            return database;
        }

        private static StationData CreateStation(string id, StationType type, int stationIndex, int rewardCoins)
        {
            var station = ScriptableObject.CreateInstance<StationData>();
            var so = new SerializedObject(station);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = id;
            so.FindProperty("stationType").enumValueIndex = (int)type;
            so.FindProperty("stationIndex").intValue = stationIndex;
            so.FindProperty("rewardCoins").intValue = rewardCoins;
            so.ApplyModifiedPropertiesWithoutUndo();
            return station;
        }
    }
}
