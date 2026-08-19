using System.Collections.Generic;
using System.IO;
using LastTrain.Data;
using UnityEditor;
using UnityEngine;

namespace LastTrain.EditorTools
{
    /// <summary>
    /// 개발 단위 2 MVP 샘플 ScriptableObject 데이터를 자동 생성한다.
    /// README MVP 범위: 승객 4종, 적 3종, 역 5개, 능력 카드 6종.
    /// </summary>
    public static class Unit2SampleDataBuilder
    {
        private const string DatabasePath = "Assets/Data/GameDatabase.asset";

        [MenuItem("Tools/막차 생존/개발 단위 2 MVP 샘플 데이터 생성")]
        public static void BuildSampleData()
        {
            if (!EditorUtility.DisplayDialog(
                    "개발 단위 2 MVP 샘플 데이터 생성",
                    "Passenger, Enemy, Wave, Station, Ability, Synergy 에셋과 GameDatabase를 생성합니다.\n" +
                    "동일 경로 에셋이 있으면 덮어씁니다. 계속할까요?",
                    "생성",
                    "취소"))
            {
                return;
            }

            EnsureDataFolders();

            var passengers = CreateMvpPassengers();
            var enemies = CreateMvpEnemies();
            var waves = CreateMvpWaves(enemies);
            var stations = CreateMvpStations(waves);
            var abilities = CreateMvpAbilities();
            var synergies = CreateMvpSynergies();

            CreateOrUpdateGameDatabase(passengers, enemies, waves, stations, abilities, synergies);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "완료",
                $"MVP 샘플 데이터 생성 완료.\nGameDatabase: {DatabasePath}",
                "확인");

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
        }

        private static void EnsureDataFolders()
        {
            CreateFolderChain("Assets/Data/Passengers");
            CreateFolderChain("Assets/Data/Enemies");
            CreateFolderChain("Assets/Data/Stations/Waves");
            CreateFolderChain("Assets/Data/Abilities");
            CreateFolderChain("Assets/Data/Synergies");
            CreateFolderChain("Assets/Data/Relics");
        }

        private static void CreateFolderChain(string path)
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

        private static List<PassengerData> CreateMvpPassengers()
        {
            return new List<PassengerData>
            {
                CreatePassenger(
                    "Assets/Data/Passengers/Passenger_OfficeWorker.asset",
                    "passenger_office_worker",
                    "야근 직장인",
                    PassengerRole.Attack,
                    PassengerTag.OfficeWorker,
                    baseAttack: 12f,
                    attackInterval: 1.0f,
                    range: 6f,
                    TargetPriority.Nearest,
                    skillId: "skill_paper_throw"),

                CreatePassenger(
                    "Assets/Data/Passengers/Passenger_Delivery.asset",
                    "passenger_delivery",
                    "배달기사",
                    PassengerRole.Attack,
                    PassengerTag.Delivery,
                    baseAttack: 10f,
                    attackInterval: 0.85f,
                    range: 5.5f,
                    TargetPriority.Fastest,
                    skillId: "skill_low_hp_bonus"),

                CreatePassenger(
                    "Assets/Data/Passengers/Passenger_Trainer.asset",
                    "passenger_trainer",
                    "헬스 트레이너",
                    PassengerRole.Defense,
                    PassengerTag.Fitness,
                    baseAttack: 8f,
                    attackInterval: 1.2f,
                    range: 2.5f,
                    TargetPriority.Nearest,
                    skillId: "skill_knockback"),

                CreatePassenger(
                    "Assets/Data/Passengers/Passenger_Nurse.asset",
                    "passenger_nurse",
                    "간호사",
                    PassengerRole.Support,
                    PassengerTag.Medical,
                    baseAttack: 6f,
                    attackInterval: 1.1f,
                    range: 5f,
                    TargetPriority.LowestHealth,
                    skillId: "skill_train_heal"),

                CreatePassenger(
                    "Assets/Data/Passengers/Passenger_Developer.asset",
                    "passenger_developer",
                    "개발자",
                    PassengerRole.Summon,
                    PassengerTag.Tech,
                    baseAttack: 7f,
                    attackInterval: 1.0f,
                    range: 5f,
                    TargetPriority.Nearest,
                    skillId: "skill_temp_turret"),

                CreatePassenger(
                    "Assets/Data/Passengers/Passenger_Graduate.asset",
                    "passenger_graduate",
                    "대학원생",
                    PassengerRole.Special,
                    PassengerTag.Academic,
                    baseAttack: 9f,
                    attackInterval: 1.15f,
                    range: 5.5f,
                    TargetPriority.LowestHealth,
                    skillId: "skill_crit_aoe")
            };
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

            SetDefaultStarLevels(so.FindProperty("starLevels"));
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            return data;
        }

        private static void SetDefaultStarLevels(SerializedProperty starLevels)
        {
            starLevels.arraySize = 3;
            WriteStar(starLevels.GetArrayElementAtIndex(0), PassengerStarData.CreateDefault(1), "일반");
            WriteStar(starLevels.GetArrayElementAtIndex(1), PassengerStarData.CreateDefault(2), "숙련");
            WriteStar(starLevels.GetArrayElementAtIndex(2), PassengerStarData.CreateDefault(3), "전문");
        }

        private static void WriteStar(SerializedProperty element, PassengerStarData star, string nameOverride)
        {
            element.FindPropertyRelative("starLevel").intValue = star.starLevel;
            element.FindPropertyRelative("displayNameOverride").stringValue = nameOverride;
            element.FindPropertyRelative("attackMultiplier").floatValue = star.attackMultiplier;
            element.FindPropertyRelative("attackSpeedMultiplier").floatValue = star.attackSpeedMultiplier;
            element.FindPropertyRelative("rangeBonus").floatValue = star.rangeBonus;
            element.FindPropertyRelative("skillValueMultiplier").floatValue = star.skillValueMultiplier;
        }

        private static List<EnemyData> CreateMvpEnemies()
        {
            return new List<EnemyData>
            {
                CreateEnemy(
                    "Assets/Data/Enemies/Enemy_Normal.asset",
                    "enemy_normal",
                    "취객 괴물",
                    EnemyType.Normal,
                    baseHealth: 50f,
                    moveSpeed: 2f,
                    trainDamage: 5f,
                    defense: 0f,
                    coinReward: 3),

                CreateEnemy(
                    "Assets/Data/Enemies/Enemy_Fast.asset",
                    "enemy_fast",
                    "막차 질주자",
                    EnemyType.Fast,
                    baseHealth: 35f,
                    moveSpeed: 3.5f,
                    trainDamage: 5f,
                    defense: 0f,
                    coinReward: 4),

                CreateEnemy(
                    "Assets/Data/Enemies/Enemy_Tank.asset",
                    "enemy_tank",
                    "가방 방패병",
                    EnemyType.Tank,
                    baseHealth: 90f,
                    moveSpeed: 1.4f,
                    trainDamage: 8f,
                    defense: 0.2f,
                    coinReward: 5),

                CreateEnemy(
                    "Assets/Data/Enemies/Enemy_Boss_DrunkManager.asset",
                    "enemy_boss_drunk_manager",
                    "취중 차장",
                    EnemyType.Boss,
                    baseHealth: 400f,
                    moveSpeed: 1.2f,
                    trainDamage: 20f,
                    defense: 0.1f,
                    coinReward: 40,
                    abilityId: "boss_mvp")
            };
        }

        private static EnemyData CreateEnemy(
            string path,
            string id,
            string displayName,
            EnemyType enemyType,
            float baseHealth,
            float moveSpeed,
            float trainDamage,
            float defense,
            int coinReward,
            string abilityId = "")
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
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            return data;
        }

        private static List<WaveData> CreateMvpWaves(IReadOnlyList<EnemyData> enemies)
        {
            EnemyData normal = enemies[0];
            EnemyData fast = enemies[1];
            EnemyData tank = enemies[2];
            EnemyData boss = enemies.Count > 3 ? enemies[3] : tank;

            return new List<WaveData>
            {
                CreateWave("Assets/Data/Stations/Waves/Wave_01_01.asset", "wave_01_01", normal, 5, 1.0f),
                CreateWave("Assets/Data/Stations/Waves/Wave_02_01.asset", "wave_02_01", normal, 6, 0.9f),
                CreateWave("Assets/Data/Stations/Waves/Wave_02_02.asset", "wave_02_02", fast, 3, 0.8f),
                CreateWave("Assets/Data/Stations/Waves/Wave_03_01.asset", "wave_03_01", normal, 4, 0.9f),
                CreateWave("Assets/Data/Stations/Waves/Wave_03_02.asset", "wave_03_02", tank, 2, 1.2f),
                CreateWave("Assets/Data/Stations/Waves/Wave_04_01.asset", "wave_04_01", fast, 5, 0.7f),
                CreateWave("Assets/Data/Stations/Waves/Wave_05_01.asset", "wave_05_01", normal, 4, 0.8f),
                CreateWave("Assets/Data/Stations/Waves/Wave_05_02.asset", "wave_05_02", tank, 2, 1.0f),
                CreateWave("Assets/Data/Stations/Waves/Wave_05_Boss.asset", "wave_05_boss", boss, 1, 1.0f)
            };
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

        private static List<StationData> CreateMvpStations(IReadOnlyList<WaveData> waves)
        {
            return new List<StationData>
            {
                CreateStation(
                    "Assets/Data/Stations/Station_01.asset",
                    "line1_station_01",
                    "1번째 역",
                    StationType.Tutorial,
                    1,
                    1.0f,
                    new[] { waves[0] },
                    rewardCoins: 15,
                    grantsAbility: false),

                CreateStation(
                    "Assets/Data/Stations/Station_02.asset",
                    "line1_station_02",
                    "2번째 역",
                    StationType.Normal,
                    2,
                    1.15f,
                    new[] { waves[1], waves[2] },
                    rewardCoins: 18,
                    grantsAbility: true),

                CreateStation(
                    "Assets/Data/Stations/Station_03.asset",
                    "line1_station_03",
                    "3번째 역",
                    StationType.Normal,
                    3,
                    1.3f,
                    new[] { waves[3], waves[4] },
                    rewardCoins: 20,
                    grantsAbility: false),

                CreateStation(
                    "Assets/Data/Stations/Station_04.asset",
                    "line1_station_04",
                    "4번째 역",
                    StationType.Normal,
                    4,
                    1.45f,
                    new[] { waves[5] },
                    rewardCoins: 22,
                    grantsAbility: true),

                CreateStation(
                    "Assets/Data/Stations/Station_05.asset",
                    "line1_station_05",
                    "5번째 역 (중간 보스)",
                    StationType.Boss,
                    5,
                    1.7f,
                    new[] { waves[6], waves[7], waves[8] },
                    rewardCoins: 30,
                    grantsAbility: true)
            };
        }

        private static StationData CreateStation(
            string path,
            string id,
            string displayName,
            StationType stationType,
            int stationIndex,
            float difficultyMultiplier,
            WaveData[] stationWaves,
            int rewardCoins,
            bool grantsAbility)
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

            SerializedProperty wavesProp = so.FindProperty("waves");
            wavesProp.arraySize = stationWaves.Length;
            for (int i = 0; i < stationWaves.Length; i++)
            {
                wavesProp.GetArrayElementAtIndex(i).objectReferenceValue = stationWaves[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            return data;
        }

        private static List<AbilityData> CreateMvpAbilities()
        {
            return new List<AbilityData>
            {
                CreateAbility(
                    "Assets/Data/Abilities/Ability_OfficeAttack.asset",
                    "ability_office_attack",
                    "야근의 힘",
                    "직장인 공격력 +20%",
                    Rarity.Common,
                    AbilityEffectType.PassengerAttackPercent,
                    20f,
                    "passenger_office_worker"),

                CreateAbility(
                    "Assets/Data/Abilities/Ability_NurseHeal.asset",
                    "ability_nurse_heal",
                    "응급 처치",
                    "간호사 회복량 +30%",
                    Rarity.Common,
                    AbilityEffectType.NurseHealPercent,
                    30f,
                    "passenger_nurse"),

                CreateAbility(
                    "Assets/Data/Abilities/Ability_TrainMaxHp.asset",
                    "ability_train_max_hp",
                    "강화 차체",
                    "객차 최대 내구도 +20",
                    Rarity.Common,
                    AbilityEffectType.TrainMaxHpFlat,
                    20f,
                    null),

                CreateAbility(
                    "Assets/Data/Abilities/Ability_CoinBoost.asset",
                    "ability_coin_boost",
                    "추가 수입",
                    "적 처치 코인 +15%",
                    Rarity.Rare,
                    AbilityEffectType.CoinOnKillPercent,
                    15f,
                    null),

                CreateAbility(
                    "Assets/Data/Abilities/Ability_FrontRow.asset",
                    "ability_front_row",
                    "전방 배치",
                    "앞줄 승객 공격력 +15%",
                    Rarity.Rare,
                    AbilityEffectType.FrontRowAttackPercent,
                    15f,
                    null),

                CreateAbility(
                    "Assets/Data/Abilities/Ability_Diverse.asset",
                    "ability_diverse",
                    "다양한 승객",
                    "서로 다른 승객 6종 배치 시 모든 피해 +20%",
                    Rarity.Legendary,
                    AbilityEffectType.DiversePassengerDamagePercent,
                    20f,
                    null,
                    allowDuplicate: false),

                CreateAbility(
                    "Assets/Data/Abilities/Ability_AttackSpeed.asset",
                    "ability_attack_speed",
                    "빠른 손놀림",
                    "모든 승객 공격속도 +10%",
                    Rarity.Common,
                    AbilityEffectType.PassengerAttackSpeedPercent,
                    10f,
                    null),

                CreateAbility(
                    "Assets/Data/Abilities/Ability_SummonCost.asset",
                    "ability_summon_cost",
                    "할인 티켓",
                    "소환 비용 증가량 -1",
                    Rarity.Rare,
                    AbilityEffectType.SummonCostIncreaseReduction,
                    1f,
                    null),

                CreateAbility(
                    "Assets/Data/Abilities/Ability_SellBoost.asset",
                    "ability_sell_boost",
                    "중고 거래",
                    "승객 판매 가격 +20%",
                    Rarity.Rare,
                    AbilityEffectType.SellPricePercent,
                    20f,
                    null)
            };
        }

        private static AbilityData CreateAbility(
            string path,
            string id,
            string displayName,
            string description,
            Rarity rarity,
            AbilityEffectType effectType,
            float effectValue,
            string targetPassengerId,
            bool allowDuplicate = true)
        {
            var data = LoadOrCreate<AbilityData>(path);
            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("description").stringValue = description;
            so.FindProperty("rarity").enumValueIndex = (int)rarity;
            so.FindProperty("effectType").enumValueIndex = (int)effectType;
            so.FindProperty("effectValue").floatValue = effectValue;
            so.FindProperty("targetPassengerId").stringValue = targetPassengerId ?? string.Empty;
            so.FindProperty("allowDuplicate").boolValue = allowDuplicate;
            so.FindProperty("maxStack").intValue = allowDuplicate ? 99 : 1;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            return data;
        }

        private static List<SynergyData> CreateMvpSynergies()
        {
            return new List<SynergyData>
            {
                CreateSynergy(
                    "Assets/Data/Synergies/Synergy_Overtime.asset",
                    "synergy_overtime",
                    "야근조",
                    "직장인·개발자·대학원생 3명 → 공격속도 +10%",
                    PassengerTag.OfficeWorker | PassengerTag.Tech | PassengerTag.Academic,
                    requiredCount: 3,
                    uniqueCount: 0,
                    SynergyEffectType.AttackSpeedPercent,
                    10f),

                CreateSynergy(
                    "Assets/Data/Synergies/Synergy_HealthCare.asset",
                    "synergy_health_care",
                    "건강관리",
                    "간호사·헬스트레이너 2명 → 객차 회복량 +20%",
                    PassengerTag.Medical | PassengerTag.Fitness,
                    requiredCount: 2,
                    uniqueCount: 0,
                    SynergyEffectType.TrainHealPercent,
                    20f),

                CreateSynergy(
                    "Assets/Data/Synergies/Synergy_Diversity.asset",
                    "synergy_diversity",
                    "다양성",
                    "서로 다른 승객 6종 → 전체 공격력 +15%",
                    PassengerTag.None,
                    requiredCount: 0,
                    uniqueCount: 6,
                    SynergyEffectType.AllAttackPercent,
                    15f),

                // Unit 47 — 시너지 +5 (Unit2 재실행 시 카탈로그 8종 유지)
                CreateSynergy(
                    "Assets/Data/Synergies/Synergy_NightCourier.asset",
                    "synergy_night_courier",
                    "심야배송",
                    "배달기사·승무원 → 빠른 적 피해 +20%",
                    PassengerTag.Delivery | PassengerTag.Transit,
                    requiredCount: 2,
                    uniqueCount: 0,
                    SynergyEffectType.FastEnemyDamagePercent,
                    20f),
                CreateSynergy(
                    "Assets/Data/Synergies/Synergy_LastCall.asset",
                    "synergy_last_call",
                    "심야카페",
                    "바리스타·직장인 → 공격속도 +8%",
                    PassengerTag.Service | PassengerTag.OfficeWorker,
                    requiredCount: 2,
                    uniqueCount: 0,
                    SynergyEffectType.AttackSpeedPercent,
                    8f),
                CreateSynergy(
                    "Assets/Data/Synergies/Synergy_PlatformGuard.asset",
                    "synergy_platform_guard",
                    "승강장경비",
                    "경찰관·경비원 → 전체 공격력 +12%",
                    PassengerTag.LawEnforcement | PassengerTag.Security,
                    requiredCount: 2,
                    uniqueCount: 0,
                    SynergyEffectType.AllAttackPercent,
                    12f),
                CreateSynergy(
                    "Assets/Data/Synergies/Synergy_ExamRush.asset",
                    "synergy_exam_rush",
                    "통학러시",
                    "수험생·대학원생 → 치명타 확률 +10%",
                    PassengerTag.Commute | PassengerTag.Academic,
                    requiredCount: 2,
                    uniqueCount: 0,
                    SynergyEffectType.CritChancePercent,
                    10f),
                CreateSynergy(
                    "Assets/Data/Synergies/Synergy_StrayExpress.asset",
                    "synergy_stray_express",
                    "막차행운",
                    "고양이·승무원 → 치명타 확률 +8%",
                    PassengerTag.Lucky | PassengerTag.Transit,
                    requiredCount: 2,
                    uniqueCount: 0,
                    SynergyEffectType.CritChancePercent,
                    8f)
            };
        }

        private static SynergyData CreateSynergy(
            string path,
            string id,
            string displayName,
            string description,
            PassengerTag requiredTags,
            int requiredCount,
            int uniqueCount,
            SynergyEffectType effectType,
            float effectValue)
        {
            var data = LoadOrCreate<SynergyData>(path);
            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("description").stringValue = description;
            so.FindProperty("requiredTags").enumValueFlag = (int)requiredTags;
            so.FindProperty("requiredCount").intValue = requiredCount;
            so.FindProperty("requiredUniquePassengerCount").intValue = uniqueCount;
            so.FindProperty("effectType").enumValueIndex = (int)effectType;
            so.FindProperty("effectValue").floatValue = effectValue;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            return data;
        }

        private static void CreateOrUpdateGameDatabase(
            IReadOnlyList<PassengerData> passengers,
            IReadOnlyList<EnemyData> enemies,
            IReadOnlyList<WaveData> waves,
            IReadOnlyList<StationData> stations,
            IReadOnlyList<AbilityData> abilities,
            IReadOnlyList<SynergyData> synergies)
        {
            GameDatabase database = LoadOrCreate<GameDatabase>(DatabasePath);
            var so = new SerializedObject(database);
            AssignArray(so, "passengers", passengers);
            AssignArray(so, "enemies", enemies);
            AssignArray(so, "waves", waves);
            AssignArray(so, "stations", stations);
            AssignArray(so, "abilities", abilities);
            AssignArray(so, "synergies", synergies);
            AssignArray(so, "relics", new RelicData[0]);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
        }

        private static void AssignArray<T>(SerializedObject so, string propertyName, IReadOnlyList<T> items)
            where T : Object
        {
            SerializedProperty array = so.FindProperty(propertyName);
            array.arraySize = items.Count;
            for (int i = 0; i < items.Count; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }
        }

        private static T LoadOrCreate<T>(string assetPath) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            string directory = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directory))
            {
                CreateFolderChain(directory.Replace('\\', '/'));
            }

            var created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, assetPath);
            return created;
        }
    }
}
