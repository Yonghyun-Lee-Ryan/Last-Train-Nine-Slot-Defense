using LastTrain.Data;
using LastTrain.Economy;
using NUnit.Framework;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class SummonCostCalculatorTests
    {
        private SummonEconomyConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<SummonEconomyConfig>();
            var so = new UnityEditor.SerializedObject(_config);
            so.FindProperty("baseSummonCost").intValue = 10;
            so.FindProperty("summonCostIncrease").intValue = 2;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void CalculateCost_IncreasesWithPaidSummonCount()
        {
            Assert.AreEqual(10, SummonCostCalculator.CalculateCost(_config, 0));
            Assert.AreEqual(12, SummonCostCalculator.CalculateCost(_config, 1));
            Assert.AreEqual(18, SummonCostCalculator.CalculateCost(_config, 4));
        }
    }
}
