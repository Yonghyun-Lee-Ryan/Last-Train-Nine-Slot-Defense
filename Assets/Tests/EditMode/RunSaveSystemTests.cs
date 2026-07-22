using System;
using System.IO;
using LastTrain.Ability;
using LastTrain.Data;
using LastTrain.Run;
using LastTrain.Save;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class RunSaveSystemTests
    {
        [Test]
        public void RunSaveMapper_RoundTrip_RestorationsMatch()
        {
            // Arrange
            RunStartConfig config = RunStartConfig.CreateDefault();
            config.InitialStationIndex = 2;
            config.InitialTrainMaxHp = 120;
            config.InitialTrainCurrentHp = 80;
            config.InitialCoins = 33;

            RunState runState = new RunState();
            runState.Initialize(config);
            runState.Battle.StartRun(); // Preparing

            runState.Station.RestoreFromSave(2, "station_2", currentWaveIndex: 0, completedStationCount: 3);
            runState.Currency.RestoreFromSave(33, totalEarned: 200, totalSpent: 120);
            runState.History.RestoreFromSave(
                enemiesKilled: 5,
                mergeCount: 2,
                passengersSummoned: 7,
                passengersSold: 1,
                highestPassengerStar: 3,
                abilityCardsSelected: 4);

            PassengerData passenger = ScriptableObject.CreateInstance<PassengerData>();
            var passengerSo = new SerializedObject(passenger);
            passengerSo.FindProperty("id").stringValue = "p_test";
            passengerSo.FindProperty("displayName").stringValue = "P_Test";
            passengerSo.FindProperty("baseAttack").floatValue = 10f;
            passengerSo.FindProperty("attackInterval").floatValue = 1f;
            passengerSo.FindProperty("range").floatValue = 5f;
            passengerSo.ApplyModifiedPropertiesWithoutUndo();

            // 1 slot에 승객 배치
            runState.TryPlacePassengerFromSave(0, PassengerRuntime.Create(passenger, starLevel: 2));

            AbilityData ability = ScriptableObject.CreateInstance<AbilityData>();
            var abilitySo = new SerializedObject(ability);
            abilitySo.FindProperty("id").stringValue = "a_test";
            abilitySo.FindProperty("displayName").stringValue = "A_Test";
            abilitySo.FindProperty("description").stringValue = "desc";
            abilitySo.FindProperty("effectType").enumValueIndex = (int)AbilityEffectType.PassengerAttackPercent;
            abilitySo.FindProperty("effectValue").floatValue = 10f;
            abilitySo.FindProperty("allowDuplicate").boolValue = true;
            abilitySo.FindProperty("maxStack").intValue = 3;
            abilitySo.ApplyModifiedPropertiesWithoutUndo();

            // 스택 2로 선택
            runState.Abilities.RestoreSelectedExpanded(new[] { ability, ability });

            // Act (save)
            RunSaveData save = RunSaveMapper.CreateFromRunState(runState);
            Assert.IsNotNull(save);

            // GameDatabase minimal
            GameDatabase db = ScriptableObject.CreateInstance<GameDatabase>();
            var dbSo = new SerializedObject(db);
            dbSo.FindProperty("passengers").arraySize = 1;
            dbSo.FindProperty("passengers").GetArrayElementAtIndex(0).objectReferenceValue = passenger;
            dbSo.FindProperty("abilities").arraySize = 1;
            dbSo.FindProperty("abilities").GetArrayElementAtIndex(0).objectReferenceValue = ability;
            dbSo.ApplyModifiedPropertiesWithoutUndo();

            // new run
            RunState restored = new RunState();
            RunStartConfig restoredConfig = RunSaveMapper.CreateStartConfigFromSave(save);
            restored.Initialize(restoredConfig);
            restored.Battle.StartRun();

            bool applied = RunSaveMapper.ApplyToRunState(restored, save, db);

            // Assert
            Assert.IsTrue(applied);
            Assert.AreEqual(2, restored.Station.CurrentStationIndex);
            Assert.AreEqual(3, restored.Station.CompletedStationCount);
            Assert.AreEqual(80, restored.Train.CurrentHp);
            Assert.AreEqual(120, restored.Train.MaxHp);

            Assert.AreEqual(33, restored.Currency.CurrentCoins);
            Assert.AreEqual(200, restored.Currency.TotalEarned);
            Assert.AreEqual(120, restored.Currency.TotalSpent);

            Assert.AreEqual(5, restored.History.EnemiesKilled);
            Assert.AreEqual(2, restored.History.MergeCount);
            Assert.AreEqual(7, restored.History.PassengersSummoned);
            Assert.AreEqual(1, restored.History.PassengersSold);
            Assert.AreEqual(3, restored.History.HighestPassengerStar);
            Assert.AreEqual(4, restored.History.AbilityCardsSelected);

            Assert.IsNotNull(restored.GetPassengerAtSlot(0));
            Assert.AreEqual("p_test", restored.GetPassengerAtSlot(0).Data.Id);
            Assert.AreEqual(2, restored.GetPassengerAtSlot(0).StarLevel);

            var expanded = restored.Abilities.ExpandSelectedWithStacks();
            Assert.AreEqual(2, expanded.Count);
            Assert.AreEqual("a_test", expanded[0].Id);

            Assert.AreEqual(RunPhase.Preparing, restored.Battle.CurrentPhase);

            UnityEngine.Object.DestroyImmediate(passenger);
            UnityEngine.Object.DestroyImmediate(ability);
            UnityEngine.Object.DestroyImmediate(db);
            runState.Dispose();
            restored.Dispose();
        }

        [Test]
        public void RunSaveMapper_RoundTrip_PreservesDifficultyId()
        {
            RunStartConfig config = RunStartConfig.CreateDefault();
            config.DifficultyId = "hard_mode";

            RunState runState = new RunState();
            runState.Initialize(config);
            runState.Battle.StartRun();

            RunSaveData save = RunSaveMapper.CreateFromRunState(runState);
            Assert.AreEqual("hard_mode", save.difficultyId);

            RunState restored = new RunState();
            restored.Initialize(RunSaveMapper.CreateStartConfigFromSave(save));
            restored.Battle.StartRun();

            bool applied = RunSaveMapper.ApplyToRunState(restored, save, null);
            Assert.IsTrue(applied);
            Assert.AreEqual("hard_mode", restored.DifficultyId);

            runState.Dispose();
            restored.Dispose();
        }

        [Test]
        public void RunSaveMapper_RoundTrip_PreservesShopAndEventState()
        {
            RunState runState = new RunState();
            runState.Initialize(RunStartConfig.CreateDefault());
            runState.Battle.StartRun();

            runState.Shop.Restore(
                "shop_station_3",
                3,
                isActive: true,
                isResolved: false,
                new[]
                {
                    new Shop.ShopOffer
                    {
                        offerId = "offer_1",
                        itemType = Shop.ShopItemType.TrainHeal,
                        price = 25,
                        payloadValue = 20,
                        purchased = true,
                    },
                    new Shop.ShopOffer
                    {
                        offerId = "offer_2",
                        itemType = Shop.ShopItemType.RandomAbility,
                        price = 40,
                        payloadId = "ability_test",
                        purchased = false,
                    },
                });

            runState.Events.Restore(
                "event_station_4",
                "event_test",
                isActive: true,
                isResolved: false,
                selectedChoiceIndex: -1);

            RunSaveData save = RunSaveMapper.CreateFromRunState(runState);
            Assert.IsTrue(save.shopActive);
            Assert.IsFalse(save.shopResolved);
            Assert.AreEqual(2, save.shopOffers.Length);
            Assert.IsTrue(save.shopOffers[0].purchased);
            Assert.IsTrue(save.eventActive);
            Assert.AreEqual("event_test", save.eventId);

            RunState restored = new RunState();
            restored.Initialize(RunSaveMapper.CreateStartConfigFromSave(save));
            restored.Battle.StartRun();

            bool applied = RunSaveMapper.ApplyToRunState(restored, save, ScriptableObject.CreateInstance<GameDatabase>());
            Assert.IsTrue(applied);
            Assert.IsTrue(restored.Shop.IsActive);
            Assert.IsFalse(restored.Shop.IsResolved);
            Assert.AreEqual(2, restored.Shop.Offers.Count);
            Assert.IsTrue(restored.Shop.Offers[0].purchased);
            Assert.IsTrue(restored.Events.IsActive);
            Assert.AreEqual("event_test", restored.Events.EventId);
            Assert.AreEqual(RunPhase.ShopOpen, restored.Battle.CurrentPhase);

            runState.Dispose();
            restored.Dispose();
        }

        [Test]
        public void JsonSaveService_InvalidJson_ReturnsFalse()
        {
            string dir = Path.Combine(Path.GetTempPath(), "LastTrainSaveTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            string runPath = Path.Combine(dir, "run.json");
            string metaPath = Path.Combine(dir, "meta.json");

            File.WriteAllText(runPath, "{ invalid json");

            var service = new JsonSaveService(runPath, metaPath);
            bool loaded = service.TryLoadRun(out RunSaveData _);

            Assert.IsFalse(loaded);

            Directory.Delete(dir, recursive: true);
        }
    }
}

