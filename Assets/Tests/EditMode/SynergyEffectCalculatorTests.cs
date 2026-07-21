using LastTrain.Data;
using LastTrain.Passenger;
using LastTrain.Run;
using LastTrain.Synergy;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class SynergyEffectCalculatorTests
    {
        private RunState _runState;
        private PassengerData _office;
        private PassengerData _developer;
        private PassengerData _graduate;
        private PassengerData _nurse;
        private PassengerData _trainer;
        private PassengerData _delivery;
        private SynergyData _overtime;
        private SynergyData _healthCare;
        private SynergyData _diversity;

        [SetUp]
        public void SetUp()
        {
            _runState = new RunState();
            _runState.Initialize(RunStartConfig.CreateDefault());

            _office = CreatePassenger("office", PassengerTag.OfficeWorker);
            _developer = CreatePassenger("developer", PassengerTag.Tech);
            _graduate = CreatePassenger("graduate", PassengerTag.Academic);
            _nurse = CreatePassenger("nurse", PassengerTag.Medical);
            _trainer = CreatePassenger("trainer", PassengerTag.Fitness);
            _delivery = CreatePassenger("delivery", PassengerTag.Delivery);

            _overtime = CreateSynergy(
                "synergy_overtime",
                PassengerTag.OfficeWorker | PassengerTag.Tech | PassengerTag.Academic,
                requiredCount: 3,
                unique: 0,
                SynergyEffectType.AttackSpeedPercent,
                10f);

            _healthCare = CreateSynergy(
                "synergy_health_care",
                PassengerTag.Medical | PassengerTag.Fitness,
                requiredCount: 2,
                unique: 0,
                SynergyEffectType.TrainHealPercent,
                20f);

            _diversity = CreateSynergy(
                "synergy_diversity",
                PassengerTag.None,
                requiredCount: 0,
                unique: 6,
                SynergyEffectType.AllAttackPercent,
                15f);
        }

        [TearDown]
        public void TearDown()
        {
            _runState?.Dispose();
            Object.DestroyImmediate(_office);
            Object.DestroyImmediate(_developer);
            Object.DestroyImmediate(_graduate);
            Object.DestroyImmediate(_nurse);
            Object.DestroyImmediate(_trainer);
            Object.DestroyImmediate(_delivery);
            Object.DestroyImmediate(_overtime);
            Object.DestroyImmediate(_healthCare);
            Object.DestroyImmediate(_diversity);
        }

        [Test]
        public void Overtime_RequiresAllThreeTags()
        {
            Place(0, _office);
            Place(1, _developer);
            Assert.IsFalse(SynergyEffectCalculator.IsActive(_overtime, _runState));

            Place(2, _graduate);
            Assert.IsTrue(SynergyEffectCalculator.IsActive(_overtime, _runState));

            SynergyEvaluation eval = SynergyEffectCalculator.Evaluate(new[] { _overtime }, _runState);
            Assert.AreEqual(1, eval.Active.Count);
            Assert.AreEqual(10f, eval.Modifiers.GlobalAttackSpeedPercent, 0.001f);
        }

        [Test]
        public void HealthCare_ActivatesWithMedicalAndFitness()
        {
            Place(0, _nurse);
            Place(1, _trainer);
            Assert.IsTrue(SynergyEffectCalculator.IsActive(_healthCare, _runState));

            SynergyEvaluation eval = SynergyEffectCalculator.Evaluate(new[] { _healthCare }, _runState);
            Assert.AreEqual(20f, eval.Modifiers.TrainHealPercent, 0.001f);
        }

        [Test]
        public void Diversity_RequiresSixUniqueTypes()
        {
            Place(0, _office);
            Place(1, _developer);
            Place(2, _graduate);
            Place(3, _nurse);
            Place(4, _trainer);
            Assert.IsFalse(SynergyEffectCalculator.IsActive(_diversity, _runState));

            Place(5, _delivery);
            Assert.IsTrue(SynergyEffectCalculator.IsActive(_diversity, _runState));

            SynergyEvaluation eval = SynergyEffectCalculator.Evaluate(new[] { _diversity }, _runState);
            Assert.AreEqual(15f, eval.Modifiers.GlobalAttackPercent, 0.001f);
        }

        [Test]
        public void Evaluate_DoesNotDuplicateSameSynergyId()
        {
            Place(0, _nurse);
            Place(1, _trainer);
            SynergyEvaluation eval = SynergyEffectCalculator.Evaluate(
                new[] { _healthCare, _healthCare },
                _runState);

            Assert.AreEqual(1, eval.Active.Count);
            Assert.AreEqual(20f, eval.Modifiers.TrainHealPercent, 0.001f);
        }

        private void Place(int slot, PassengerData data)
        {
            Assert.IsTrue(_runState.TryPlacePassenger(slot, PassengerRuntime.Create(data)));
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

        private static SynergyData CreateSynergy(
            string id,
            PassengerTag tags,
            int requiredCount,
            int unique,
            SynergyEffectType effectType,
            float effectValue)
        {
            var data = ScriptableObject.CreateInstance<SynergyData>();
            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = id;
            so.FindProperty("requiredTags").enumValueFlag = (int)tags;
            so.FindProperty("requiredCount").intValue = requiredCount;
            so.FindProperty("requiredUniquePassengerCount").intValue = unique;
            so.FindProperty("effectType").enumValueIndex = (int)effectType;
            so.FindProperty("effectValue").floatValue = effectValue;
            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }
    }
}
