using System.Collections.Generic;
using LastTrain.Data;
using UnityEditor;
using UnityEngine;

namespace LastTrain.EditorTools
{
    public static class Unit25RouteAssetsBuilder
    {
        private const string StationFolder = "Assets/Data/Stations";
        private const string WaveFolder = "Assets/Data/Stations/Waves";
        private const string RouteFolder = "Assets/Data/Routes";
        private const string EnemyFolder = "Assets/Data/Enemies";
        private const string GameDatabasePath = "Assets/Data/GameDatabase.asset";

        [MenuItem("Tools/막차 생존/개발 단위 25 Route 10역 노선 생성")]
        public static void BuildRouteAssets()
        {
            EnsureFolder("Assets/Data", "Routes");
            EnsureFolder(StationFolder, "Waves");

            EnemyData normal = LoadEnemy("Assets/Data/Enemies/Enemy_Normal.asset");
            EnemyData fast = LoadEnemy("Assets/Data/Enemies/Enemy_Fast.asset");
            EnemyData tank = LoadEnemy("Assets/Data/Enemies/Enemy_Tank.asset");
            EnemyData midBoss = LoadEnemy("Assets/Data/Enemies/Enemy_Boss_DrunkManager.asset");
            EnemyData finalBoss = CreateOrLoadFinalBoss();

            WaveData wave0601 = CreateWave($"{WaveFolder}/Wave_06_01.asset", "wave_06_01", normal, 7, 0.85f);
            WaveData wave0602 = CreateWave($"{WaveFolder}/Wave_06_02.asset", "wave_06_02", fast, 4, 0.75f);
            WaveData wave0701 = CreateWave($"{WaveFolder}/Wave_07_01.asset", "wave_07_01", tank, 3, 1.0f);
            WaveData wave0702 = CreateWave($"{WaveFolder}/Wave_07_02.asset", "wave_07_02", fast, 5, 0.7f);
            WaveData wave0901 = CreateWave($"{WaveFolder}/Wave_09_01.asset", "wave_09_01", normal, 6, 0.8f);
            WaveData wave0902 = CreateWave($"{WaveFolder}/Wave_09_02.asset", "wave_09_02", tank, 3, 0.9f);
            WaveData wave1001 = CreateWave($"{WaveFolder}/Wave_10_01.asset", "wave_10_01", normal, 5, 0.75f);
            WaveData wave1002 = CreateWave($"{WaveFolder}/Wave_10_02.asset", "wave_10_02", tank, 3, 0.85f);
            WaveData wave10Boss = CreateWave($"{WaveFolder}/Wave_10_Boss.asset", "wave_10_boss", finalBoss, 1, 1.0f);

            StationData station01 = UpdateStation(
                $"{StationFolder}/Station_01.asset",
                "line1_station_01",
                "1번째 역",
                StationType.Tutorial,
                1,
                1.0f,
                LoadWaves("wave_01_01"),
                15,
                false,
                string.Empty);

            StationData station02 = UpdateStation(
                $"{StationFolder}/Station_02.asset",
                "line1_station_02",
                "2번째 역",
                StationType.Normal,
                2,
                1.15f,
                LoadWaves("wave_02_01", "wave_02_02"),
                18,
                true,
                string.Empty);

            StationData station03 = UpdateStation(
                $"{StationFolder}/Station_03.asset",
                "line1_station_03",
                "3번째 역",
                StationType.Normal,
                3,
                1.3f,
                LoadWaves("wave_03_01", "wave_03_02"),
                20,
                false,
                string.Empty);

            StationData station04 = UpdateStation(
                $"{StationFolder}/Station_04.asset",
                "line1_station_04",
                "4번째 역 (이벤트)",
                StationType.Event,
                4,
                1.0f,
                System.Array.Empty<WaveData>(),
                22,
                false,
                string.Empty);

            StationData station05 = UpdateStation(
                $"{StationFolder}/Station_05.asset",
                "line1_station_05",
                "5번째 역 (중간 보스)",
                StationType.Boss,
                5,
                1.7f,
                LoadWaves("wave_05_01", "wave_05_02", "wave_05_boss"),
                30,
                true,
                "취중 차장이 보호막과 함께 등장합니다.");

            StationData station06 = UpdateStation(
                $"{StationFolder}/Station_06.asset",
                "line1_station_06",
                "6번째 역",
                StationType.Normal,
                6,
                1.85f,
                new[] { wave0601, wave0602 },
                24,
                false,
                string.Empty);

            StationData station07 = UpdateStation(
                $"{StationFolder}/Station_07.asset",
                "line1_station_07",
                "7번째 역 (정예)",
                StationType.Elite,
                7,
                2.0f,
                new[] { wave0701, wave0702 },
                28,
                true,
                string.Empty);

            StationData station08 = UpdateStation(
                $"{StationFolder}/Station_08.asset",
                "line1_station_08",
                "8번째 역 (상점)",
                StationType.Shop,
                8,
                1.0f,
                System.Array.Empty<WaveData>(),
                18,
                false,
                string.Empty);

            StationData station09 = UpdateStation(
                $"{StationFolder}/Station_09.asset",
                "line1_station_09",
                "9번째 역",
                StationType.Normal,
                9,
                2.15f,
                new[] { wave0901, wave0902 },
                26,
                false,
                string.Empty);

            StationData station10 = UpdateStation(
                $"{StationFolder}/Station_10.asset",
                "line1_station_10",
                "10번째 역 (종착 보스)",
                StationType.Boss,
                10,
                2.4f,
                new[] { wave1001, wave1002, wave10Boss },
                40,
                true,
                "종착역 기관사가 강력한 연속 공격을 사용합니다.");

            StationData[] routeStations =
            {
                station01, station02, station03, station04, station05,
                station06, station07, station08, station09, station10,
            };

            RouteData route = CreateOrUpdateRoute($"{RouteFolder}/Route_Line1.asset", routeStations);
            RegisterDatabase(routeStations, route);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Unit25] 10역 노선 에셋 생성 완료.");
        }

        private static EnemyData CreateOrLoadFinalBoss()
        {
            const string path = EnemyFolder + "/Enemy_Boss_FinalConductor.asset";
            var data = LoadOrCreate<EnemyData>(path);
            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = "enemy_boss_final_conductor";
            so.FindProperty("displayName").stringValue = "종착역 기관사";
            so.FindProperty("enemyType").enumValueIndex = (int)EnemyType.Boss;
            so.FindProperty("baseHealth").floatValue = 650f;
            so.FindProperty("moveSpeed").floatValue = 1.1f;
            so.FindProperty("trainDamage").floatValue = 28f;
            so.FindProperty("defense").floatValue = 0.15f;
            so.FindProperty("coinReward").intValue = 60;
            so.FindProperty("abilityId").stringValue = "boss_mvp";
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            return data;
        }

        private static RouteData CreateOrUpdateRoute(string path, StationData[] stations)
        {
            var route = LoadOrCreate<RouteData>(path);
            var so = new SerializedObject(route);
            so.FindProperty("id").stringValue = RouteIds.Default;
            so.FindProperty("displayName").stringValue = "기본 노선";
            SerializedProperty stationsProp = so.FindProperty("stationsInOrder");
            stationsProp.arraySize = stations.Length;
            for (int i = 0; i < stations.Length; i++)
            {
                stationsProp.GetArrayElementAtIndex(i).objectReferenceValue = stations[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(route);
            return route;
        }

        private static void RegisterDatabase(StationData[] stations, RouteData route)
        {
            GameDatabase database = AssetDatabase.LoadAssetAtPath<GameDatabase>(GameDatabasePath);
            if (database == null)
            {
                Debug.LogWarning("[Unit25] GameDatabase를 찾지 못했습니다.");
                return;
            }

            SerializedObject so = new SerializedObject(database);
            SerializedProperty stationProp = so.FindProperty("stations");
            stationProp.arraySize = stations.Length;
            for (int i = 0; i < stations.Length; i++)
            {
                stationProp.GetArrayElementAtIndex(i).objectReferenceValue = stations[i];
            }

            SerializedProperty routeProp = so.FindProperty("routes");
            routeProp.arraySize = 1;
            routeProp.GetArrayElementAtIndex(0).objectReferenceValue = route;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
        }

        private static StationData UpdateStation(
            string path,
            string id,
            string displayName,
            StationType stationType,
            int stationIndex,
            float difficultyMultiplier,
            WaveData[] stationWaves,
            int rewardCoins,
            bool grantsAbility,
            string bossHint)
        {
            var data = LoadOrCreate<StationData>(path);
            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("stationType").enumValueIndex = (int)stationType;
            so.FindProperty("stationIndex").intValue = stationIndex;
            so.FindProperty("difficultyMultiplier").floatValue = difficultyMultiplier;
            so.FindProperty("rewardCoins").intValue = rewardCoins;
            so.FindProperty("grantsAbilityChoice").boolValue = grantsAbility;
            so.FindProperty("bossPatternHint").stringValue = bossHint ?? string.Empty;

            SerializedProperty wavesProp = so.FindProperty("waves");
            wavesProp.arraySize = stationWaves?.Length ?? 0;
            for (int i = 0; i < wavesProp.arraySize; i++)
            {
                wavesProp.GetArrayElementAtIndex(i).objectReferenceValue = stationWaves[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            return data;
        }

        private static WaveData CreateWave(
            string path,
            string id,
            EnemyData enemy,
            int count,
            float spawnInterval)
        {
            var data = LoadOrCreate<WaveData>(path);
            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            SerializedProperty spawns = so.FindProperty("spawns");
            spawns.arraySize = 1;
            SerializedProperty spawn = spawns.GetArrayElementAtIndex(0);
            spawn.FindPropertyRelative("enemy").objectReferenceValue = enemy;
            spawn.FindPropertyRelative("count").intValue = count;
            spawn.FindPropertyRelative("spawnInterval").floatValue = spawnInterval;
            spawn.FindPropertyRelative("spawnDelay").floatValue = 0f;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            return data;
        }

        private static WaveData[] LoadWaves(params string[] waveIds)
        {
            var waves = new List<WaveData>(waveIds.Length);
            string[] guids = AssetDatabase.FindAssets("t:WaveData", new[] { WaveFolder });
            for (int i = 0; i < waveIds.Length; i++)
            {
                string targetId = waveIds[i];
                for (int g = 0; g < guids.Length; g++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[g]);
                    var wave = AssetDatabase.LoadAssetAtPath<WaveData>(path);
                    if (wave != null && wave.Id == targetId)
                    {
                        waves.Add(wave);
                        break;
                    }
                }
            }

            return waves.ToArray();
        }

        private static EnemyData LoadEnemy(string path)
        {
            return AssetDatabase.LoadAssetAtPath<EnemyData>(path);
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            string folder = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(folder) && !AssetDatabase.IsValidFolder(folder))
            {
                string parent = System.IO.Path.GetDirectoryName(folder)?.Replace('\\', '/');
                string child = System.IO.Path.GetFileName(folder);
                if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(child))
                {
                    AssetDatabase.CreateFolder(parent, child);
                }
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
