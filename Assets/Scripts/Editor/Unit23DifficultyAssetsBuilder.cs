using LastTrain.Data;
using LastTrain.Difficulty;
using UnityEditor;
using UnityEngine;

namespace LastTrain.EditorTools
{
    public static class Unit23DifficultyAssetsBuilder
    {
        private const string DifficultyFolder = "Assets/Data/Difficulties";
        private const string NormalPath = DifficultyFolder + "/Difficulty_Normal.asset";
        private const string GameDatabasePath = "Assets/Data/GameDatabase.asset";

        [MenuItem("Tools/막차 생존/개발 단위 23 Difficulty 데이터 생성")]
        public static void BuildDifficultyAssets()
        {
            EnsureFolder("Assets/Data", "Difficulties");

            DifficultyData normal = LoadOrCreate(NormalPath, DifficultyIds.Normal, "일반 막차");
            AssignNormalDefaults(normal);

            GameDatabase database = AssetDatabase.LoadAssetAtPath<GameDatabase>(GameDatabasePath);
            if (database != null)
            {
                SerializedObject so = new SerializedObject(database);
                SerializedProperty prop = so.FindProperty("difficulties");
                prop.arraySize = 1;
                prop.GetArrayElementAtIndex(0).objectReferenceValue = normal;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Unit 23", "Difficulty_Normal 에셋을 생성하고 GameDatabase에 등록했습니다.", "확인");
            }
        }

        private static void AssignNormalDefaults(DifficultyData data)
        {
            SerializedObject so = new SerializedObject(data);
            so.FindProperty("id").stringValue = DifficultyIds.Normal;
            so.FindProperty("displayName").stringValue = "일반 막차";
            so.FindProperty("sortOrder").intValue = 0;
            so.FindProperty("enemyHealthMultiplier").floatValue = 1f;
            so.FindProperty("enemyMoveSpeedMultiplier").floatValue = 1f;
            so.FindProperty("enemyTrainDamageMultiplier").floatValue = 1f;
            so.FindProperty("enemyCountMultiplier").floatValue = 1f;
            so.FindProperty("spawnIntervalMultiplier").floatValue = 1f;
            so.FindProperty("eliteSpawnRate").floatValue = 0f;
            so.FindProperty("bossHealthMultiplier").floatValue = 1f;
            so.FindProperty("summonCostMultiplier").floatValue = 1f;
            so.FindProperty("shopPriceMultiplier").floatValue = 1f;
            so.FindProperty("rewardMultiplier").floatValue = 1f;
            so.FindProperty("preparationTime").floatValue = 5f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static DifficultyData LoadOrCreate(string path, string id, string displayName)
        {
            DifficultyData existing = AssetDatabase.LoadAssetAtPath<DifficultyData>(path);
            if (existing != null)
            {
                return existing;
            }

            var created = ScriptableObject.CreateInstance<DifficultyData>();
            AssetDatabase.CreateAsset(created, path);
            return created;
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
