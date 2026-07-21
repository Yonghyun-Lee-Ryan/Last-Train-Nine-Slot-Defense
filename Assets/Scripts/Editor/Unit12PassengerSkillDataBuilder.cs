using LastTrain.Data;
using LastTrain.Passenger.Skills;
using UnityEditor;
using UnityEngine;

namespace LastTrain.EditorTools
{
    /// <summary>개발 단위 12: 개발자·대학원생 승객 데이터와 GameDatabase 등록.</summary>
    public static class Unit12PassengerSkillDataBuilder
    {
        private const string GameDatabasePath = "Assets/Data/GameDatabase.asset";

        [MenuItem("Tools/막차 생존/개발 단위 12 승객 스킬 데이터 추가")]
        public static void BuildSkillPassengers()
        {
            if (!EditorUtility.DisplayDialog(
                    "개발 단위 12 승객 스킬 데이터",
                    "개발자·대학원생 PassengerData를 생성하고 GameDatabase에 등록합니다.\n계속할까요?",
                    "추가",
                    "취소"))
            {
                return;
            }

            PassengerData developer = CreatePassenger(
                "Assets/Data/Passengers/Passenger_Developer.asset",
                "passenger_developer",
                "개발자",
                PassengerRole.Summon,
                PassengerTag.Tech,
                baseAttack: 7f,
                attackInterval: 1.0f,
                range: 5f,
                TargetPriority.Nearest,
                PassengerSkillIds.TemporaryTurret);

            PassengerData graduate = CreatePassenger(
                "Assets/Data/Passengers/Passenger_Graduate.asset",
                "passenger_graduate",
                "대학원생",
                PassengerRole.Special,
                PassengerTag.Academic,
                baseAttack: 9f,
                attackInterval: 1.15f,
                range: 5.5f,
                TargetPriority.LowestHealth,
                PassengerSkillIds.CriticalAreaDamage);

            var database = AssetDatabase.LoadAssetAtPath<GameDatabase>(GameDatabasePath);
            if (database == null)
            {
                EditorUtility.DisplayDialog("오류", "GameDatabase를 찾지 못했습니다.", "확인");
                return;
            }

            var so = new SerializedObject(database);
            SerializedProperty passengers = so.FindProperty("passengers");
            AppendIfMissing(passengers, developer);
            AppendIfMissing(passengers, graduate);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog(
                "완료",
                "개발자(임시 터렛)·대학원생(광역 치명타) 데이터를 등록했습니다.",
                "확인");
        }

        private static void AppendIfMissing(SerializedProperty array, PassengerData data)
        {
            if (array == null || data == null)
            {
                return;
            }

            for (int i = 0; i < array.arraySize; i++)
            {
                if (array.GetArrayElementAtIndex(i).objectReferenceValue == data)
                {
                    return;
                }
            }

            int index = array.arraySize;
            array.arraySize++;
            array.GetArrayElementAtIndex(index).objectReferenceValue = data;
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
            var data = AssetDatabase.LoadAssetAtPath<PassengerData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<PassengerData>();
                AssetDatabase.CreateAsset(data, path);
            }

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

            SerializedProperty starLevels = so.FindProperty("starLevels");
            starLevels.arraySize = 3;
            WriteStar(starLevels.GetArrayElementAtIndex(0), PassengerStarData.CreateDefault(1), "일반");
            WriteStar(starLevels.GetArrayElementAtIndex(1), PassengerStarData.CreateDefault(2), "숙련");
            WriteStar(starLevels.GetArrayElementAtIndex(2), PassengerStarData.CreateDefault(3), "전문");
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            return data;
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
    }
}
