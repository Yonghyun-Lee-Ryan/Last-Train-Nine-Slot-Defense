using LastTrain.Data;
using UnityEditor;
using UnityEngine;

namespace LastTrain.EditorTools
{
    /// <summary>Unit 50: Quick Run 5역 노선 + GameDatabase/Resources 동기화.</summary>
    public static class Unit50QuickRunAssetsBuilder
    {
        private const string DatabasePath = "Assets/Data/GameDatabase.asset";
        private const string RoutePath = "Assets/Data/Routes/Route_Quick.asset";

        [MenuItem("Tools/막차 생존/개발 단위 50 Quick Run 노선 생성")]
        public static void BuildFromMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "개발 단위 50 Quick Run",
                    "5역 Quick Run 노선을 생성하고 GameDatabase에 병합합니다.",
                    "생성",
                    "취소"))
            {
                return;
            }

            BuildInternal(showDialog: true);
        }

        /// <summary>Batchmode: -executeMethod LastTrain.EditorTools.Unit50QuickRunAssetsBuilder.BuildBatch</summary>
        public static void BuildBatch()
        {
            try
            {
                BuildInternal(showDialog: false);
                Debug.Log("[Unit50QuickRunAssetsBuilder] OK");
                EditorApplication.Exit(0);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[Unit50QuickRunAssetsBuilder] " + ex);
                EditorApplication.Exit(1);
            }
        }

        private static void BuildInternal(bool showDialog)
        {
            StationData[] stations =
            {
                LoadStation("Assets/Data/Stations/Station_01.asset"),
                LoadStation("Assets/Data/Stations/Station_02.asset"),
                LoadStation("Assets/Data/Stations/Station_03.asset"),
                LoadStation("Assets/Data/Stations/Station_04.asset"),
                LoadStation("Assets/Data/Stations/Station_05.asset"),
            };

            RouteData route = AssetDatabase.LoadAssetAtPath<RouteData>(RoutePath);
            if (route == null)
            {
                route = ScriptableObject.CreateInstance<RouteData>();
                AssetDatabase.CreateAsset(route, RoutePath);
            }

            var routeSo = new SerializedObject(route);
            routeSo.FindProperty("id").stringValue = RouteIds.Quick;
            routeSo.FindProperty("displayName").stringValue = "퀵 런";
            routeSo.FindProperty("rewardMultiplier").floatValue = 0.7f;
            SerializedProperty stationsProp = routeSo.FindProperty("stationsInOrder");
            stationsProp.arraySize = stations.Length;
            for (int i = 0; i < stations.Length; i++)
            {
                stationsProp.GetArrayElementAtIndex(i).objectReferenceValue = stations[i];
            }

            routeSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(route);

            GameDatabase database = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            if (database == null)
            {
                throw new System.InvalidOperationException("GameDatabase.asset 없음");
            }

            var dbSo = new SerializedObject(database);
            SerializedProperty routes = dbSo.FindProperty("routes");
            bool found = false;
            for (int i = 0; i < routes.arraySize; i++)
            {
                var existing = routes.GetArrayElementAtIndex(i).objectReferenceValue as RouteData;
                if (existing != null && existing.Id == RouteIds.Quick)
                {
                    routes.GetArrayElementAtIndex(i).objectReferenceValue = route;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                int index = routes.arraySize;
                routes.arraySize++;
                routes.GetArrayElementAtIndex(index).objectReferenceValue = route;
            }

            dbSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);

            ReleaseAssetsBuilder.EnsureReleaseAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (showDialog)
            {
                EditorUtility.DisplayDialog("완료", "Quick Run 5역 노선 병합 완료", "확인");
            }
        }

        private static StationData LoadStation(string path)
        {
            StationData station = AssetDatabase.LoadAssetAtPath<StationData>(path);
            if (station == null)
            {
                throw new System.InvalidOperationException("Station 없음: " + path);
            }

            return station;
        }
    }
}
