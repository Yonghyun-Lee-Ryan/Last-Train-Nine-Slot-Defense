using LastTrain.Battle;
using LastTrain.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class PassengerRangeDesignTests
    {
        [TestCase("Passenger_Trainer", 2.5f)]
        [TestCase("Passenger_Cat", 4.5f)]
        [TestCase("Passenger_Nurse", 5f)]
        [TestCase("Passenger_Developer", 5f)]
        [TestCase("Passenger_Delivery", 5.5f)]
        [TestCase("Passenger_Graduate", 5.5f)]
        [TestCase("Passenger_Police", 5.5f)]
        [TestCase("Passenger_OfficeWorker", 6f)]
        public void PassengerBaseRange_MatchesDesignRoles(string assetName, float expectedRange)
        {
            string path = $"Assets/Data/Passengers/{assetName}.asset";
            var data = AssetDatabase.LoadAssetAtPath<PassengerData>(path);

            Assert.IsNotNull(data, path);
            Assert.AreEqual(expectedRange, data.Range, 0.001f, assetName);
        }

        [Test]
        public void OfficeWorker_IsLongestBaseRange()
        {
            string[] paths = AssetDatabase.FindAssets("t:PassengerData", new[] { "Assets/Data/Passengers" });
            float maxRange = 0f;
            string maxId = null;
            for (int i = 0; i < paths.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(paths[i]);
                var data = AssetDatabase.LoadAssetAtPath<PassengerData>(assetPath);
                Assert.IsNotNull(data, assetPath);
                if (data.Range >= maxRange)
                {
                    maxRange = data.Range;
                    maxId = data.Id;
                }
            }

            Assert.AreEqual("passenger_office_worker", maxId);
            Assert.AreEqual(6f, maxRange, 0.001f);
            Assert.GreaterOrEqual(
                BattleConstants.ToWorldRange(maxRange),
                Vector2.Distance(new Vector2(0f, -68f), BattleConstants.SpawnAnchoredPosition) - 40f);
        }
    }
}
