using System.Collections.Generic;
using LastTrain.Data;
using UnityEditor;
using UnityEngine;

namespace LastTrain.EditorTools
{
    /// <summary>Unit 49: Daily Rule 6종 + GameDatabase/Resources 동기화.</summary>
    public static class Unit49DailyRuleAssetsBuilder
    {
        private const string DatabasePath = "Assets/Data/GameDatabase.asset";
        private const string Folder = "Assets/Data/DailyRules";

        [MenuItem("Tools/막차 생존/개발 단위 49 Daily Rule 생성")]
        public static void BuildFromMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "개발 단위 49 Daily Rule",
                    "일일 규칙 6종을 생성하고 GameDatabase에 병합합니다.",
                    "생성",
                    "취소"))
            {
                return;
            }

            BuildInternal(showDialog: true);
        }

        /// <summary>Batchmode: -executeMethod LastTrain.EditorTools.Unit49DailyRuleAssetsBuilder.BuildBatch</summary>
        public static void BuildBatch()
        {
            try
            {
                BuildInternal(showDialog: false);
                Debug.Log("[Unit49DailyRuleAssetsBuilder] OK");
                EditorApplication.Exit(0);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[Unit49DailyRuleAssetsBuilder] " + ex);
                EditorApplication.Exit(1);
            }
        }

        private static void BuildInternal(bool showDialog)
        {
            if (!AssetDatabase.IsValidFolder(Folder))
            {
                AssetDatabase.CreateFolder("Assets/Data", "DailyRules");
            }

            var created = new List<DailyRuleData>
            {
                CreateRule(
                    "daily_rule_locked_seat",
                    "공사 중 좌석",
                    "오늘 좌석 1칸이 잠긴다.",
                    DailyRuleKind.LockSeat,
                    1f,
                    string.Empty),
                CreateRule(
                    "daily_rule_summon_tax",
                    "심야 할증",
                    "소환 비용 +25%.",
                    DailyRuleKind.SummonCostMul,
                    1.25f,
                    string.Empty),
                CreateRule(
                    "daily_rule_cheap_summon",
                    "막차 할인",
                    "소환 비용 -25%.",
                    DailyRuleKind.SummonCostMul,
                    0.75f,
                    string.Empty),
                CreateRule(
                    "daily_rule_rush_hour",
                    "러시 아워",
                    "적 이동 속도 +15%.",
                    DailyRuleKind.EnemySpeedMul,
                    1.15f,
                    string.Empty),
                CreateRule(
                    "daily_rule_lost_and_found",
                    "유실물 센터",
                    "끊어진 교통카드를 들고 출발한다.",
                    DailyRuleKind.GrantRelic,
                    1f,
                    "relic_broken_card"),
                CreateRule(
                    "daily_rule_no_dwell",
                    "무정차 운행",
                    "준비 시간이 2초로 줄어든다.",
                    DailyRuleKind.ReducedPrepTime,
                    2f,
                    string.Empty),
            };

            GameDatabase database = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            if (database == null)
            {
                throw new System.InvalidOperationException("GameDatabase.asset 없음: " + DatabasePath);
            }

            var so = new SerializedObject(database);
            MergeById(so, "dailyRules", created);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);

            ReleaseAssetsBuilder.EnsureReleaseAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (showDialog)
            {
                EditorUtility.DisplayDialog("완료", $"Daily Rule {created.Count}종 병합 완료", "확인");
            }
        }

        private static DailyRuleData CreateRule(
            string id,
            string displayName,
            string description,
            DailyRuleKind kind,
            float magnitude,
            string targetId)
        {
            string path = $"{Folder}/DailyRule_{id}.asset";
            DailyRuleData data = AssetDatabase.LoadAssetAtPath<DailyRuleData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<DailyRuleData>();
                AssetDatabase.CreateAsset(data, path);
            }

            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("description").stringValue = description;
            so.FindProperty("kind").enumValueIndex = (int)kind;
            so.FindProperty("magnitude").floatValue = magnitude;
            so.FindProperty("targetId").stringValue = targetId ?? string.Empty;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            return data;
        }

        private static void MergeById<T>(SerializedObject so, string propertyName, IReadOnlyList<T> created)
            where T : Object, IDataWithId
        {
            SerializedProperty array = so.FindProperty(propertyName);
            var existingIds = new HashSet<string>();
            for (int i = 0; i < array.arraySize; i++)
            {
                if (array.GetArrayElementAtIndex(i).objectReferenceValue is IDataWithId withId
                    && !string.IsNullOrWhiteSpace(withId.Id))
                {
                    existingIds.Add(withId.Id);
                }
            }

            for (int i = 0; i < created.Count; i++)
            {
                T item = created[i];
                if (item == null || string.IsNullOrWhiteSpace(item.Id))
                {
                    continue;
                }

                if (existingIds.Contains(item.Id))
                {
                    for (int j = 0; j < array.arraySize; j++)
                    {
                        if (array.GetArrayElementAtIndex(j).objectReferenceValue is IDataWithId withId
                            && withId.Id == item.Id)
                        {
                            array.GetArrayElementAtIndex(j).objectReferenceValue = item;
                            break;
                        }
                    }
                }
                else
                {
                    int index = array.arraySize;
                    array.arraySize++;
                    array.GetArrayElementAtIndex(index).objectReferenceValue = item;
                    existingIds.Add(item.Id);
                }
            }
        }
    }
}
