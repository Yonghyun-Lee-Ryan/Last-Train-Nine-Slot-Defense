using LastTrain.Data;
using LastTrain.Performance;
using NUnit.Framework;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class EndlessPerformanceTests
    {
        [Test]
        public void EndlessRoute_50Stations_WithPrune_CacheDoesNotGrowUnbounded()
        {
            EndlessRouteData route = ScriptableObject.CreateInstance<EndlessRouteData>();
            StationData template = ScriptableObject.CreateInstance<StationData>();
            var so = new UnityEditor.SerializedObject(template);
            so.FindProperty("id").stringValue = "endless_perf_template";
            so.FindProperty("displayName").stringValue = "Perf";
            so.FindProperty("stationIndex").intValue = 1;
            so.FindProperty("stationType").enumValueIndex = (int)StationType.Normal;
            so.FindProperty("difficultyMultiplier").floatValue = 1f;
            so.ApplyModifiedPropertiesWithoutUndo();

#if UNITY_EDITOR
            route.EditorSet(
                RouteIds.Endless,
                "Perf Endless",
                new[] { template },
                null,
                interval: 5,
                growth: 0.05f,
                bossBonus: 0.2f,
                modifiers: null);
#endif

            const int stations = 55;
            int maxCache = 0;
            for (int i = 1; i <= stations; i++)
            {
                Assert.IsTrue(route.TryGetStationByIndex(i, out StationData station), $"station {i}");
                Assert.IsNotNull(station);
                route.PruneRuntimeCache(i, keepWindow: 8);
                maxCache = Mathf.Max(maxCache, route.RuntimeCacheCount);
            }

            Assert.LessOrEqual(maxCache, 12, "프룬 후에도 캐시가 과도하게 커지면 안 됩니다.");
            Assert.LessOrEqual(route.RuntimeCacheCount, 12);

            route.ClearRuntimeCache();
            Assert.AreEqual(0, route.RuntimeCacheCount);

            Object.DestroyImmediate(template);
            Object.DestroyImmediate(route);
        }

        [Test]
        public void PerformanceChecklist_BuildReport_ContainsUpdateList()
        {
            string report = PerformanceChecklist.BuildReport();
            StringAssert.Contains("BattleManager.Update", report);
            StringAssert.Contains("Profiler", report);
            Assert.GreaterOrEqual(PerformanceChecklist.UpdateMonoBehaviours.Length, 5);
        }
    }
}
