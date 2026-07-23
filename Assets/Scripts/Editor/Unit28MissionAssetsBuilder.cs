using System.Collections.Generic;
using LastTrain.Data;
using LastTrain.Mission;
using UnityEditor;
using UnityEngine;

namespace LastTrain.EditorTools
{
    public static class Unit28MissionAssetsBuilder
    {
        private const string DatabasePath = "Assets/Data/GameDatabase.asset";
        private const string MissionFolder = "Assets/Data/Missions";

        [MenuItem("Tools/막차 생존/개발 단위 28 미션 데이터 생성")]
        public static void BuildMissions()
        {
            EnsureFolder("Assets/Data", "Missions");

            var missions = new List<MissionData>
            {
                Create(
                    "mission_daily_merge_5",
                    "합성 연습",
                    "승객을 5회 합성한다.",
                    MissionPeriod.Daily,
                    new MissionCondition(MissionConditionType.MergeCount, 5),
                    tickets: 15,
                    xp: 25),
                Create(
                    "mission_daily_star3",
                    "3성 승객",
                    "아무 승객이나 3성에 도달한다.",
                    MissionPeriod.Daily,
                    new MissionCondition(MissionConditionType.ReachPassengerStar, 3),
                    tickets: 20,
                    xp: 30),
                Create(
                    "mission_daily_hp50_station5",
                    "튼튼한 객차",
                    "객차 내구도 50 이상으로 5역을 도달한다.",
                    MissionPeriod.Daily,
                    new MissionCondition(MissionConditionType.ReachStationWithMinHp, 5, targetParam: 50),
                    tickets: 20,
                    xp: 30),
                Create(
                    "mission_daily_boss_damage_200",
                    "보스 타격",
                    "보스에게 총 200 피해를 입힌다.",
                    MissionPeriod.Daily,
                    new MissionCondition(MissionConditionType.DealBossDamage, 200),
                    tickets: 15,
                    xp: 25),
                Create(
                    "mission_daily_distinct_6",
                    "다양한 승객",
                    "서로 다른 승객 6종을 배치한다.",
                    MissionPeriod.Daily,
                    new MissionCondition(MissionConditionType.DistinctPassengersPlaced, 6),
                    tickets: 15,
                    xp: 25),
                Create(
                    "mission_daily_no_ads_3",
                    "무광고 주행",
                    "광고 없이 3역에 도달한다.",
                    MissionPeriod.Daily,
                    new MissionCondition(MissionConditionType.ReachStationWithoutAds, 3),
                    tickets: 25,
                    xp: 35),
                Create(
                    "mission_daily_shop_2",
                    "상점 손님",
                    "상점에서 상품을 2개 구매한다.",
                    MissionPeriod.Daily,
                    new MissionCondition(MissionConditionType.ShopPurchaseCount, 2),
                    tickets: 10,
                    xp: 20),
                Create(
                    "mission_weekly_clear_5",
                    "주간 노선 완주",
                    "기본 노선을 5회 완료한다.",
                    MissionPeriod.Weekly,
                    new MissionCondition(MissionConditionType.ClearRouteCount, 5),
                    tickets: 80,
                    xp: 100),
                Create(
                    "mission_weekly_elite_100",
                    "정예 사냥",
                    "정예 적을 100마리 처치한다.",
                    MissionPeriod.Weekly,
                    new MissionCondition(MissionConditionType.EliteKillCount, 100),
                    tickets: 60,
                    xp: 80),
                Create(
                    "mission_weekly_rare_ability_10",
                    "희귀 능력",
                    "희귀 이상 능력 카드를 10회 선택한다.",
                    MissionPeriod.Weekly,
                    new MissionCondition(MissionConditionType.RareOrHigherAbilitySelect, 10),
                    tickets: 50,
                    xp: 70),
                Create(
                    "mission_weekly_summon_50",
                    "대량 소환",
                    "승객을 50회 소환한다.",
                    MissionPeriod.Weekly,
                    new MissionCondition(MissionConditionType.SummonCount, 50),
                    tickets: 50,
                    xp: 70),
                Create(
                    "mission_weekly_express_3",
                    "급행 도전",
                    "난이도 급행 이상으로 3회 완료한다.",
                    MissionPeriod.Weekly,
                    new MissionCondition(
                        MissionConditionType.ClearDifficultyOrHigher,
                        3,
                        targetId: LastTrain.Difficulty.DifficultyIds.Express),
                    tickets: 70,
                    xp: 90),
                Create(
                    "mission_weekly_final_boss_5",
                    "최종 보스",
                    "최종 보스를 5회 처치한다.",
                    MissionPeriod.Weekly,
                    new MissionCondition(MissionConditionType.DefeatFinalBoss, 5),
                    tickets: 100,
                    xp: 120),
            };

            MergeIntoGameDatabase(missions);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("완료", $"미션 {missions.Count}종 생성·등록", "확인");
        }

        private static MissionData Create(
            string id,
            string displayName,
            string description,
            MissionPeriod period,
            MissionCondition condition,
            int tickets,
            int xp)
        {
            string path = $"{MissionFolder}/Mission_{id}.asset";
            MissionData data = LoadOrCreate<MissionData>(path);
            data.EditorSet(id, displayName, description, period, condition, tickets, xp);
            EditorUtility.SetDirty(data);
            return data;
        }

        private static void MergeIntoGameDatabase(IReadOnlyList<MissionData> missions)
        {
            GameDatabase database = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            if (database == null)
            {
                Debug.LogError("[Unit28] GameDatabase를 찾지 못했습니다.");
                return;
            }

            var so = new SerializedObject(database);
            SerializedProperty array = so.FindProperty("missions");
            array.arraySize = missions.Count;
            for (int i = 0; i < missions.Count; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = missions[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
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
