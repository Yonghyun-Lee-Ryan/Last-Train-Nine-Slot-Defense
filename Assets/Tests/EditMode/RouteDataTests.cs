using LastTrain.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class RouteDataTests
    {
        [Test]
        public void TryGetStationByIndex_ReturnsOrderedStations()
        {
            RouteData route = CreateRoute(
                CreateStation("route_s1", 1),
                CreateStation("route_s2", 2),
                CreateStation("route_s3", 3));

            Assert.IsTrue(route.TryGetStationByIndex(2, out StationData station));
            Assert.AreEqual("route_s2", station.Id);
            Assert.AreEqual(3, route.StationCount);
            Assert.AreEqual("route_s1", route.GetFirstStation().Id);
        }

        [Test]
        public void GameDatabase_TryGetStationByRouteIndex_UsesRoute()
        {
            var database = ScriptableObject.CreateInstance<GameDatabase>();
            RouteData route = CreateRoute(
                CreateStation("db_s5", 5),
                CreateStation("db_s10", 10));

            var dbSo = new SerializedObject(database);
            SerializedProperty routesProp = dbSo.FindProperty("routes");
            routesProp.arraySize = 1;
            routesProp.GetArrayElementAtIndex(0).objectReferenceValue = route;
            dbSo.ApplyModifiedPropertiesWithoutUndo();

            Assert.IsTrue(database.TryGetStationByRouteIndex(RouteIds.Default, 10, out StationData station));
            Assert.AreEqual("db_s10", station.Id);
            Assert.AreEqual(2, database.GetRouteStationCount(RouteIds.Default));
        }

        private static RouteData CreateRoute(params StationData[] stations)
        {
            var route = ScriptableObject.CreateInstance<RouteData>();
            var so = new SerializedObject(route);
            so.FindProperty("id").stringValue = RouteIds.Default;
            SerializedProperty stationsProp = so.FindProperty("stationsInOrder");
            stationsProp.arraySize = stations.Length;
            for (int i = 0; i < stations.Length; i++)
            {
                stationsProp.GetArrayElementAtIndex(i).objectReferenceValue = stations[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return route;
        }

        private static StationData CreateStation(string id, int stationIndex)
        {
            var station = ScriptableObject.CreateInstance<StationData>();
            var so = new SerializedObject(station);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("stationIndex").intValue = stationIndex;
            so.ApplyModifiedPropertiesWithoutUndo();
            return station;
        }
    }
}
