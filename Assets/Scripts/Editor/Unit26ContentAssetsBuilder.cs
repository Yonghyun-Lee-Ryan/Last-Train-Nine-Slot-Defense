using System.Collections.Generic;
using LastTrain.Data;
using LastTrain.Enemy;
using LastTrain.Passenger.Skills;
using UnityEditor;
using UnityEngine;

namespace LastTrain.EditorTools
{
    /// <summary>개발 단위 26 알파 콘텐츠(승객 8·적 6·보스 2) 에셋 생성.</summary>
    public static class Unit26ContentAssetsBuilder
    {
        private const string DatabasePath = "Assets/Data/GameDatabase.asset";
        private const string PassengerFolder = "Assets/Data/Passengers";
        private const string EnemyFolder = "Assets/Data/Enemies";
        private const string WaveFolder = "Assets/Data/Stations/Waves";

        [MenuItem("Tools/막차 생존/개발 단위 26 알파 콘텐츠 생성")]
        public static void BuildAlphaContent()
        {
            if (!EditorUtility.DisplayDialog(
                    "개발 단위 26 알파 콘텐츠 생성",
                    "승객 8종, 적 6종, 보스 2종 데이터와 웨이브를 생성·갱신합니다.\n" +
                    "기존 GameDatabase 배열에 병합합니다. 계속할까요?",
                    "생성",
                    "취소"))
            {
                return;
            }

            EnsureFolder("Assets/Data", "Passengers");
            EnsureFolder("Assets/Data", "Enemies");

            List<PassengerData> passengers = CreatePassengers();
            List<EnemyData> enemies = CreateEnemies();
            UpdateWaves(enemies);
            MergeIntoGameDatabase(passengers, enemies);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "완료",
                $"알파 콘텐츠 생성 완료.\n승객 {passengers.Count}종, 적 {enemies.Count}종",
                "확인");
        }

        private static List<PassengerData> CreatePassengers()
        {
            return new List<PassengerData>
            {
                LoadPassenger("Passenger_OfficeWorker.asset", "skill_paper_throw"),
                LoadPassenger("Passenger_Delivery.asset", "skill_low_hp_bonus"),
                LoadPassenger("Passenger_Trainer.asset", "skill_knockback"),
                LoadPassenger("Passenger_Nurse.asset", "skill_train_heal"),
                LoadPassenger("Passenger_Developer.asset", "skill_temp_turret"),
                LoadPassenger("Passenger_Graduate.asset", "skill_crit_aoe"),

                CreatePassenger(
                    $"{PassengerFolder}/Passenger_Police.asset",
                    "passenger_police",
                    "경찰관",
                    PassengerRole.Attack,
                    PassengerTag.LawEnforcement,
                    baseAttack: 11f,
                    attackInterval: 1.05f,
                    range: 5.5f,
                    TargetPriority.BossFirst,
                    PassengerSkillIds.BossInterrupt),

                CreatePassenger(
                    $"{PassengerFolder}/Passenger_Cat.asset",
                    "passenger_cat",
                    "고양이",
                    PassengerRole.Special,
                    PassengerTag.Lucky,
                    baseAttack: 6f,
                    attackInterval: 1.25f,
                    range: 4.5f,
                    TargetPriority.Nearest,
                    PassengerSkillIds.LuckyCrit),
            };
        }

        private static PassengerData LoadPassenger(string fileName, string skillId)
        {
            string path = $"{PassengerFolder}/{fileName}";
            var data = AssetDatabase.LoadAssetAtPath<PassengerData>(path);
            if (data == null)
            {
                Debug.LogWarning($"[Unit26] 승객 에셋 없음: {path}");
                return null;
            }

            var so = new SerializedObject(data);
            so.FindProperty("skillId").stringValue = skillId;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            return data;
        }

        private static List<EnemyData> CreateEnemies()
        {
            EnemyData normal = LoadOrUpdateEnemy(
                $"{EnemyFolder}/Enemy_Normal.asset",
                "enemy_normal",
                "취객 괴물",
                EnemyType.Normal,
                50f,
                2f,
                5f,
                0f,
                3,
                string.Empty,
                string.Empty,
                BossPhaseThresholds.Create(0f, 0f));

            EnemyData fast = LoadOrUpdateEnemy(
                $"{EnemyFolder}/Enemy_Fast.asset",
                "enemy_fast",
                "막차 질주자",
                EnemyType.Fast,
                35f,
                3.5f,
                5f,
                0f,
                4,
                string.Empty,
                string.Empty,
                BossPhaseThresholds.Create(0f, 0f));

            EnemyData tank = LoadOrUpdateEnemy(
                $"{EnemyFolder}/Enemy_Tank.asset",
                "enemy_tank",
                "가방 방패병",
                EnemyType.Tank,
                90f,
                1.4f,
                8f,
                0.2f,
                5,
                string.Empty,
                string.Empty,
                BossPhaseThresholds.Create(0f, 0f));

            EnemyData splitMinion = LoadOrUpdateEnemy(
                $"{EnemyFolder}/Enemy_SplitMinion.asset",
                "enemy_split_minion",
                "분열 잔해",
                EnemyType.Fast,
                18f,
                3f,
                3f,
                0f,
                2,
                string.Empty,
                string.Empty,
                BossPhaseThresholds.Create(0f, 0f));

            EnemyData split = LoadOrUpdateEnemy(
                $"{EnemyFolder}/Enemy_Split.asset",
                "enemy_split_passenger",
                "분열 승객",
                EnemyType.Normal,
                45f,
                2.2f,
                5f,
                0f,
                4,
                EnemyAbilityIds.SplitOnDeath,
                "enemy_split_minion",
                BossPhaseThresholds.Create(0f, 0f));

            EnemyData aura = LoadOrUpdateEnemy(
                $"{EnemyFolder}/Enemy_AuraWatcher.asset",
                "enemy_aura_watcher",
                "무임승차 감시자",
                EnemyType.Normal,
                55f,
                1.8f,
                5f,
                0.05f,
                5,
                EnemyAbilityIds.NearbyBuff,
                string.Empty,
                BossPhaseThresholds.Create(0f, 0f));

            EnemyData seat = LoadOrUpdateEnemy(
                $"{EnemyFolder}/Enemy_SeatBlocker.asset",
                "enemy_seat_blocker",
                "좌석 점거자",
                EnemyType.Tank,
                70f,
                1.5f,
                6f,
                0.1f,
                5,
                EnemyAbilityIds.SeatBlock,
                string.Empty,
                BossPhaseThresholds.Create(0f, 0f));

            EnemyData drunkBoss = LoadOrUpdateEnemy(
                $"{EnemyFolder}/Enemy_Boss_DrunkManager.asset",
                "enemy_boss_drunk_manager",
                "만취한 부장",
                EnemyType.Boss,
                420f,
                1.2f,
                20f,
                0.1f,
                45,
                EnemyAbilityIds.BossDrunkManager,
                string.Empty,
                BossPhaseThresholds.DefaultMidBoss);

            EnemyData finalBoss = LoadOrUpdateEnemy(
                $"{EnemyFolder}/Enemy_Boss_FinalConductor.asset",
                "enemy_boss_final_conductor",
                "기관사 없는 열차",
                EnemyType.Boss,
                650f,
                1.1f,
                28f,
                0.15f,
                60,
                EnemyAbilityIds.BossFinalConductor,
                string.Empty,
                BossPhaseThresholds.DefaultFinalBoss);

            return new List<EnemyData>
            {
                normal,
                fast,
                tank,
                splitMinion,
                split,
                aura,
                seat,
                drunkBoss,
                finalBoss,
            };
        }

        private static void UpdateWaves(IReadOnlyList<EnemyData> enemies)
        {
            EnemyData normal = FindEnemy(enemies, "enemy_normal");
            EnemyData fast = FindEnemy(enemies, "enemy_fast");
            EnemyData tank = FindEnemy(enemies, "enemy_tank");
            EnemyData split = FindEnemy(enemies, "enemy_split_passenger");
            EnemyData aura = FindEnemy(enemies, "enemy_aura_watcher");
            EnemyData seat = FindEnemy(enemies, "enemy_seat_blocker");

            CreateOrUpdateWave($"{WaveFolder}/Wave_06_03.asset", "wave_06_03", split, 3, 1.0f);
            CreateOrUpdateWave($"{WaveFolder}/Wave_07_03.asset", "wave_07_03", aura, 2, 1.1f);
            CreateOrUpdateWave($"{WaveFolder}/Wave_09_03.asset", "wave_09_03", seat, 2, 1.0f);
            CreateOrUpdateWave($"{WaveFolder}/Wave_06_01.asset", "wave_06_01", normal, 7, 0.85f);
            CreateOrUpdateWave($"{WaveFolder}/Wave_06_02.asset", "wave_06_02", fast, 4, 0.75f);
            CreateOrUpdateWave($"{WaveFolder}/Wave_07_01.asset", "wave_07_01", tank, 3, 1.0f);
            CreateOrUpdateWave($"{WaveFolder}/Wave_07_02.asset", "wave_07_02", fast, 5, 0.7f);
            CreateOrUpdateWave($"{WaveFolder}/Wave_09_01.asset", "wave_09_01", normal, 6, 0.8f);
            CreateOrUpdateWave($"{WaveFolder}/Wave_09_02.asset", "wave_09_02", tank, 3, 0.9f);
        }

        private static void MergeIntoGameDatabase(
            IReadOnlyList<PassengerData> newPassengers,
            IReadOnlyList<EnemyData> newEnemies)
        {
            GameDatabase database = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            if (database == null)
            {
                Debug.LogError("[Unit26] GameDatabase를 찾지 못했습니다.");
                return;
            }

            var so = new SerializedObject(database);
            MergeArray(so, "passengers", newPassengers);
            MergeArray(so, "enemies", newEnemies);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
        }

        private static void MergeArray<T>(SerializedObject so, string propertyName, IReadOnlyList<T> items)
            where T : ScriptableObject, IDataWithId
        {
            SerializedProperty array = so.FindProperty(propertyName);
            var map = new Dictionary<string, T>(System.StringComparer.Ordinal);
            for (int i = 0; i < array.arraySize; i++)
            {
                T existing = array.GetArrayElementAtIndex(i).objectReferenceValue as T;
                if (existing != null && !string.IsNullOrWhiteSpace(existing.Id))
                {
                    map[existing.Id] = existing;
                }
            }

            for (int i = 0; i < items.Count; i++)
            {
                T item = items[i];
                if (item != null && !string.IsNullOrWhiteSpace(item.Id))
                {
                    map[item.Id] = item;
                }
            }

            var merged = new List<T>(map.Values);
            merged.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            array.arraySize = merged.Count;
            for (int i = 0; i < merged.Count; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = merged[i];
            }
        }

        private static EnemyData FindEnemy(IReadOnlyList<EnemyData> enemies, string id)
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyData enemy = enemies[i];
                if (enemy != null && enemy.Id == id)
                {
                    return enemy;
                }
            }

            return null;
        }

        private static PassengerData CreatePassenger(
            string path,
            string id,
            string displayName,
            PassengerRole role,
            PassengerTag tags,
            float baseAttack,
            float attackInterval,
            float range,
            TargetPriority targetPriority,
            string skillId)
        {
            var data = LoadOrCreate<PassengerData>(path);
            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("role").enumValueIndex = (int)role;
            so.FindProperty("tags").enumValueFlag = (int)tags;
            so.FindProperty("baseAttack").floatValue = baseAttack;
            so.FindProperty("attackInterval").floatValue = attackInterval;
            so.FindProperty("range").floatValue = range;
            so.FindProperty("targetPriority").enumValueIndex = (int)targetPriority;
            so.FindProperty("skillId").stringValue = skillId;
            so.FindProperty("sellPriceStar1").intValue = 5;
            so.FindProperty("sellPriceStar2").intValue = 12;
            so.FindProperty("sellPriceStar3").intValue = 28;
            so.FindProperty("startsUnlocked").boolValue = false;
            SetDefaultStarLevels(so.FindProperty("starLevels"));
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            return data;
        }

        private static EnemyData LoadOrUpdateEnemy(
            string path,
            string id,
            string displayName,
            EnemyType enemyType,
            float baseHealth,
            float moveSpeed,
            float trainDamage,
            float defense,
            int coinReward,
            string abilityId,
            string splitMinionId,
            BossPhaseThresholds phaseThresholds)
        {
            var data = LoadOrCreate<EnemyData>(path);
            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("enemyType").enumValueIndex = (int)enemyType;
            so.FindProperty("baseHealth").floatValue = baseHealth;
            so.FindProperty("moveSpeed").floatValue = moveSpeed;
            so.FindProperty("trainDamage").floatValue = trainDamage;
            so.FindProperty("defense").floatValue = defense;
            so.FindProperty("coinReward").intValue = coinReward;
            so.FindProperty("abilityId").stringValue = abilityId ?? string.Empty;
            so.FindProperty("splitMinionId").stringValue = splitMinionId ?? string.Empty;

            SerializedProperty thresholds = so.FindProperty("bossPhaseThresholds");
            thresholds.FindPropertyRelative("doorOpenHealthRatio").floatValue = phaseThresholds.DoorOpenHealthRatio;
            thresholds.FindPropertyRelative("enrageHealthRatio").floatValue = phaseThresholds.EnrageHealthRatio;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            return data;
        }

        private static void CreateOrUpdateWave(
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
        }

        private static void SetDefaultStarLevels(SerializedProperty starLevels)
        {
            starLevels.arraySize = 3;
            WriteStar(starLevels.GetArrayElementAtIndex(0), 1, 1f, 1f, 1f);
            WriteStar(starLevels.GetArrayElementAtIndex(1), 2, 2.2f, 1.05f, 1.2f);
            WriteStar(starLevels.GetArrayElementAtIndex(2), 3, 4.8f, 1.1f, 1.5f);
        }

        private static void WriteStar(
            SerializedProperty element,
            int star,
            float attackMul,
            float speedMul,
            float skillMul)
        {
            element.FindPropertyRelative("starLevel").intValue = star;
            element.FindPropertyRelative("attackMultiplier").floatValue = attackMul;
            element.FindPropertyRelative("attackSpeedMultiplier").floatValue = speedMul;
            element.FindPropertyRelative("rangeBonus").floatValue = 0f;
            element.FindPropertyRelative("skillValueMultiplier").floatValue = skillMul;
        }

        private static T LoadOrCreate<T>(string assetPath) where T : ScriptableObject
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            string directory = System.IO.Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(directory))
            {
                EnsureFolderChain(directory);
            }

            T created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, assetPath);
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

        private static void EnsureFolderChain(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
