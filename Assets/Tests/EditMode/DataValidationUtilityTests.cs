using System.Collections.Generic;
using LastTrain.Data;
using NUnit.Framework;

namespace LastTrain.Tests.EditMode
{
    public class DataValidationUtilityTests
    {
        [Test]
        public void FindDuplicateIds_ReturnsDuplicates()
        {
            var ids = new List<string> { "a", "b", "a", "c", "b" };

            List<string> duplicates = DataValidationUtility.FindDuplicateIds(ids);

            Assert.AreEqual(2, duplicates.Count);
            CollectionAssert.Contains(duplicates, "a");
            CollectionAssert.Contains(duplicates, "b");
        }

        [Test]
        public void FindDuplicateIds_IgnoresEmptyIds()
        {
            var ids = new List<string> { "", "  ", "valid", "valid" };

            List<string> duplicates = DataValidationUtility.FindDuplicateIds(ids);

            Assert.AreEqual(1, duplicates.Count);
            Assert.AreEqual("valid", duplicates[0]);
        }

        [Test]
        public void CalculateAttack_AppliesMultiplier()
        {
            float result = DataValidationUtility.CalculateAttack(10f, 2.2f);
            Assert.AreEqual(22f, result, 0.0001f);
        }

        [Test]
        public void CalculateAttackInterval_DividesBySpeedMultiplier()
        {
            float result = DataValidationUtility.CalculateAttackInterval(1f, 1.1f);
            Assert.AreEqual(1f / 1.1f, result, 0.0001f);
        }

        [Test]
        public void CalculateEnemyHealth_AppliesDifficultyMultipliers()
        {
            float result = DataValidationUtility.CalculateEnemyHealth(50f, 1.3f, 1f);
            Assert.AreEqual(65f, result, 0.0001f);
        }

        [Test]
        public void IsValidId_RejectsWhitespace()
        {
            Assert.IsFalse(DataValidationUtility.IsValidId(null));
            Assert.IsFalse(DataValidationUtility.IsValidId(""));
            Assert.IsFalse(DataValidationUtility.IsValidId("   "));
            Assert.IsTrue(DataValidationUtility.IsValidId("passenger_office_worker"));
        }
    }
}
