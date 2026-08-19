using System.Collections.Generic;
using LastTrain.Core;
using LastTrain.Data;
using LastTrain.Passenger;
using LastTrain.Relic;
using LastTrain.Run;
using LastTrain.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class SummonManagerTests
    {
        private RunState _runState;
        private PassengerData _passenger;
        private SummonEconomyConfig _config;

        [SetUp]
        public void SetUp()
        {
            _passenger = CreatePassenger("office");
            _config = ScriptableObject.CreateInstance<SummonEconomyConfig>();
            var configSo = new SerializedObject(_config);
            configSo.FindProperty("baseSummonCost").intValue = 10;
            configSo.FindProperty("summonCostIncrease").intValue = 2;
            configSo.FindProperty("offerCount").intValue = 3;
            configSo.FindProperty("freeRerollsPerRun").intValue = 1;
            configSo.FindProperty("adRerollsPerRun").intValue = 2;
            configSo.ApplyModifiedPropertiesWithoutUndo();

            _runState = new RunState();
            _runState.Initialize(new RunStartConfig { InitialCoins = 30 });
            _runState.Battle.StartRun();
        }

        [TearDown]
        public void TearDown()
        {
            _runState?.Dispose();
            Object.DestroyImmediate(_passenger);
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void TryBeginSummon_NoEmptySlot_Fails()
        {
            for (int i = 0; i < RunState.GridSlotCount; i++)
            {
                _runState.TryPlacePassenger(i, PassengerRuntime.Create(_passenger));
            }

            SummonManager manager = CreateManager();
            SummonRequestResult result = manager.TryBeginSummon();

            Assert.AreEqual(SummonRequestResult.NoEmptySlot, result);
            Assert.IsFalse(manager.HasActiveOffers);
        }

        [Test]
        public void TryBeginSummon_NotEnoughCoins_Fails()
        {
            _runState.Dispose();
            _runState = new RunState();
            _runState.Initialize(new RunStartConfig { InitialCoins = 5 });
            _runState.Battle.StartRun();

            SummonManager manager = CreateManager();
            Assert.AreEqual(SummonRequestResult.NotEnoughCoins, manager.TryBeginSummon());
        }

        [Test]
        public void TrySelectOffer_SpendsCoinsAndPlacesPassengerAtomically()
        {
            SummonManager manager = CreateManager();
            Assert.AreEqual(SummonRequestResult.Success, manager.TryBeginSummon());
            Assert.AreEqual(30, _runState.Currency.CurrentCoins);

            SelectOfferResult select = manager.TrySelectOffer(0, out PassengerRuntime placed);

            Assert.AreEqual(SelectOfferResult.Success, select);
            Assert.IsNotNull(placed);
            Assert.AreEqual(20, _runState.Currency.CurrentCoins);
            Assert.AreEqual(1, _runState.Summon.PaidSummonCount);
            Assert.AreEqual(12, manager.CurrentSummonCost);
            Assert.IsFalse(manager.HasActiveOffers);
        }

        [Test]
        public void SummonCostLabel_FirstRelicSummon_ReadsAsFree()
        {
            GameDatabase database = GameDatabaseLocator.Load();
            Assume.That(database, Is.Not.Null);
            var relics = new RelicManager(_runState, database);
            Assume.That(relics.TryAcquire("relic_broken_card"));
            Assert.AreEqual("첫 소환 무료", SummonPanelController.FormatSummonCostLabel(10, _runState));
        }

        [Test]
        public void TrySell_GivesCoinsAndRemovesPassenger()
        {
            var passenger = PassengerRuntime.Create(_passenger, starLevel: 1);
            _runState.TryPlacePassenger(0, passenger);

            bool sold = PassengerSellService.TrySell(_runState, 0, out int coins);

            Assert.IsTrue(sold);
            Assert.AreEqual(5, coins);
            Assert.IsNull(_runState.GetPassengerAtSlot(0));
            Assert.AreEqual(35, _runState.Currency.CurrentCoins);
        }

        [Test]
        public void CancelOffers_ThenBeginAgain_RestoresSameOffers()
        {
            var passengers = new List<PassengerData>
            {
                _passenger,
                CreatePassenger("delivery"),
                CreatePassenger("nurse"),
                CreatePassenger("trainer"),
            };

            var offerService = new PassengerOfferService(passengers, new RandomService(42), offerCount: 3);
            var manager = new SummonManager(_runState, _config, offerService);

            Assert.AreEqual(SummonRequestResult.Success, manager.TryBeginSummon());
            var first = new List<PassengerData>(manager.CurrentOffers);
            Assert.AreEqual(3, first.Count);

            manager.CancelOffers();
            Assert.IsFalse(manager.HasActiveOffers);

            Assert.AreEqual(SummonRequestResult.Success, manager.TryBeginSummon());
            Assert.AreEqual(3, manager.CurrentOffers.Count);
            for (int i = 0; i < first.Count; i++)
            {
                Assert.AreSame(first[i], manager.CurrentOffers[i]);
            }

            for (int i = 1; i < passengers.Count; i++)
            {
                Object.DestroyImmediate(passengers[i]);
            }
        }

        private SummonManager CreateManager()
        {
            var offerService = new PassengerOfferService(
                new List<PassengerData> { _passenger },
                new RandomService(99),
                _config.OfferCount);
            return new SummonManager(_runState, _config, offerService);
        }

        private static PassengerData CreatePassenger(string id)
        {
            var data = ScriptableObject.CreateInstance<PassengerData>();
            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = id;
            so.FindProperty("sellPriceStar1").intValue = 5;
            so.FindProperty("sellPriceStar2").intValue = 12;
            so.FindProperty("sellPriceStar3").intValue = 28;
            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }
    }
}
