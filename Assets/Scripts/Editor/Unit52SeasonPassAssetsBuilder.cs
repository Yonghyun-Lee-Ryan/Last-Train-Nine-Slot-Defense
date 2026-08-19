using LastTrain.LiveOps;
using UnityEditor;
using UnityEngine;

namespace LastTrain.EditorTools
{
    /// <summary>Unit 52: Heatwave 트랙에 Ad 레인 2단을 병합한다.</summary>
    public static class Unit52SeasonPassAssetsBuilder
    {
        private const string TrackPath = "Assets/Data/LiveOps/Rewards/EventRewardTrack_Heatwave.asset";

        [MenuItem("Tools/막차 생존/개발 단위 52 시즌 패스 Ad 트랙")]
        public static void BuildFromMenu()
        {
            BuildInternal(showDialog: true);
        }

        /// <summary>Batchmode: -executeMethod LastTrain.EditorTools.Unit52SeasonPassAssetsBuilder.BuildBatch</summary>
        public static void BuildBatch()
        {
            try
            {
                BuildInternal(showDialog: false);
                Debug.Log("[Unit52SeasonPassAssetsBuilder] OK");
                EditorApplication.Exit(0);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[Unit52SeasonPassAssetsBuilder] " + ex);
                EditorApplication.Exit(1);
            }
        }

        private static void BuildInternal(bool showDialog)
        {
            EventRewardTrack track = AssetDatabase.LoadAssetAtPath<EventRewardTrack>(TrackPath);
            if (track == null)
            {
                throw new System.InvalidOperationException("Heatwave 트랙 없음: " + TrackPath);
            }

            var so = new SerializedObject(track);
            SerializedProperty steps = so.FindProperty("steps");
            EnsureStep(steps, "reward_10", 10, 5, 0, string.Empty, RewardTrackLane.Free);
            EnsureStep(steps, "reward_50", 50, 0, 20, "passenger_cat", RewardTrackLane.Free);
            EnsureStep(steps, "ad_reward_10", 10, 8, 0, string.Empty, RewardTrackLane.Ad);
            EnsureStep(steps, "ad_reward_50", 50, 0, 15, string.Empty, RewardTrackLane.Ad);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(track);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (showDialog)
            {
                EditorUtility.DisplayDialog("완료", "시즌 패스 Free/Ad 트랙 병합", "확인");
            }
        }

        private static void EnsureStep(
            SerializedProperty steps,
            string rewardId,
            int currency,
            int tickets,
            int xp,
            string unlock,
            RewardTrackLane lane)
        {
            for (int i = 0; i < steps.arraySize; i++)
            {
                SerializedProperty item = steps.GetArrayElementAtIndex(i);
                if (item.FindPropertyRelative("rewardId").stringValue == rewardId)
                {
                    Write(item, rewardId, currency, tickets, xp, unlock, lane);
                    return;
                }
            }

            int index = steps.arraySize;
            steps.arraySize++;
            Write(steps.GetArrayElementAtIndex(index), rewardId, currency, tickets, xp, unlock, lane);
        }

        private static void Write(
            SerializedProperty item,
            string rewardId,
            int currency,
            int tickets,
            int xp,
            string unlock,
            RewardTrackLane lane)
        {
            item.FindPropertyRelative("rewardId").stringValue = rewardId;
            item.FindPropertyRelative("requiredCurrency").intValue = currency;
            item.FindPropertyRelative("ticketFragments").intValue = tickets;
            item.FindPropertyRelative("accountXp").intValue = xp;
            item.FindPropertyRelative("unlockPassengerId").stringValue = unlock ?? string.Empty;
            item.FindPropertyRelative("lane").enumValueIndex = (int)lane;
        }
    }
}
