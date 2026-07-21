using System.Collections.Generic;
using LastTrain.Ability;
using LastTrain.Core;
using LastTrain.Data;
using LastTrain.Passenger;
using LastTrain.Run;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class AbilityManagerTests
    {
        private RunState _runState;
        private readonly List<Object> _created = new();

        [SetUp]
        public void SetUp()
        {
            _runState = new RunState();
            _runState.Initialize(RunStartConfig.CreateDefault());
            _runState.Battle.StartRun();
        }

        [TearDown]
        public void TearDown()
        {
            _runState?.Dispose();
            for (int i = 0; i < _created.Count; i++)
            {
                Object.DestroyImmediate(_created[i]);
            }

            _created.Clear();
        }

        [Test]
        public void SelectOffer_AppliesTrainMaxHpAndRecordsHistory()
        {
            AbilityData train = CreateAbility(
                "train_hp",
                AbilityEffectType.TrainMaxHpFlat,
                20f,
                Rarity.Common);
            var offerService = new AbilityOfferService(new List<AbilityData> { train }, new RandomService(1), 3);
            bool finished = false;
            var manager = new AbilityManager(
                _runState,
                offerService,
                _runState.BaseTrainMaxHp,
                onRewardFinished: () => finished = true);

            Assert.AreEqual(AbilityOfferResult.Success, manager.TryBeginRewardSelection());
            Assert.AreEqual(AbilitySelectResult.Success, manager.TrySelectOffer(0));
            Assert.IsTrue(finished);
            Assert.AreEqual(120, _runState.Train.MaxHp);
            Assert.AreEqual(1, _runState.History.AbilityCardsSelected);
            Assert.IsFalse(_runState.Abilities.HasActiveOffers);
        }

        [Test]
        public void SelectOffer_AppliesPassengerAttackBuff()
        {
            PassengerData passengerData = CreatePassenger("passenger_office_worker");
            AbilityData attack = CreateAbility(
                "office_atk",
                AbilityEffectType.PassengerAttackPercent,
                20f,
                Rarity.Common,
                "passenger_office_worker");

            var passenger = PassengerRuntime.Create(passengerData, 1);
            Assert.IsTrue(_runState.TryPlacePassenger(0, passenger));
            float baseAttack = passenger.GetEffectiveAttack();

            var offerService = new AbilityOfferService(new List<AbilityData> { attack }, new RandomService(2), 3);
            var manager = new AbilityManager(_runState, offerService, _runState.BaseTrainMaxHp);
            manager.TryBeginRewardSelection();
            manager.TrySelectOffer(0);

            Assert.AreEqual(baseAttack * 1.2f, passenger.GetEffectiveAttack(), 0.01f);
        }

        [Test]
        public void AdReroll_UsesInjectedCallback()
        {
            AbilityData a = CreateAbility("a1", AbilityEffectType.CoinOnKillPercent, 10f, Rarity.Rare);
            AbilityData b = CreateAbility("a2", AbilityEffectType.CoinOnKillPercent, 10f, Rarity.Rare);
            var offerService = new AbilityOfferService(new List<AbilityData> { a, b }, new RandomService(3), 3);
            int adCalls = 0;
            var manager = new AbilityManager(
                _runState,
                offerService,
                _runState.BaseTrainMaxHp,
                tryShowRewardedAd: () =>
                {
                    adCalls++;
                    return true;
                });

            manager.TryBeginRewardSelection();
            Assert.AreEqual(AbilityRerollResult.Success, manager.TryRerollWithAd());
            Assert.AreEqual(1, adCalls);
            Assert.AreEqual(1, _runState.Abilities.AdRerollsUsed);
            Assert.AreEqual(1, manager.RemainingAdRerolls);
        }

        private AbilityData CreateAbility(
            string id,
            AbilityEffectType type,
            float value,
            Rarity rarity,
            string targetPassengerId = null)
        {
            var data = ScriptableObject.CreateInstance<AbilityData>();
            _created.Add(data);
            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = id;
            so.FindProperty("rarity").enumValueIndex = (int)rarity;
            so.FindProperty("effectType").enumValueIndex = (int)type;
            so.FindProperty("effectValue").floatValue = value;
            so.FindProperty("targetPassengerId").stringValue = targetPassengerId ?? string.Empty;
            so.FindProperty("allowDuplicate").boolValue = true;
            so.FindProperty("maxStack").intValue = 99;
            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }

        private PassengerData CreatePassenger(string id)
        {
            var data = ScriptableObject.CreateInstance<PassengerData>();
            _created.Add(data);
            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = id;
            // star stats: use defaults from asset if any; set minimal via serialized if present
            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }
    }
}
