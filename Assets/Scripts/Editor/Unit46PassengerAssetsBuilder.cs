using System.Collections.Generic;
using LastTrain.Data;
using LastTrain.Passenger.Skills;
using LastTrain.Save;
using UnityEditor;
using UnityEngine;

namespace LastTrain.EditorTools
{
    /// <summary>Unit 46: 승객 4종 추가 + GameDatabase/Visual/Resources 동기화.</summary>
    public static class Unit46PassengerAssetsBuilder
    {
        private const string DatabasePath = "Assets/Data/GameDatabase.asset";
        private const string PassengerFolder = "Assets/Data/Passengers";

        [MenuItem("Tools/막차 생존/개발 단위 46 승객 4종 생성")]
        public static void BuildFromMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "개발 단위 46 승객 4종",
                    "승무원·바리스타·경비원·수험생을 생성하고 GameDatabase에 병합합니다.",
                    "생성",
                    "취소"))
            {
                return;
            }

            BuildInternal(showDialog: true);
        }

        /// <summary>Batchmode: -executeMethod LastTrain.EditorTools.Unit46PassengerAssetsBuilder.BuildBatch</summary>
        public static void BuildBatch()
        {
            try
            {
                BuildInternal(showDialog: false);
                Debug.Log("[Unit46PassengerAssetsBuilder] OK");
                EditorApplication.Exit(0);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[Unit46PassengerAssetsBuilder] " + ex);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Batchmode: -executeMethod LastTrain.EditorTools.Unit46PassengerAssetsBuilder.GeneratePassengerArtBatch</summary>
        public static void GeneratePassengerArtBatch()
        {
            try
            {
                MvpFlatVectorArtGenerator.RegeneratePassengerAndEnemySprites();
                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();
                Debug.Log("[Unit46PassengerAssetsBuilder] Passenger art OK");
                EditorApplication.Exit(0);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[Unit46PassengerAssetsBuilder] art " + ex);
                EditorApplication.Exit(1);
            }
        }

        private static IEnumerable<string> Unit46PassengerSpritePaths()
        {
            string[] ids =
            {
                MetaProgressionDefaults.PassengerConductorId,
                MetaProgressionDefaults.PassengerBaristaId,
                MetaProgressionDefaults.PassengerSecurityId,
                MetaProgressionDefaults.PassengerStudentId,
            };
            string[] suffixes =
            {
                "_portrait.png",
                "_idle_sheet.png",
                "_attack_sheet.png",
                "_skill_sheet.png",
                "_merge_sheet.png",
                "_hit_sheet.png",
            };

            var paths = new List<string>(ids.Length * suffixes.Length);
            for (int i = 0; i < ids.Length; i++)
            {
                for (int j = 0; j < suffixes.Length; j++)
                {
                    paths.Add($"Assets/Art/Sprites/Characters/{ids[i]}{suffixes[j]}");
                }
            }

            return paths;
        }

        private static void BuildInternal(bool showDialog)
        {
            if (!AssetDatabase.IsValidFolder(PassengerFolder))
            {
                AssetDatabase.CreateFolder("Assets/Data", "Passengers");
            }

            var created = new List<PassengerData>
            {
                CreatePassenger(
                    $"{PassengerFolder}/Passenger_Conductor.asset",
                    MetaProgressionDefaults.PassengerConductorId,
                    "승무원",
                    PassengerRole.Support,
                    PassengerTag.Transit,
                    baseAttack: 8f,
                    attackInterval: 1.15f,
                    range: 5.2f,
                    TargetPriority.Nearest,
                    PassengerSkillIds.ChainZap),
                CreatePassenger(
                    $"{PassengerFolder}/Passenger_Barista.asset",
                    MetaProgressionDefaults.PassengerBaristaId,
                    "바리스타",
                    PassengerRole.Support,
                    PassengerTag.Service,
                    baseAttack: 7.5f,
                    attackInterval: 1.2f,
                    range: 4.8f,
                    TargetPriority.Nearest,
                    PassengerSkillIds.ScaldSplash),
                CreatePassenger(
                    $"{PassengerFolder}/Passenger_Security.asset",
                    MetaProgressionDefaults.PassengerSecurityId,
                    "경비원",
                    PassengerRole.Defense,
                    PassengerTag.Security,
                    baseAttack: 9f,
                    attackInterval: 1.1f,
                    range: 4.2f,
                    TargetPriority.Nearest,
                    PassengerSkillIds.PerimeterPulse),
                CreatePassenger(
                    $"{PassengerFolder}/Passenger_Student.asset",
                    MetaProgressionDefaults.PassengerStudentId,
                    "수험생",
                    PassengerRole.Attack,
                    PassengerTag.Commute | PassengerTag.Academic,
                    baseAttack: 10f,
                    attackInterval: 1.3f,
                    range: 5.8f,
                    TargetPriority.LowestHealth,
                    PassengerSkillIds.FocusShot),
            };

            MergeIntoGameDatabase(created);
            MvpVisualDataBuilder.EnsurePassengerVisualsForUnit46();
            ReleaseAssetsBuilder.EnsureReleaseAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (showDialog)
            {
                EditorUtility.DisplayDialog("완료", $"승객 {created.Count}종 추가·동기화 완료", "확인");
            }
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
            TargetPriority priority,
            string skillId)
        {
            PassengerData data = AssetDatabase.LoadAssetAtPath<PassengerData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<PassengerData>();
                AssetDatabase.CreateAsset(data, path);
            }

            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("role").enumValueIndex = (int)role;
            so.FindProperty("tags").intValue = (int)tags;
            so.FindProperty("baseAttack").floatValue = baseAttack;
            so.FindProperty("attackInterval").floatValue = attackInterval;
            so.FindProperty("range").floatValue = range;
            so.FindProperty("targetPriority").enumValueIndex = (int)priority;
            so.FindProperty("maxTargetCount").intValue = 1;
            so.FindProperty("damageType").enumValueIndex = 0;
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

        private static void SetDefaultStarLevels(SerializedProperty starLevels)
        {
            starLevels.arraySize = 3;
            WriteStar(starLevels.GetArrayElementAtIndex(0), 1, "일반", 1f, 1f, 0f, 1f);
            WriteStar(starLevels.GetArrayElementAtIndex(1), 2, "숙련", 2.2f, 1.05f, 0f, 1.2f);
            WriteStar(starLevels.GetArrayElementAtIndex(2), 3, "전문", 4.8f, 1.1f, 0f, 1.5f);
        }

        private static void WriteStar(
            SerializedProperty element,
            int star,
            string nameOverride,
            float attackMul,
            float speedMul,
            float rangeBonus,
            float skillMul)
        {
            element.FindPropertyRelative("starLevel").intValue = star;
            element.FindPropertyRelative("displayNameOverride").stringValue = nameOverride;
            element.FindPropertyRelative("attackMultiplier").floatValue = attackMul;
            element.FindPropertyRelative("attackSpeedMultiplier").floatValue = speedMul;
            element.FindPropertyRelative("rangeBonus").floatValue = rangeBonus;
            element.FindPropertyRelative("skillValueMultiplier").floatValue = skillMul;
        }

        private static void MergeIntoGameDatabase(List<PassengerData> passengers)
        {
            GameDatabase database = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            if (database == null)
            {
                throw new System.InvalidOperationException("GameDatabase missing: " + DatabasePath);
            }

            var so = new SerializedObject(database);
            SerializedProperty array = so.FindProperty("passengers");
            var existing = new HashSet<string>();
            for (int i = 0; i < array.arraySize; i++)
            {
                var refData = array.GetArrayElementAtIndex(i).objectReferenceValue as PassengerData;
                if (refData != null && !string.IsNullOrWhiteSpace(refData.Id))
                {
                    existing.Add(refData.Id);
                }
            }

            for (int i = 0; i < passengers.Count; i++)
            {
                PassengerData passenger = passengers[i];
                if (passenger == null || existing.Contains(passenger.Id))
                {
                    // Replace existing reference if same id
                    if (passenger != null)
                    {
                        for (int j = 0; j < array.arraySize; j++)
                        {
                            var refData = array.GetArrayElementAtIndex(j).objectReferenceValue as PassengerData;
                            if (refData != null && refData.Id == passenger.Id)
                            {
                                array.GetArrayElementAtIndex(j).objectReferenceValue = passenger;
                            }
                        }
                    }

                    continue;
                }

                int index = array.arraySize;
                array.arraySize++;
                array.GetArrayElementAtIndex(index).objectReferenceValue = passenger;
                existing.Add(passenger.Id);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
        }
    }
}
