using LastTrain.Difficulty;
using LastTrain.Data;
using NUnit.Framework;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class DifficultyCalculatorTests
    {
        [Test]
        public void IdentityRuntime_KeepsBaseValues()
        {
            Assert.AreEqual(10, DifficultyCalculator.ApplySummonCost(10, DifficultyRuntime.Identity));
            Assert.AreEqual(50, DifficultyCalculator.ApplyStationReward(50, DifficultyRuntime.Identity));
            Assert.AreEqual(3, DifficultyCalculator.ScaleEnemyCount(3, DifficultyRuntime.Identity));
        }

        [Test]
        public void FromData_AppliesMultipliersOnce()
        {
            var data = ScriptableObject.CreateInstance<DifficultyData>();
            SetField(data, "id", "test");
            SetField(data, "enemyHealthMultiplier", 1.5f);
            SetField(data, "enemyCountMultiplier", 2f);
            SetField(data, "spawnIntervalMultiplier", 0.5f);
            SetField(data, "summonCostMultiplier", 1.2f);
            SetField(data, "rewardMultiplier", 1.25f);

            DifficultyRuntime runtime = DifficultyCalculator.CreateRuntime(data);

            Assert.AreEqual(1.5f, DifficultyCalculator.CombineLineDifficulty(1f, runtime));
            Assert.AreEqual(6, DifficultyCalculator.ScaleEnemyCount(3, runtime));
            Assert.AreEqual(12, DifficultyCalculator.ApplySummonCost(10, runtime));
            Assert.AreEqual(62, DifficultyCalculator.ApplyMetaReward(50, runtime));
        }

        [Test]
        public void ResolveSavedDifficultyId_DefaultsToNormal()
        {
            Assert.AreEqual(DifficultyIds.Normal, DifficultyService.ResolveSavedDifficultyId(null));
            Assert.AreEqual(DifficultyIds.Normal, DifficultyService.ResolveSavedDifficultyId(string.Empty));
        }

        [Test]
        public void CreateRuntime_UnknownId_FallsBackToIdentityValues()
        {
            DifficultyRuntime runtime = DifficultyService.CreateRuntime("missing", database: null);
            Assert.AreEqual(DifficultyIds.Normal, runtime.Id);
            Assert.AreEqual(1f, runtime.EnemyHealthMultiplier);
        }

        private static void SetField(Object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field?.SetValue(target, value);
        }
    }
}
