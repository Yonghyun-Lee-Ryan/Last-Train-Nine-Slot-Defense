using System.Collections.Generic;
using LastTrain.Ability;
using LastTrain.Core;
using LastTrain.Data;
using LastTrain.Relic;
using LastTrain.Run;
using LastTrain.Shop;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class ShopServiceTests
    {
        private RunState _runState;
        private GameDatabase _database;
        private RelicManager _relicManager;
        private ShopService _shopService;
        private StationData _station;

        [SetUp]
        public void SetUp()
        {
            _runState = new RunState();
            _runState.Initialize(RunStartConfig.CreateDefault());
            _runState.Battle.StartRun();
            _database = CreateDatabase();
            _relicManager = new RelicManager(_runState, _database);
            _shopService = new ShopService(
                _runState,
                _database,
                _relicManager,
                new RandomService(12345));
            _station = CreateStation("shop_station", 8);
        }

        [TearDown]
        public void TearDown()
        {
            _runState?.Dispose();
            Object.DestroyImmediate(_database);
            Object.DestroyImmediate(_station);
        }

        [Test]
        public void GenerateOffers_IsDeterministicForSameSeed()
        {
            var serviceA = new ShopService(_runState, _database, _relicManager, new RandomService(99));
            var serviceB = new ShopService(_runState, _database, _relicManager, new RandomService(99));

            List<ShopOffer> offersA = serviceA.GenerateOffers(_station);
            List<ShopOffer> offersB = serviceB.GenerateOffers(_station);

            Assert.AreEqual(offersA.Count, offersB.Count);
            for (int i = 0; i < offersA.Count; i++)
            {
                Assert.AreEqual(offersA[i].itemType, offersB[i].itemType);
                Assert.AreEqual(offersA[i].price, offersB[i].price);
            }
        }

        [Test]
        public void Purchase_DeductsCoins_AndBlocksRepurchase()
        {
            _runState.Currency.AddCoins(200);
            Assert.IsTrue(_shopService.TryOpenShop(_station));
            Assert.AreEqual(ShopService.OfferCount, _runState.Shop.Offers.Count);

            int index = FindPurchasableOfferIndex();
            Assert.GreaterOrEqual(index, 0, "테스트 DB로 구매 가능한 상품이 없습니다.");

            int coinsBefore = _runState.Currency.CurrentCoins;
            ShopPurchaseResult first = _shopService.TryPurchase(index);
            Assert.AreEqual(ShopPurchaseResult.Success, first);
            Assert.Less(_runState.Currency.CurrentCoins, coinsBefore);

            ShopPurchaseResult second = _shopService.TryPurchase(index);
            Assert.AreEqual(ShopPurchaseResult.AlreadyPurchased, second);
        }

        [Test]
        public void TryRefreshOffersFromAd_RebuildsOffers_AndClearsPurchase()
        {
            _runState.Currency.AddCoins(200);
            Assert.IsTrue(_shopService.TryOpenShop(_station));
            int index = FindPurchasableOfferIndex();
            Assert.GreaterOrEqual(index, 0);
            Assert.AreEqual(ShopPurchaseResult.Success, _shopService.TryPurchase(index));
            Assert.IsTrue(_runState.Shop.Offers[index].purchased);

            Assert.IsTrue(_shopService.TryRefreshOffersFromAd());
            Assert.AreEqual(ShopService.OfferCount, _runState.Shop.Offers.Count);
            Assert.IsFalse(_runState.Shop.Offers[0].purchased);
            Assert.IsTrue(_runState.Shop.IsActive);
        }

        private int FindPurchasableOfferIndex()
        {
            var offers = _runState.Shop.Offers;
            for (int i = 0; i < offers.Count; i++)
            {
                ShopOffer offer = offers[i];
                if (offer == null || offer.purchased)
                {
                    continue;
                }

                // 테스트 DB에는 Relic/빈 그리드 Duplicate가 없어 실패할 수 있다.
                if (offer.itemType == ShopItemType.Relic
                    || offer.itemType == ShopItemType.DuplicatePassenger)
                {
                    continue;
                }

                return i;
            }

            return -1;
        }

        private static GameDatabase CreateDatabase()
        {
            PassengerData passenger = ScriptableObject.CreateInstance<PassengerData>();
            var passengerSo = new SerializedObject(passenger);
            passengerSo.FindProperty("id").stringValue = "passenger_office_worker";
            passengerSo.FindProperty("displayName").stringValue = "직장인";
            passengerSo.FindProperty("baseAttack").floatValue = 10f;
            passengerSo.FindProperty("attackInterval").floatValue = 1f;
            passengerSo.FindProperty("range").floatValue = 5f;
            passengerSo.ApplyModifiedPropertiesWithoutUndo();

            AbilityData ability = ScriptableObject.CreateInstance<AbilityData>();
            var abilitySo = new SerializedObject(ability);
            abilitySo.FindProperty("id").stringValue = "ability_test";
            abilitySo.FindProperty("displayName").stringValue = "테스트";
            abilitySo.FindProperty("description").stringValue = "desc";
            abilitySo.FindProperty("effectType").enumValueIndex = (int)AbilityEffectType.PassengerAttackPercent;
            abilitySo.FindProperty("effectValue").floatValue = 10f;
            abilitySo.FindProperty("allowDuplicate").boolValue = true;
            abilitySo.FindProperty("maxStack").intValue = 3;
            abilitySo.ApplyModifiedPropertiesWithoutUndo();

            var database = ScriptableObject.CreateInstance<GameDatabase>();
            var dbSo = new SerializedObject(database);
            dbSo.FindProperty("passengers").arraySize = 1;
            dbSo.FindProperty("passengers").GetArrayElementAtIndex(0).objectReferenceValue = passenger;
            dbSo.FindProperty("abilities").arraySize = 1;
            dbSo.FindProperty("abilities").GetArrayElementAtIndex(0).objectReferenceValue = ability;
            dbSo.ApplyModifiedPropertiesWithoutUndo();
            return database;
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
