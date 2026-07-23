using LastTrain.Data;
using LastTrain.LiveOps;
using LastTrain.Mission;
using UnityEditor;
using UnityEngine;

namespace LastTrain.EditorTools
{
    public static class Unit34LiveOpsAssetsBuilder
    {
        private const string Root = "Assets/Data/LiveOps";
        private const string CatalogPath = "Assets/Resources/LiveOps/LiveOpsCatalog.asset";

        [MenuItem("Tools/막차 생존/개발 단위 34 시즌·이벤트 샘플 생성")]
        public static void Build()
        {
            EnsureFolder("Assets/Data", "LiveOps");
            EnsureFolder(Root, "Currencies");
            EnsureFolder(Root, "Rewards");
            EnsureFolder(Root, "Events");
            EnsureFolder(Root, "Seasons");
            EnsureFolder("Assets/Resources", "LiveOps");

            RouteData route = AssetDatabase.LoadAssetAtPath<RouteData>("Assets/Data/Routes/Route_Line1.asset");
            MissionData mission = AssetDatabase.LoadAssetAtPath<MissionData>(
                "Assets/Data/Missions/Mission_mission_daily_merge_5.asset");

            EventCurrencyData currency = CreateAsset<EventCurrencyData>(
                $"{Root}/Currencies/EventCurrency_Token.asset",
                so =>
                {
                    so.FindProperty("id").stringValue = "event_token";
                    so.FindProperty("displayName").stringValue = "이벤트 토큰";
                    so.FindProperty("maxBalance").intValue = 99999;
                });

            EventRewardTrack track = CreateAsset<EventRewardTrack>(
                $"{Root}/Rewards/EventRewardTrack_Heatwave.asset",
                so =>
                {
                    so.FindProperty("id").stringValue = "track_heatwave";
                    var steps = so.FindProperty("steps");
                    steps.arraySize = 2;
                    steps.GetArrayElementAtIndex(0).FindPropertyRelative("rewardId").stringValue = "reward_10";
                    steps.GetArrayElementAtIndex(0).FindPropertyRelative("requiredCurrency").intValue = 10;
                    steps.GetArrayElementAtIndex(0).FindPropertyRelative("ticketFragments").intValue = 5;
                    steps.GetArrayElementAtIndex(1).FindPropertyRelative("rewardId").stringValue = "reward_50";
                    steps.GetArrayElementAtIndex(1).FindPropertyRelative("requiredCurrency").intValue = 50;
                    steps.GetArrayElementAtIndex(1).FindPropertyRelative("accountXp").intValue = 20;
                    steps.GetArrayElementAtIndex(1).FindPropertyRelative("unlockPassengerId").stringValue =
                        "passenger_cat";
                });

            LiveEventData evt = CreateAsset<LiveEventData>(
                $"{Root}/Events/LiveEvent_Heatwave.asset",
                so =>
                {
                    so.FindProperty("id").stringValue = "event_heatwave";
                    so.FindProperty("displayName").stringValue = "폭염 막차";
                    so.FindProperty("themeId").stringValue = "heatwave";
                    so.FindProperty("startUtc").stringValue = "2026-07-01T00:00:00Z";
                    so.FindProperty("endUtc").stringValue = "2026-12-31T23:59:59Z";
                    so.FindProperty("eventCurrency").objectReferenceValue = currency;
                    so.FindProperty("rewardTrack").objectReferenceValue = track;
                    so.FindProperty("eventRoute").objectReferenceValue = route;
                    so.FindProperty("dailyCurrencyCap").intValue = 200;
                    so.FindProperty("boostedPassengerAttackMultiplier").floatValue = 1.25f;
                    var boosted = so.FindProperty("boostedPassengerIds");
                    boosted.arraySize = 1;
                    boosted.GetArrayElementAtIndex(0).stringValue = "passenger_office_worker";
                    if (mission != null)
                    {
                        var missions = so.FindProperty("eventMissions");
                        missions.arraySize = 1;
                        missions.GetArrayElementAtIndex(0).objectReferenceValue = mission;
                    }
                });

            SeasonData season = CreateAsset<SeasonData>(
                $"{Root}/Seasons/Season_01.asset",
                so =>
                {
                    so.FindProperty("id").stringValue = "season_01";
                    so.FindProperty("displayName").stringValue = "시즌 1";
                    so.FindProperty("themeId").stringValue = "summer";
                    var events = so.FindProperty("events");
                    events.arraySize = 1;
                    events.GetArrayElementAtIndex(0).objectReferenceValue = evt;
                });

            WriteCatalog(season, evt);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(
                "완료",
                "시즌/이벤트 샘플과 Resources/LiveOps/LiveOpsCatalog를 갱신했습니다.",
                "확인");
        }

        [MenuItem("Tools/막차 생존/개발 단위 34 LiveOps Resources 동기화")]
        public static void SyncCatalog()
        {
            EnsureFolder("Assets/Resources", "LiveOps");
            SeasonData season = AssetDatabase.LoadAssetAtPath<SeasonData>($"{Root}/Seasons/Season_01.asset");
            LiveEventData evt = AssetDatabase.LoadAssetAtPath<LiveEventData>($"{Root}/Events/LiveEvent_Heatwave.asset");
            WriteCatalog(season, evt);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void WriteCatalog(SeasonData season, LiveEventData evt)
        {
            LiveOpsCatalog catalog = CreateAsset<LiveOpsCatalog>(
                CatalogPath,
                so =>
                {
                    var seasons = so.FindProperty("seasons");
                    seasons.arraySize = season != null ? 1 : 0;
                    if (season != null)
                    {
                        seasons.GetArrayElementAtIndex(0).objectReferenceValue = season;
                    }

                    var events = so.FindProperty("events");
                    events.arraySize = evt != null ? 1 : 0;
                    if (evt != null)
                    {
                        events.GetArrayElementAtIndex(0).objectReferenceValue = evt;
                    }
                });
            _ = catalog;
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static T CreateAsset<T>(string path, System.Action<SerializedObject> configure)
            where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }

            var so = new SerializedObject(asset);
            configure?.Invoke(so);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }
    }
}
