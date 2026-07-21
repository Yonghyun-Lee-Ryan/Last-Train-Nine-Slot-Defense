using LastTrain.Data;
using LastTrain.Passenger;
using LastTrain.Run;
using LastTrain.Synergy;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class SynergyManagerTests
    {
        private RunState _runState;
        private PassengerData _nurse;
        private PassengerData _trainer;
        private SynergyData _healthCare;
        private SynergyManager _manager;

        [SetUp]
        public void SetUp()
        {
            _runState = new RunState();
            _runState.Initialize(RunStartConfig.CreateDefault());
            _nurse = CreatePassenger("nurse", PassengerTag.Medical);
            _trainer = CreatePassenger("trainer", PassengerTag.Fitness);
            _healthCare = CreateSynergy();
            _manager = new SynergyManager(_runState, new[] { _healthCare });
        }

        [TearDown]
        public void TearDown()
        {
            _runState?.Dispose();
            Object.DestroyImmediate(_nurse);
            Object.DestroyImmediate(_trainer);
            Object.DestroyImmediate(_healthCare);
        }

        [Test]
        public void Recalculate_AppliesAttackBuff_AndRemovesWhenBroken()
        {
            Assert.IsTrue(_runState.TryPlacePassenger(0, PassengerRuntime.Create(_nurse)));
            Assert.IsTrue(_runState.TryPlacePassenger(1, PassengerRuntime.Create(_trainer)));
            _manager.Recalculate();

            Assert.AreEqual(1, _manager.ActiveSynergies.Count);
            // HealthCare is TrainHealPercent only — attack buff stays 0
            Assert.AreEqual(0f, _runState.GetPassengerAtSlot(0).GetEffectiveAttack() - 10f, 0.01f);
            Assert.AreEqual(20f, _manager.Modifiers.TrainHealPercent, 0.001f);

            Assert.IsTrue(_runState.TryConsumePassenger(1, out _));
            _manager.Recalculate();
            Assert.AreEqual(0, _manager.ActiveSynergies.Count);
            Assert.AreEqual(0f, _manager.Modifiers.TrainHealPercent, 0.001f);
        }

        [Test]
        public void Applier_DoesNotStackSynergyBuffsOnRepeatedRefresh()
        {
            var attackSynergy = CreateSynergy(
                "synergy_atk",
                PassengerTag.Medical,
                requiredCount: 1,
                SynergyEffectType.AllAttackPercent,
                15f);

            _runState.Synergies.SetCatalog(new[] { attackSynergy });
            Assert.IsTrue(_runState.TryPlacePassenger(0, PassengerRuntime.Create(_nurse)));

            SynergyEffectApplier.Refresh(_runState);
            SynergyEffectApplier.Refresh(_runState);
            SynergyEffectApplier.Refresh(_runState);

            PassengerRuntime passenger = _runState.GetPassengerAtSlot(0);
            Assert.AreEqual(11.5f, passenger.GetEffectiveAttack(), 0.001f);

            int synergyBuffCount = 0;
            for (int i = 0; i < passenger.Buffs.Count; i++)
            {
                if (passenger.Buffs[i].BuffId.StartsWith(SynergyEffectCalculator.SynergyBuffIdPrefix))
                {
                    synergyBuffCount++;
                }
            }

            Assert.AreEqual(1, synergyBuffCount);
            Object.DestroyImmediate(attackSynergy);
        }

        [Test]
        public void ActiveSynergiesChanged_FiresOnRecalculate()
        {
            int fireCount = 0;
            _manager.ActiveSynergiesChanged += _ => fireCount++;

            Assert.IsTrue(_runState.TryPlacePassenger(0, PassengerRuntime.Create(_nurse)));
            Assert.IsTrue(_runState.TryPlacePassenger(1, PassengerRuntime.Create(_trainer)));
            _manager.Recalculate();

            Assert.GreaterOrEqual(fireCount, 1);
            Assert.AreEqual(1, _manager.ActiveSynergies.Count);
        }

        private static PassengerData CreatePassenger(string id, PassengerTag tag)
        {
            var data = ScriptableObject.CreateInstance<PassengerData>();
            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = id;
            so.FindProperty("tags").enumValueFlag = (int)tag;
            so.FindProperty("baseAttack").floatValue = 10f;
            so.FindProperty("attackInterval").floatValue = 1f;
            so.FindProperty("range").floatValue = 5f;
            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }

        private static SynergyData CreateSynergy()
        {
            return CreateSynergy(
                "synergy_health_care",
                PassengerTag.Medical | PassengerTag.Fitness,
                2,
                SynergyEffectType.TrainHealPercent,
                20f);
        }

        private static SynergyData CreateSynergy(
            string id,
            PassengerTag tags,
            int requiredCount,
            SynergyEffectType effectType,
            float effectValue)
        {
            var data = ScriptableObject.CreateInstance<SynergyData>();
            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = id;
            so.FindProperty("requiredTags").enumValueFlag = (int)tags;
            so.FindProperty("requiredCount").intValue = requiredCount;
            so.FindProperty("requiredUniquePassengerCount").intValue = 0;
            so.FindProperty("effectType").enumValueIndex = (int)effectType;
            so.FindProperty("effectValue").floatValue = effectValue;
            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }
    }
}
