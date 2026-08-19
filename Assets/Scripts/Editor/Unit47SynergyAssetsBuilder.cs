using System.Collections.Generic;
using LastTrain.Data;
using UnityEditor;
using UnityEngine;

namespace LastTrain.EditorTools
{
    /// <summary>Unit 47: 시너지 5종 추가 + GameDatabase/Resources 동기화.</summary>
    public static class Unit47SynergyAssetsBuilder
    {
        private const string DatabasePath = "Assets/Data/GameDatabase.asset";
        private const string SynergyFolder = "Assets/Data/Synergies";

        [MenuItem("Tools/막차 생존/개발 단위 47 시너지 5종 생성")]
        public static void BuildFromMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "개발 단위 47 시너지 5종",
                    "심야배송·심야카페·승강장경비·통학러시·막차행운을 생성하고 GameDatabase에 병합합니다.",
                    "생성",
                    "취소"))
            {
                return;
            }

            BuildInternal(showDialog: true);
        }

        /// <summary>Batchmode: -executeMethod LastTrain.EditorTools.Unit47SynergyAssetsBuilder.BuildBatch</summary>
        public static void BuildBatch()
        {
            try
            {
                BuildInternal(showDialog: false);
                Debug.Log("[Unit47SynergyAssetsBuilder] OK");
                EditorApplication.Exit(0);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[Unit47SynergyAssetsBuilder] " + ex);
                EditorApplication.Exit(1);
            }
        }

        private static void BuildInternal(bool showDialog)
        {
            if (!AssetDatabase.IsValidFolder(SynergyFolder))
            {
                AssetDatabase.CreateFolder("Assets/Data", "Synergies");
            }

            var created = new List<SynergyData>
            {
                CreateSynergy(
                    $"{SynergyFolder}/Synergy_NightCourier.asset",
                    "synergy_night_courier",
                    "심야배송",
                    "배달기사·승무원 → 빠른 적 피해 +20%",
                    PassengerTag.Delivery | PassengerTag.Transit,
                    requiredCount: 2,
                    uniqueCount: 0,
                    SynergyEffectType.FastEnemyDamagePercent,
                    20f),
                CreateSynergy(
                    $"{SynergyFolder}/Synergy_LastCall.asset",
                    "synergy_last_call",
                    "심야카페",
                    "바리스타·직장인 → 공격속도 +8%",
                    PassengerTag.Service | PassengerTag.OfficeWorker,
                    requiredCount: 2,
                    uniqueCount: 0,
                    SynergyEffectType.AttackSpeedPercent,
                    8f),
                CreateSynergy(
                    $"{SynergyFolder}/Synergy_PlatformGuard.asset",
                    "synergy_platform_guard",
                    "승강장경비",
                    "경찰관·경비원 → 전체 공격력 +12%",
                    PassengerTag.LawEnforcement | PassengerTag.Security,
                    requiredCount: 2,
                    uniqueCount: 0,
                    SynergyEffectType.AllAttackPercent,
                    12f),
                CreateSynergy(
                    $"{SynergyFolder}/Synergy_ExamRush.asset",
                    "synergy_exam_rush",
                    "통학러시",
                    "수험생·대학원생 → 치명타 확률 +10%",
                    PassengerTag.Commute | PassengerTag.Academic,
                    requiredCount: 2,
                    uniqueCount: 0,
                    SynergyEffectType.CritChancePercent,
                    10f),
                CreateSynergy(
                    $"{SynergyFolder}/Synergy_StrayExpress.asset",
                    "synergy_stray_express",
                    "막차행운",
                    "고양이·승무원 → 치명타 확률 +8%",
                    PassengerTag.Lucky | PassengerTag.Transit,
                    requiredCount: 2,
                    uniqueCount: 0,
                    SynergyEffectType.CritChancePercent,
                    8f),
            };

            MergeIntoGameDatabase(created);
            ReleaseAssetsBuilder.EnsureReleaseAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (showDialog)
            {
                EditorUtility.DisplayDialog("완료", $"시너지 {created.Count}종 추가·동기화 완료", "확인");
            }
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
            SynergyData data = AssetDatabase.LoadAssetAtPath<SynergyData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<SynergyData>();
                AssetDatabase.CreateAsset(data, path);
            }

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

        private static void MergeIntoGameDatabase(List<SynergyData> created)
        {
            GameDatabase database = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            if (database == null)
            {
                throw new System.InvalidOperationException("GameDatabase.asset 없음: " + DatabasePath);
            }

            var so = new SerializedObject(database);
            SerializedProperty array = so.FindProperty("synergies");
            var existingIds = new HashSet<string>();
            for (int i = 0; i < array.arraySize; i++)
            {
                var obj = array.GetArrayElementAtIndex(i).objectReferenceValue as SynergyData;
                if (obj != null && !string.IsNullOrWhiteSpace(obj.Id))
                {
                    existingIds.Add(obj.Id);
                }
            }

            for (int i = 0; i < created.Count; i++)
            {
                SynergyData synergy = created[i];
                if (synergy == null || string.IsNullOrWhiteSpace(synergy.Id))
                {
                    continue;
                }

                if (existingIds.Contains(synergy.Id))
                {
                    for (int j = 0; j < array.arraySize; j++)
                    {
                        var obj = array.GetArrayElementAtIndex(j).objectReferenceValue as SynergyData;
                        if (obj != null && obj.Id == synergy.Id)
                        {
                            array.GetArrayElementAtIndex(j).objectReferenceValue = synergy;
                            break;
                        }
                    }
                }
                else
                {
                    int index = array.arraySize;
                    array.arraySize++;
                    array.GetArrayElementAtIndex(index).objectReferenceValue = synergy;
                    existingIds.Add(synergy.Id);
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
        }
    }
}
