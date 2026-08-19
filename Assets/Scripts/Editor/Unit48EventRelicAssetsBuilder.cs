using System.Collections.Generic;
using LastTrain.Data;
using LastTrain.Event;
using UnityEditor;
using UnityEngine;

namespace LastTrain.EditorTools
{
    /// <summary>Unit 48: 이벤트 8종 · 유물 6종 병합 + Line1 6번째 역 Rest 배치.</summary>
    public static class Unit48EventRelicAssetsBuilder
    {
        private const string DatabasePath = "Assets/Data/GameDatabase.asset";
        private const string EventFolder = "Assets/Data/Events";
        private const string RelicFolder = "Assets/Data/Relics";
        private const string Station06Path = "Assets/Data/Stations/Station_06.asset";

        [MenuItem("Tools/막차 생존/개발 단위 48 이벤트·유물·Rest 생성")]
        public static void BuildFromMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "개발 단위 48 이벤트·유물",
                    "이벤트 8종·유물 6종을 병합하고 Line1 6번째 역을 휴식으로 바꿉니다.",
                    "생성",
                    "취소"))
            {
                return;
            }

            BuildInternal(showDialog: true);
        }

        /// <summary>Batchmode: -executeMethod LastTrain.EditorTools.Unit48EventRelicAssetsBuilder.BuildBatch</summary>
        public static void BuildBatch()
        {
            try
            {
                BuildInternal(showDialog: false);
                Debug.Log("[Unit48EventRelicAssetsBuilder] OK");
                EditorApplication.Exit(0);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[Unit48EventRelicAssetsBuilder] " + ex);
                EditorApplication.Exit(1);
            }
        }

        private static void BuildInternal(bool showDialog)
        {
            EnsureFolder("Assets/Data", "Events");
            EnsureFolder("Assets/Data", "Relics");

            List<EventData> events = CreateEvents();
            List<RelicData> relics = CreateRelics();
            ConvertLine1Station06ToRest();

            GameDatabase database = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            if (database == null)
            {
                throw new System.InvalidOperationException("GameDatabase.asset 없음: " + DatabasePath);
            }

            var so = new SerializedObject(database);
            MergeById(so, "events", events);
            MergeById(so, "relics", relics);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);

            ReleaseAssetsBuilder.EnsureReleaseAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "완료",
                    $"이벤트 {events.Count}종, 유물 {relics.Count}종 병합 · 6번째 역 휴식",
                    "확인");
            }
        }

        private static List<RelicData> CreateRelics()
        {
            return new List<RelicData>
            {
                CreateRelic(
                    "relic_night_coffee",
                    "심야 캔커피",
                    "직장인 공격속도 +10%",
                    RelicEffectType.OfficeWorkerAttackSpeedPercent,
                    10f),
                CreateRelic(
                    "relic_spare_fuse",
                    "예비 퓨즈",
                    "개발자 터렛 지속 +20%",
                    RelicEffectType.DeveloperTurretDurationPercent,
                    20f),
                CreateRelic(
                    "relic_coin_pouch",
                    "잔돈 주머니",
                    "역 종료 코인 +5",
                    RelicEffectType.StationCompleteCoinBonus,
                    5f),
                CreateRelic(
                    "relic_platform_bench",
                    "승강장 벤치",
                    "객차 최대 HP +10",
                    RelicEffectType.TrainMaxHpFlat,
                    10f),
                CreateRelic(
                    "relic_lost_umbrella",
                    "잊힌 우산",
                    "이벤트 나쁜 결과 -15%",
                    RelicEffectType.EventBadOutcomeReductionPercent,
                    15f),
                CreateRelic(
                    "relic_warm_pack",
                    "손난로",
                    "게임당 1회 자동 회복 +15",
                    RelicEffectType.EmergencyAutoHealFlat,
                    15f),
            };
        }

        private static List<EventData> CreateEvents()
        {
            return new List<EventData>
            {
                CreateEvent(
                    "event_vending_jam",
                    "고장 난 자판기",
                    "심야 자판기가 동전만 삼킵니다.",
                    new[]
                    {
                        Choice("kick", "슬쩍 걷어찬다", Effect(EventEffectType.AddCoins, 12f)),
                        Choice(
                            "force",
                            "억지로 빼낸다",
                            Effect(EventEffectType.AddCoins, 28f),
                            Effect(EventEffectType.DamageTrain, -8f)),
                    }),
                CreateEvent(
                    "event_last_delay",
                    "막차 지연 방송",
                    "종착이 잠시 미뤄진다는 안내가 나옵니다.",
                    new[]
                    {
                        Choice("wait", "기다리며 숨 고른다", Effect(EventEffectType.HealTrain, 18f)),
                        Choice(
                            "push",
                            "시간을 끌어 보상 노선을 탄다",
                            Effect(EventEffectType.NextStationRewardBonus, 1.5f),
                            Effect(EventEffectType.NextStationEnemyBuff, 1.25f)),
                    }),
                CreateEvent(
                    "event_ticket_gate",
                    "개찰구 혼잡",
                    "개찰구에 사람들이 끼어 있습니다.",
                    new[]
                    {
                        Choice("queue", "줄을 선다", Effect(EventEffectType.AddCoins, 10f)),
                        Choice(
                            "squeeze",
                            "빈틈을 노린다",
                            Effect(EventEffectType.RemoveCoins, -20f),
                            Effect(EventEffectType.AddCoins, 45f),
                            Condition(EventConditionType.MinCoins, string.Empty, 20)),
                    }),
                CreateEvent(
                    "event_lost_umbrella",
                    "유실물 우산",
                    "벤치에 접힌 우산이 놓여 있습니다.",
                    new[]
                    {
                        Choice("leave", "그대로 둔다", Effect(EventEffectType.HealTrain, 10f)),
                        Choice("take", "챙겨 간다", Effect(EventEffectType.GrantRelic, 0f, "relic_lost_umbrella")),
                    }),
                CreateEvent(
                    "event_overtime_board",
                    "야근 공지판",
                    "직장인만 알아보는 야근 공지가 붙어 있습니다.",
                    new[]
                    {
                        Choice("ignore", "지나친다", Effect(EventEffectType.AddCoins, 8f)),
                        Choice(
                            "cover",
                            "야근을 커버한다",
                            Effect(EventEffectType.GrantAbility, 0f, "ability_train_max_hp"),
                            Condition(EventConditionType.RequiresPassenger, "passenger_office_worker")),
                    }),
                CreateEvent(
                    "event_platform_cat",
                    "승강장 길고양이",
                    "막차 시간에 고양이가 다가옵니다.",
                    new[]
                    {
                        Choice("pet", "쓰다듬는다", Effect(EventEffectType.HealTrain, 12f)),
                        Choice(
                            "feed",
                            "함께 온기를 나눈다",
                            Effect(EventEffectType.GrantRelic, 0f, "relic_warm_pack"),
                            Condition(EventConditionType.RequiresPassenger, "passenger_cat")),
                    }),
                CreateEvent(
                    "event_night_check",
                    "심야 선로 점검",
                    "정비 안내 방송이 울립니다.",
                    new[]
                    {
                        Choice(
                            "detour",
                            "우회한다",
                            Effect(EventEffectType.NextStationEnemyBuff, 1.2f),
                            Effect(EventEffectType.HealTrain, 15f)),
                        Choice(
                            "help",
                            "환승 안내서를 펼친다",
                            Effect(EventEffectType.NextStationRewardBonus, 1.4f),
                            Condition(EventConditionType.RequiresRelic, "relic_transfer_guide")),
                    }),
                CreateEvent(
                    "event_last_call_cafe",
                    "막차 카페 셔터",
                    "카페가 셔터를 내리며 남은 원두를 건넵니다.",
                    new[]
                    {
                        Choice("pass", "지나간다", Effect(EventEffectType.AddCoins, 15f)),
                        Choice(
                            "buy",
                            "마지막 잔을 산다",
                            Effect(EventEffectType.RemoveCoins, -25f),
                            Effect(EventEffectType.GrantRelic, 0f, "relic_night_coffee"),
                            Condition(EventConditionType.MinCoins, string.Empty, 25)),
                        Choice(
                            "staff",
                            "바리스타와 마감한다",
                            Effect(EventEffectType.HealTrain, 25f),
                            Condition(EventConditionType.RequiresPassenger, "passenger_barista")),
                    }),
            };
        }

        private static void ConvertLine1Station06ToRest()
        {
            StationData data = AssetDatabase.LoadAssetAtPath<StationData>(Station06Path);
            if (data == null)
            {
                throw new System.InvalidOperationException("Station_06.asset 없음: " + Station06Path);
            }

            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = "line1_station_06";
            so.FindProperty("displayName").stringValue = "6번째 역 (휴식)";
            so.FindProperty("stationType").enumValueIndex = (int)StationType.Rest;
            so.FindProperty("stationIndex").intValue = 6;
            so.FindProperty("difficultyMultiplier").floatValue = 1f;
            so.FindProperty("rewardCoins").intValue = 12;
            so.FindProperty("grantsAbilityChoice").boolValue = false;
            so.FindProperty("bossPatternHint").stringValue = string.Empty;
            so.FindProperty("waves").arraySize = 0;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
        }

        private static RelicData CreateRelic(
            string id,
            string displayName,
            string description,
            RelicEffectType effectType,
            float effectValue)
        {
            string path = $"{RelicFolder}/Relic_{id}.asset";
            RelicData data = AssetDatabase.LoadAssetAtPath<RelicData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<RelicData>();
                AssetDatabase.CreateAsset(data, path);
            }

            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("description").stringValue = description;
            so.FindProperty("rarity").enumValueIndex = (int)Rarity.Common;
            so.FindProperty("effectType").enumValueIndex = (int)effectType;
            so.FindProperty("effectValue").floatValue = effectValue;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            return data;
        }

        private static EventData CreateEvent(string id, string displayName, string description, EventChoiceData[] choices)
        {
            string path = $"{EventFolder}/Event_{id}.asset";
            EventData data = AssetDatabase.LoadAssetAtPath<EventData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<EventData>();
                AssetDatabase.CreateAsset(data, path);
            }

            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = displayName;
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

        private static EventChoiceData Choice(string id, string text, params object[] entries)
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
