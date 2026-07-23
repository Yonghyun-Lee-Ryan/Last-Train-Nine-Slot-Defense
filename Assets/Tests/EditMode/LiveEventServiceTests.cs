using System;
using LastTrain.LiveOps;
using LastTrain.Run;
using LastTrain.Save;
using NUnit.Framework;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class LiveEventServiceTests
    {
        private sealed class FixedClock : ILiveEventClock
        {
            public DateTime UtcNow { get; set; }
        }

        [Test]
        public void MissingCatalog_FallsBackToNoEvent()
        {
            var service = new LiveEventService(new LocalLiveEventProvider(), new LocalLiveEventClock());
            service.RefreshCatalog();
            Assert.IsFalse(service.HasActiveEvent);
            Assert.IsNull(service.ActiveEvent);
        }

        [Test]
        public void ActiveWindow_ResolvesEvent()
        {
            LiveEventData data = CreateEvent(
                "evt_heat",
                "2026-07-01T00:00:00Z",
                "2026-07-10T00:00:00Z");
            var clock = new FixedClock { UtcNow = DateTime.Parse("2026-07-05T12:00:00Z").ToUniversalTime() };
            var service = new LiveEventService(new LocalLiveEventProvider(null, new[] { data }), clock);
            service.RefreshCatalog();

            Assert.IsTrue(service.HasActiveEvent);
            Assert.AreEqual("evt_heat", service.ActiveEvent.Id);
            Assert.AreEqual(LiveEventPhase.Active, service.GetPhase(data));

            UnityEngine.Object.DestroyImmediate(data);
        }

        [Test]
        public void ClaimReward_PreventsDuplicates()
        {
            LiveEventData data = CreateEvent(
                "evt_claim",
                "2026-07-01T00:00:00Z",
                "2026-07-10T00:00:00Z");
            EventRewardTrack track = ScriptableObject.CreateInstance<EventRewardTrack>();
            SetRewardTrack(track, new EventRewardStep
            {
                rewardId = "r1",
                requiredCurrency = 10,
                ticketFragments = 5,
                accountXp = 10,
            });
            SetEventTrack(data, track);

            var clock = new FixedClock { UtcNow = DateTime.Parse("2026-07-05T12:00:00Z").ToUniversalTime() };
            var service = new LiveEventService(new LocalLiveEventProvider(null, new[] { data }), clock);
            service.RefreshCatalog();

            var meta = new MetaSaveData();
            meta.EnsureDefaults();
            int ticketsBefore = meta.ticketFragments;
            int xpBefore = meta.accountXp;
            LiveEventProgress progress = service.GetOrCreateProgress(meta, data);
            progress.currencyBalance = 100;

            Assert.IsTrue(service.TryClaimReward(meta, data, "r1"));
            Assert.Greater(meta.ticketFragments, ticketsBefore);
            Assert.Greater(meta.accountXp, xpBefore);
            Assert.IsFalse(service.TryClaimReward(meta, data, "r1"));

            UnityEngine.Object.DestroyImmediate(track);
            UnityEngine.Object.DestroyImmediate(data);
        }

        [Test]
        public void CreateLiveEventRun_AppliesRouteAndBoost()
        {
            LiveEventData data = CreateEvent(
                "evt_run",
                "2026-07-01T00:00:00Z",
                "2026-07-10T00:00:00Z");
            SetBoost(data, "passenger_office_worker", 1.5f);

            RunStartConfig config = RunStartConfig.CreateLiveEventRun(data);
            Assert.AreEqual("evt_run", config.LiveEventId);
            Assert.AreEqual(1.5f, config.LiveEventBoostAttackMultiplier, 0.001f);
            Assert.AreEqual(1, config.LiveEventBoostedPassengerIds.Length);

            var run = new RunState();
            run.Initialize(config);
            Assert.AreEqual("evt_run", run.LiveEventId);
            Assert.AreEqual(50f, run.GetLiveEventAttackPercentBonus("passenger_office_worker"), 0.01f);
            Assert.AreEqual(0f, run.GetLiveEventAttackPercentBonus("passenger_nurse"));

            run.Dispose();
            UnityEngine.Object.DestroyImmediate(data);
        }

        [Test]
        public void FromResourcesCatalog_DoesNotThrow()
        {
            var provider = LocalLiveEventProvider.FromResources();
            var service = new LiveEventService(provider, new LocalLiveEventClock());
            Assert.DoesNotThrow(() => service.RefreshCatalog());
        }

        [Test]
        public void EndedEvent_ForfeitsUnclaimed_AndDoesNotCorruptMeta()
        {
            LiveEventData data = CreateEvent(
                "evt_ended",
                "2026-01-01T00:00:00Z",
                "2026-01-02T00:00:00Z");
            data.GetType(); // keep reference
            SetEndedPolicy(data, EndedRewardPolicy.ForfeitUnclaimed);

            EventRewardTrack track = ScriptableObject.CreateInstance<EventRewardTrack>();
            SetRewardTrack(track, new EventRewardStep { rewardId = "r1", requiredCurrency = 0 });
            SetEventTrack(data, track);

            var clock = new FixedClock { UtcNow = DateTime.Parse("2026-01-03T00:00:00Z").ToUniversalTime() };
            var service = new LiveEventService(new LocalLiveEventProvider(null, new[] { data }), clock);
            service.RefreshCatalog();

            var meta = new MetaSaveData();
            meta.EnsureDefaults();
            meta.ticketFragments = 42;
            LiveEventProgress progress = service.GetOrCreateProgress(meta, data);
            progress.currencyBalance = 50;

            Assert.AreEqual(LiveEventPhase.Ended, service.GetPhase(data));
            Assert.IsFalse(service.TryClaimReward(meta, data, "r1"));
            service.FinalizeEndedEvents(meta);
            Assert.IsTrue(progress.finalized);
            Assert.AreEqual(42, meta.ticketFragments);

            UnityEngine.Object.DestroyImmediate(track);
            UnityEngine.Object.DestroyImmediate(data);
        }

        [Test]
        public void DailyCap_LimitsCurrencyEarn()
        {
            LiveEventData data = CreateEvent(
                "evt_cap",
                "2026-07-01T00:00:00Z",
                "2026-07-10T00:00:00Z");
            SetDailyCap(data, 30);
            EventCurrencyData currency = ScriptableObject.CreateInstance<EventCurrencyData>();
            SetCurrency(data, currency);

            var clock = new FixedClock { UtcNow = DateTime.Parse("2026-07-05T12:00:00Z").ToUniversalTime() };
            var service = new LiveEventService(new LocalLiveEventProvider(null, new[] { data }), clock);
            service.RefreshCatalog();
            var meta = new MetaSaveData();
            meta.EnsureDefaults();

            Assert.AreEqual(30, service.TryEarnCurrency(meta, data, 100));
            Assert.AreEqual(0, service.TryEarnCurrency(meta, data, 10));

            UnityEngine.Object.DestroyImmediate(currency);
            UnityEngine.Object.DestroyImmediate(data);
        }

        private static void SetBoost(LiveEventData data, string passengerId, float multiplier)
        {
            var so = new UnityEditor.SerializedObject(data);
            so.FindProperty("boostedPassengerAttackMultiplier").floatValue = multiplier;
            var boosted = so.FindProperty("boostedPassengerIds");
            boosted.arraySize = 1;
            boosted.GetArrayElementAtIndex(0).stringValue = passengerId;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static LiveEventData CreateEvent(string id, string start, string end)
        {
            var data = ScriptableObject.CreateInstance<LiveEventData>();
            var so = new UnityEditor.SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = id;
            so.FindProperty("startUtc").stringValue = start;
            so.FindProperty("endUtc").stringValue = end;
            so.FindProperty("dailyCurrencyCap").intValue = 500;
            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }

        private static void SetRewardTrack(EventRewardTrack track, EventRewardStep step)
        {
            var so = new UnityEditor.SerializedObject(track);
            so.FindProperty("id").stringValue = "track";
            var steps = so.FindProperty("steps");
            steps.arraySize = 1;
            steps.GetArrayElementAtIndex(0).FindPropertyRelative("rewardId").stringValue = step.rewardId;
            steps.GetArrayElementAtIndex(0).FindPropertyRelative("requiredCurrency").intValue = step.requiredCurrency;
            steps.GetArrayElementAtIndex(0).FindPropertyRelative("ticketFragments").intValue = step.ticketFragments;
            steps.GetArrayElementAtIndex(0).FindPropertyRelative("accountXp").intValue = step.accountXp;
            steps.GetArrayElementAtIndex(0).FindPropertyRelative("unlockPassengerId").stringValue =
                step.unlockPassengerId ?? string.Empty;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetEventTrack(LiveEventData data, EventRewardTrack track)
        {
            var so = new UnityEditor.SerializedObject(data);
            so.FindProperty("rewardTrack").objectReferenceValue = track;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetEndedPolicy(LiveEventData data, EndedRewardPolicy policy)
        {
            var so = new UnityEditor.SerializedObject(data);
            so.FindProperty("endedRewardPolicy").enumValueIndex = (int)policy;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetDailyCap(LiveEventData data, int cap)
        {
            var so = new UnityEditor.SerializedObject(data);
            so.FindProperty("dailyCurrencyCap").intValue = cap;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetCurrency(LiveEventData data, EventCurrencyData currency)
        {
            var so = new UnityEditor.SerializedObject(data);
            so.FindProperty("eventCurrency").objectReferenceValue = currency;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
