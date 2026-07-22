using LastTrain.Difficulty;
using LastTrain.Data;
using UnityEditor;
using UnityEngine;

namespace LastTrain.EditorTools
{
    public static class Unit24DifficultyAssetsBuilder
    {
        private const string DifficultyFolder = "Assets/Data/Difficulties";
        private const string ModifierFolder = "Assets/Data/Difficulties/Modifiers";
        private const string GameDatabasePath = "Assets/Data/GameDatabase.asset";

        [MenuItem("Tools/막차 생존/개발 단위 24 Difficulty 4단계 생성")]
        public static void BuildAllDifficultyAssets()
        {
            EnsureFolder("Assets/Data", "Difficulties");
            EnsureFolder(DifficultyFolder, "Modifiers");

            DifficultyData normal = CreateOrUpdate(
                DifficultyFolder + "/Difficulty_Normal.asset",
                DifficultyIds.Normal,
                "일반 막차",
                sortOrder: 0,
                enemyHealth: 1f,
                enemySpeed: 1f,
                trainDamage: 1f,
                enemyCount: 1f,
                spawnInterval: 1f,
                eliteRate: 0f,
                bossHealth: 1f,
                bossAbilities: 0,
                summonCost: 1f,
                shopPrice: 1f,
                reward: 1f,
                prepTime: 5f,
                unlockAlways: true);

            DifficultyData express = CreateOrUpdate(
                DifficultyFolder + "/Difficulty_Express.asset",
                DifficultyIds.Express,
                "급행 막차",
                sortOrder: 1,
                enemyHealth: 1.25f,
                enemySpeed: 1.05f,
                trainDamage: 1.1f,
                enemyCount: 1f,
                spawnInterval: 0.92f,
                eliteRate: 0.1f,
                bossHealth: 1f,
                bossAbilities: 0,
                summonCost: 1f,
                shopPrice: 1f,
                reward: 1.25f,
                prepTime: 5f,
                unlockBossOn: DifficultyIds.Normal);

            DifficultyData midnight = CreateOrUpdate(
                DifficultyFolder + "/Difficulty_MidnightExpress.asset",
                DifficultyIds.MidnightExpress,
                "심야 특급",
                sortOrder: 2,
                enemyHealth: 1.55f,
                enemySpeed: 1.1f,
                trainDamage: 1.2f,
                enemyCount: 1.15f,
                spawnInterval: 1f,
                eliteRate: 0.2f,
                bossHealth: 1f,
                bossAbilities: 1,
                summonCost: 1f,
                shopPrice: 1f,
                reward: 1.6f,
                prepTime: 5f,
                unlockBossOn: DifficultyIds.Express,
                unlockPassengers: 4);

            DifficultyData hell = CreateOrUpdate(
                DifficultyFolder + "/Difficulty_NonstopHell.asset",
                DifficultyIds.NonstopHell,
                "무정차 지옥선",
                sortOrder: 3,
                enemyHealth: 2f,
                enemySpeed: 1.15f,
                trainDamage: 1.35f,
                enemyCount: 1.25f,
                spawnInterval: 0.82f,
                eliteRate: 0.3f,
                bossHealth: 1f,
                bossAbilities: 0,
                summonCost: 1f,
                shopPrice: 1f,
                reward: 2.1f,
                prepTime: 3f,
                unlockBossOn: DifficultyIds.MidnightExpress,
                unlockAccountLevel: 10);

            RegisterDatabase(normal, express, midnight, hell);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Unit 24", "4단계 난이도 에셋을 생성하고 GameDatabase에 등록했습니다.", "확인");
            }
        }

        private static DifficultyData CreateOrUpdate(
            string path,
            string id,
            string displayName,
            int sortOrder,
            float enemyHealth,
            float enemySpeed,
            float trainDamage,
            float enemyCount,
            float spawnInterval,
            float eliteRate,
            float bossHealth,
            int bossAbilities,
            float summonCost,
            float shopPrice,
            float reward,
            float prepTime,
            bool unlockAlways = false,
            string unlockBossOn = null,
            int unlockPassengers = 0,
            int unlockAccountLevel = 0)
        {
            DifficultyData data = AssetDatabase.LoadAssetAtPath<DifficultyData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<DifficultyData>();
                AssetDatabase.CreateAsset(data, path);
            }

            SerializedObject so = new SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("sortOrder").intValue = sortOrder;
            so.FindProperty("enemyHealthMultiplier").floatValue = enemyHealth;
            so.FindProperty("enemyMoveSpeedMultiplier").floatValue = enemySpeed;
            so.FindProperty("enemyTrainDamageMultiplier").floatValue = trainDamage;
            so.FindProperty("enemyCountMultiplier").floatValue = enemyCount;
            so.FindProperty("spawnIntervalMultiplier").floatValue = spawnInterval;
            so.FindProperty("eliteSpawnRate").floatValue = eliteRate;
            so.FindProperty("bossHealthMultiplier").floatValue = bossHealth;
            so.FindProperty("bossAbilityCount").intValue = bossAbilities;
            so.FindProperty("summonCostMultiplier").floatValue = summonCost;
            so.FindProperty("shopPriceMultiplier").floatValue = shopPrice;
            so.FindProperty("rewardMultiplier").floatValue = reward;
            so.FindProperty("preparationTime").floatValue = prepTime;

            SerializedProperty reqs = so.FindProperty("unlockCondition").FindPropertyRelative("requirements");
            reqs.arraySize = 0;
            if (!unlockAlways)
            {
                int count = 0;
                if (!string.IsNullOrWhiteSpace(unlockBossOn))
                {
                    count++;
                }

                if (unlockPassengers > 0)
                {
                    count++;
                }

                if (unlockAccountLevel > 0)
                {
                    count++;
                }

                reqs.arraySize = count;
                int index = 0;
                if (!string.IsNullOrWhiteSpace(unlockBossOn))
                {
                    SetRequirement(reqs.GetArrayElementAtIndex(index++), DifficultyUnlockType.DefeatFinalBossOnDifficulty, unlockBossOn);
                }

                if (unlockPassengers > 0)
                {
                    SerializedProperty passengerReq = reqs.GetArrayElementAtIndex(index++);
                    passengerReq.FindPropertyRelative("unlockType").enumValueIndex = (int)DifficultyUnlockType.UnlockedPassengerCount;
                    passengerReq.FindPropertyRelative("requiredUnlockedPassengerCount").intValue = unlockPassengers;
                }

                if (unlockAccountLevel > 0)
                {
                    SerializedProperty levelReq = reqs.GetArrayElementAtIndex(index);
                    levelReq.FindPropertyRelative("unlockType").enumValueIndex = (int)DifficultyUnlockType.AccountLevel;
                    levelReq.FindPropertyRelative("requiredAccountLevel").intValue = unlockAccountLevel;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }

        private static void SetRequirement(SerializedProperty req, DifficultyUnlockType type, string difficultyId)
        {
            req.FindPropertyRelative("unlockType").enumValueIndex = (int)type;
            req.FindPropertyRelative("requiredDifficultyId").stringValue = difficultyId;
        }

        private static void RegisterDatabase(params DifficultyData[] difficulties)
        {
            GameDatabase database = AssetDatabase.LoadAssetAtPath<GameDatabase>(GameDatabasePath);
            if (database == null)
            {
                return;
            }

            SerializedObject so = new SerializedObject(database);
            SerializedProperty prop = so.FindProperty("difficulties");
            prop.arraySize = difficulties.Length;
            for (int i = 0; i < difficulties.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = difficulties[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
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
