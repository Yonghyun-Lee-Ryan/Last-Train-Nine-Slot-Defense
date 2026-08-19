using System.Collections.Generic;
using LastTrain.Data;
using LastTrain.Event;
using UnityEditor;
using UnityEngine;

namespace LastTrain.EditorTools
{
    public static class Unit27ContentAssetsBuilder
    {
        private const string DatabasePath = "Assets/Data/GameDatabase.asset";
        private const string RelicFolder = "Assets/Data/Relics";
        private const string EventFolder = "Assets/Data/Events";

        [MenuItem("Tools/막차 생존/개발 단위 27 상점·유물·이벤트 생성")]
        public static void BuildContent()
        {
            EnsureFolder("Assets/Data", "Events");

            List<RelicData> relics = CreateRelics();
            List<EventData> events = CreateEvents();
            MergeIntoGameDatabase(relics, events);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("완료", $"유물 {relics.Count}종, 이벤트 {events.Count}종 생성", "확인");
        }

        private static List<RelicData> CreateRelics()
        {
            return new List<RelicData>
            {
                CreateRelic("relic_broken_card", "끊어진 교통카드", "첫 소환 무료", RelicEffectType.FirstSummonFree, 1f),
                CreateRelic("relic_energy_drink", "야근용 에너지드링크", "직장인 공격속도 +15%", RelicEffectType.OfficeWorkerAttackSpeedPercent, 15f),
                CreateRelic("relic_backup_battery", "비상용 보조배터리", "개발자 터렛 지속 +30%", RelicEffectType.DeveloperTurretDurationPercent, 30f),
                CreateRelic("relic_golden_handle", "황금 손잡이", "역 종료 코인 +8", RelicEffectType.StationCompleteCoinBonus, 8f),
                CreateRelic("relic_cat_fur", "고양이 털", "치명타 확률 +5%", RelicEffectType.CritChancePercent, 5f),
                CreateRelic("relic_conductor_hat", "기관사 모자", "객차 최대 HP +15", RelicEffectType.TrainMaxHpFlat, 15f),
                CreateRelic("relic_old_whistle", "낡은 호루라기", "보스 최초 행동 1.5초 지연", RelicEffectType.BossFirstActionDelaySeconds, 1.5f),
                CreateRelic("relic_empty_lunchbox", "빈 도시락", "판매 가격 +20%", RelicEffectType.SellPricePercent, 20f),
                CreateRelic("relic_first_aid", "응급 구급함", "게임당 1회 자동 회복 25", RelicEffectType.EmergencyAutoHealFlat, 25f),
                CreateRelic("relic_transfer_guide", "환승 안내서", "이벤트 나쁜 결과 -20%", RelicEffectType.EventBadOutcomeReductionPercent, 20f),
            };
        }

        private static List<EventData> CreateEvents()
        {
            return new List<EventData>
            {
                CreateEvent(
                    "event_lost_wallet",
                    "잃어버린 지갑",
                    "객실 바닥에 낡은 지갑이 있습니다.",
                    new[]
                    {
                        Choice("take", "주인을 찾아 돌려준다", Effect(EventEffectType.AddCoins, 15f)),
                        Choice("keep", "내용물을 확인한다", Effect(EventEffectType.AddCoins, 30f), Effect(EventEffectType.DamageTrain, 5f)),
                    }),
                CreateEvent(
                    "event_nurse_help",
                    "응급 처치",
                    "간호사가 승객들을 진정시키려 합니다.",
                    new[]
                    {
                        Choice("heal", "도와준다", Effect(EventEffectType.HealTrain, 20f)),
                        Choice("police", "경찰관과 함께 대응한다", Effect(EventEffectType.GrantPassenger, 0f, "passenger_police"), Condition(EventConditionType.RequiresPassenger, "passenger_nurse")),
                    }),
            };
        }

        private static void MergeIntoGameDatabase(IReadOnlyList<RelicData> relics, IReadOnlyList<EventData> events)
        {
            GameDatabase database = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            if (database == null)
            {
                Debug.LogError("[Unit27] GameDatabase를 찾지 못했습니다.");
                return;
            }

            var so = new SerializedObject(database);
            MergeById(so, "relics", relics);
            MergeById(so, "events", events);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
        }

        private static RelicData CreateRelic(string id, string name, string desc, RelicEffectType type, float value)
        {
            string path = $"{RelicFolder}/Relic_{id}.asset";
            var data = LoadOrCreate<RelicData>(path);
            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = name;
            so.FindProperty("description").stringValue = desc;
            so.FindProperty("effectType").enumValueIndex = (int)type;
            so.FindProperty("effectValue").floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            return data;
        }

        private static EventData CreateEvent(string id, string name, string description, EventChoiceData[] choices)
        {
            string path = $"{EventFolder}/Event_{id}.asset";
            var data = LoadOrCreate<EventData>(path);
            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = name;
            so.FindProperty("description").stringValue = description;
            SerializedProperty choicesProp = so.FindProperty("choices");
            choicesProp.arraySize = choices?.Length ?? 0;
            for (int i = 0; i < choicesProp.arraySize; i++)
            {
                WriteChoice(choicesProp.GetArrayElementAtIndex(i), choices[i]);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            return data;
        }

        private static EventChoiceData Choice(
            string id,
            string text,
            params object[] entries)
        {
            var choice = new EventChoiceData { choiceId = id, text = text };
            var effects = new List<EventEffectData>();
            var conditions = new List<EventConditionData>();
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i] is EventEffectData effect)
                {
                    effects.Add(effect);
                }
                else if (entries[i] is EventConditionData condition)
                {
                    conditions.Add(condition);
                }
            }

            choice.effects = effects.ToArray();
            choice.conditions = conditions.ToArray();
            return choice;
        }

        private static EventEffectData Effect(EventEffectType type, float value, string targetId = "")
        {
            return new EventEffectData
            {
                effectType = type,
                value = value,
                targetId = targetId ?? string.Empty,
            };
        }

        private static EventConditionData Condition(EventConditionType type, string targetId, int value = 0)
        {
            return new EventConditionData
            {
                conditionType = type,
                targetId = targetId ?? string.Empty,
                value = value,
            };
        }

        private static void WriteChoice(SerializedProperty element, EventChoiceData choice)
        {
            element.FindPropertyRelative("choiceId").stringValue = choice.choiceId;
            element.FindPropertyRelative("text").stringValue = choice.text;
            WriteEffects(element.FindPropertyRelative("effects"), choice.effects);
            WriteConditions(element.FindPropertyRelative("conditions"), choice.conditions);
        }

        private static void WriteEffects(SerializedProperty array, EventEffectData[] effects)
        {
            array.arraySize = effects?.Length ?? 0;
            for (int i = 0; i < array.arraySize; i++)
            {
                SerializedProperty item = array.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("effectType").enumValueIndex = (int)effects[i].effectType;
                item.FindPropertyRelative("targetId").stringValue = effects[i].targetId ?? string.Empty;
                item.FindPropertyRelative("value").floatValue = effects[i].value;
            }
        }

        private static void WriteConditions(SerializedProperty array, EventConditionData[] conditions)
        {
            array.arraySize = conditions?.Length ?? 0;
            for (int i = 0; i < array.arraySize; i++)
            {
                SerializedProperty item = array.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("conditionType").enumValueIndex = (int)conditions[i].conditionType;
                item.FindPropertyRelative("targetId").stringValue = conditions[i].targetId ?? string.Empty;
                item.FindPropertyRelative("value").intValue = conditions[i].value;
            }
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

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                return existing;
            }

            EnsureFolderChain(System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/'));
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

        private static void EnsureFolderChain(string path)
        {
            if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path))
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
