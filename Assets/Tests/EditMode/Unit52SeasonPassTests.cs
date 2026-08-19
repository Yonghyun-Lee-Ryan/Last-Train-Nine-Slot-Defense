using LastTrain.Ads;
using LastTrain.LiveOps;
using LastTrain.Save;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class Unit52SeasonPassTests
    {
        private sealed class FixedClock : ILiveEventClock
        {
            public System.DateTime UtcNow { get; set; }
        }

        [Test]
        public void HeatwaveTrack_HasFreeAndAdLanes()
        {
            EventRewardTrack track = Resources.Load<EventRewardTrack>("LiveOps/Rewards/EventRewardTrack_Heatwave");
            if (track == null)
            {
                track = AssetDatabase.LoadAssetAtPath<EventRewardTrack>(
                    "Assets/Data/LiveOps/Rewards/EventRewardTrack_Heatwave.asset");
            }

            Assert.IsNotNull(track);
            Assert.GreaterOrEqual(track.Steps.Length, 4);
            Assert.IsTrue(HasLane(track, "reward_10", RewardTrackLane.Free));
            Assert.IsTrue(HasLane(track, "ad_reward_10", RewardTrackLane.Ad));
            Assert.IsTrue(HasLane(track, "ad_reward_50", RewardTrackLane.Ad));
        }

        [Test]
        public void AdStep_ClaimsOnce_AndEndedForfeits()
        {
            LiveEventData data = ScriptableObject.CreateInstance<LiveEventData>();
            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = "evt_pass";
            so.FindProperty("startUtc").stringValue = "2026-07-01T00:00:00Z";
            so.FindProperty("endUtc").stringValue = "2026-07-10T00:00:00Z";
            so.ApplyModifiedPropertiesWithoutUndo();

            EventRewardTrack track = ScriptableObject.CreateInstance<EventRewardTrack>();
            var tSo = new SerializedObject(track);
            SerializedProperty steps = tSo.FindProperty("steps");
            steps.arraySize = 1;
            steps.GetArrayElementAtIndex(0).FindPropertyRelative("rewardId").stringValue = "ad_reward_10";
            steps.GetArrayElementAtIndex(0).FindPropertyRelative("requiredCurrency").intValue = 10;
            steps.GetArrayElementAtIndex(0).FindPropertyRelative("ticketFragments").intValue = 8;
            steps.GetArrayElementAtIndex(0).FindPropertyRelative("lane").enumValueIndex = (int)RewardTrackLane.Ad;
            tSo.ApplyModifiedPropertiesWithoutUndo();

            var evtSo = new SerializedObject(data);
            evtSo.FindProperty("rewardTrack").objectReferenceValue = track;
            evtSo.ApplyModifiedPropertiesWithoutUndo();

            var clock = new FixedClock { UtcNow = System.DateTime.Parse("2026-07-05T12:00:00Z").ToUniversalTime() };
            var service = new LiveEventService(new LocalLiveEventProvider(null, new[] { data }), clock);
            service.RefreshCatalog();

            var meta = new MetaSaveData();
            meta.EnsureDefaults();
            LiveEventProgress progress = service.GetOrCreateProgress(meta, data);
            progress.currencyBalance = 20;
            Assert.IsTrue(service.TryClaimReward(meta, data, "ad_reward_10"));
            Assert.IsFalse(service.TryClaimReward(meta, data, "ad_reward_10"));

            Object.DestroyImmediate(track);
            Object.DestroyImmediate(data);
        }

        [Test]
        public void SeasonPassPlacement_HasDailyLimit()
        {
            var limits = new AdLimitService();
            limits.BeginRun();
            Assert.Greater(limits.GetRemaining(RewardedAdPlacement.SeasonPassTrack), 0);
            Assert.AreEqual(AdLimitService.SeasonPassTrackPerDay, limits.GetRemaining(RewardedAdPlacement.SeasonPassTrack));
        }

        [Test]
        public void RewardButtonLabel_UsesKoreanSummary_NotInternalId()
        {
            var step = new EventRewardStep
            {
                rewardId = "reward_10",
                requiredCurrency = 10,
                ticketFragments = 5,
                accountXp = 0,
                unlockPassengerId = string.Empty,
                lane = RewardTrackLane.Free,
            };

            string label = EventRewardStepFormatter.FormatButtonLabel(step, claimed: false, database: null);
            StringAssert.Contains("무료", label);
            StringAssert.Contains("조각 5", label);
            StringAssert.DoesNotContain("reward_10", label);
            StringAssert.DoesNotContain("Free", label);

            string claimed = EventRewardStepFormatter.FormatButtonLabel(step, claimed: true, database: null);
            StringAssert.Contains("수령 완료", claimed);
            StringAssert.DoesNotContain("reward_10", claimed);
        }

        private static bool HasLane(EventRewardTrack track, string id, RewardTrackLane lane)
        {
            for (int i = 0; i < track.Steps.Length; i++)
            {
                EventRewardStep step = track.Steps[i];
                if (step != null && step.rewardId == id && step.lane == lane)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
