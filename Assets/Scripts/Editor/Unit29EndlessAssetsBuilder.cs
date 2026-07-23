using LastTrain.Data;
using LastTrain.Difficulty;
using UnityEditor;
using UnityEngine;

namespace LastTrain.EditorTools
{
    public static class Unit29EndlessAssetsBuilder
    {
        private const string DatabasePath = "Assets/Data/GameDatabase.asset";
        private const string RoutePath = "Assets/Data/Routes/EndlessRoute.asset";
        private const string ModifierFolder = "Assets/Data/Difficulties/Modifiers";

        [MenuItem("Tools/막차 생존/개발 단위 29 무한 노선 생성")]
        public static void Build()
        {
            EnsureFolder("Assets/Data/Difficulties", "Modifiers");

            DifficultyModifierData mod10 = CreateModifier(
                "endless_escalate_10",
                "심야 과밀 (10역+)",
                "10역부터 적 체력 +10%",
                DifficultyModifierKind.EscalatingEnemies,
                magnitude: 1.1f,
                stationMin: 10);
            DifficultyModifierData mod20 = CreateModifier(
                "endless_escalate_20",
                "심야 과밀 (20역+)",
                "20역부터 적 체력 +20%",
                DifficultyModifierKind.EscalatingEnemies,
                magnitude: 1.2f,
                stationMin: 20);
            DifficultyModifierData mod30 = CreateModifier(
                "endless_sell_30",
                "검표 강화 (30역+)",
                "30역부터 판매가 감소",
                DifficultyModifierKind.ReducedSellPrice,
                magnitude: 0.7f,
                stationMin: 30);
            DifficultyModifierData mod40 = CreateModifier(
                "endless_prep_40",
                "출발 독촉 (40역+)",
                "40역부터 준비 시간 단축",
                DifficultyModifierKind.ReducedPreparationTime,
                magnitude: 2f,
                stationMin: 40);

            StationData s1 = LoadStation("Assets/Data/Stations/Station_01.asset");
            StationData s2 = LoadStation("Assets/Data/Stations/Station_02.asset");
            StationData s3 = LoadStation("Assets/Data/Stations/Station_03.asset");
            StationData s4 = LoadStation("Assets/Data/Stations/Station_04.asset");
            StationData boss = LoadStation("Assets/Data/Stations/Station_05.asset");

            EndlessRouteData route = LoadOrCreate<EndlessRouteData>(RoutePath);
            route.EditorSet(
                RouteIds.Endless,
                "무한 노선",
                new[] { s1, s2, s3, s4 },
                boss,
                interval: 5,
                growth: 0.08f,
                bossBonus: 0.35f,
                new[] { mod10, mod20, mod30, mod40 });
            EditorUtility.SetDirty(route);

            GameDatabase database = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            if (database != null)
            {
                var so = new SerializedObject(database);
                so.FindProperty("endlessRoute").objectReferenceValue = route;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(database);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("완료", "EndlessRoute + Depth Modifiers 생성", "확인");
        }

        private static DifficultyModifierData CreateModifier(
            string id,
            string name,
            string desc,
            DifficultyModifierKind kind,
            float magnitude,
            int stationMin)
        {
            string path = $"{ModifierFolder}/Modifier_{id}.asset";
            DifficultyModifierData data = LoadOrCreate<DifficultyModifierData>(path);
            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = name;
            so.FindProperty("description").stringValue = desc;
            so.FindProperty("modifierKind").enumValueIndex = (int)kind;
            so.FindProperty("magnitude").floatValue = magnitude;
            so.FindProperty("stationIndexMin").intValue = stationMin;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            return data;
        }

        private static StationData LoadStation(string path)
        {
            return AssetDatabase.LoadAssetAtPath<StationData>(path);
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                return existing;
            }

            T created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
