using System.Collections.Generic;
using LastTrain.Core;
using LastTrain.Data;
using LastTrain.Passenger;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class PassengerOfferServiceTests
    {
        private PassengerData _a;
        private PassengerData _b;
        private PassengerData _c;

        [SetUp]
        public void SetUp()
        {
            _a = CreatePassenger("a");
            _b = CreatePassenger("b");
            _c = CreatePassenger("c");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_a);
            Object.DestroyImmediate(_b);
            Object.DestroyImmediate(_c);
        }

        [Test]
        public void GenerateOffers_SameSeed_ProducesSameSequence()
        {
            var pool = new List<PassengerData> { _a, _b, _c };

            var randomA = new RandomService(12345);
            var serviceA = new PassengerOfferService(pool, randomA, offerCount: 3);
            List<PassengerData> first = serviceA.GenerateOffers();

            var randomB = new RandomService(12345);
            var serviceB = new PassengerOfferService(pool, randomB, offerCount: 3);
            List<PassengerData> second = serviceB.GenerateOffers();

            Assert.AreEqual(3, first.Count);
            Assert.AreEqual(3, second.Count);
            Assert.AreEqual(first[0].Id, second[0].Id);
            Assert.AreEqual(first[1].Id, second[1].Id);
            Assert.AreEqual(first[2].Id, second[2].Id);
        }

        [Test]
        public void GenerateOffers_OnlyUsesUnlockedPool()
        {
            var pool = new List<PassengerData> { _a };
            var service = new PassengerOfferService(pool, new RandomService(1), offerCount: 3);

            List<PassengerData> offers = service.GenerateOffers();

            Assert.AreEqual(3, offers.Count);
            Assert.AreEqual("a", offers[0].Id);
            Assert.AreEqual("a", offers[1].Id);
            Assert.AreEqual("a", offers[2].Id);
        }

        private static PassengerData CreatePassenger(string id)
        {
            var data = ScriptableObject.CreateInstance<PassengerData>();
            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = id;
            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }
    }
}
