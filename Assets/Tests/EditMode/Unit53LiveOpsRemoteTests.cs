using System;
using System.IO;
using LastTrain.Integrations;
using LastTrain.LiveOps;
using LastTrain.Run;
using LastTrain.Save;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class Unit53LiveOpsRemoteTests
    {
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "lasttrain-unit53-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            RunSaveSystem.SetServiceForTests(new JsonSaveService(
                Path.Combine(_tempDir, "run.json"),
                Path.Combine(_tempDir, "meta.json")));
            RemoteConfigRuntime.Apply(RemoteConfigSnapshot.Default);
        }

        [TearDown]
        public void TearDown()
        {
            RunSaveSystem.SetServiceForTests(null);
            RemoteConfigRuntime.Apply(RemoteConfigSnapshot.Default);
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }

        private sealed class FixedClock : ILiveEventClock
        {
            public DateTime UtcNow { get; set; }
        }

        [Test]
        public void ServerClock_AppliesOffset_FromServerUtc()
        {
            var local = new FixedClock { UtcNow = new DateTime(2026, 8, 14, 0, 0, 0, DateTimeKind.Utc) };
            var clock = new ServerSyncedLiveEventClock(local);
            clock.SetServerUtc(new DateTime(2026, 8, 14, 3, 0, 0, DateTimeKind.Utc));
            Assert.AreEqual(new DateTime(2026, 8, 14, 3, 0, 0, DateTimeKind.Utc), clock.UtcNow);
            Assert.IsTrue(clock.HasServerOffset);
        }

        [Test]
        public void ServerClock_InvalidIso_KeepsLocalNow()
        {
            var local = new FixedClock { UtcNow = new DateTime(2026, 8, 14, 12, 0, 0, DateTimeKind.Utc) };
            var clock = new ServerSyncedLiveEventClock(local);
            Assert.IsFalse(LiveOpsRuntimeFactory.TrySyncClock(clock, "not-a-date"));
            Assert.AreEqual(local.UtcNow, clock.UtcNow);
            Assert.IsFalse(clock.HasServerOffset);
        }

        [Test]
        public void JsonCatalog_InvalidJson_FallsBackToLocal()
        {
            LiveEventData data = CreateEvent("evt_heat", "2026-01-01T00:00:00Z", "2027-01-01T00:00:00Z");
            var provider = new JsonLiveEventProvider(new LocalLiveEventProvider(null, new[] { data }), "{not-json");
            LiveEventData[] events = provider.LoadEvents();
            Assert.IsFalse(provider.LastRemoteParseSucceeded);
            Assert.AreEqual(1, events.Length);
            Assert.AreEqual("evt_heat", events[0].Id);
            UnityEngine.Object.DestroyImmediate(data);
        }

        [Test]
        public void JsonCatalog_EnabledIds_FiltersLocal()
        {
            LiveEventData keep = CreateEvent("evt_keep", "2026-01-01T00:00:00Z", "2027-01-01T00:00:00Z");
            LiveEventData drop = CreateEvent("evt_drop", "2026-01-01T00:00:00Z", "2027-01-01T00:00:00Z");
            const string json = "{\"disableAll\":false,\"enabledEventIds\":[\"evt_keep\"]}";
            var provider = new JsonLiveEventProvider(
                new LocalLiveEventProvider(null, new[] { keep, drop }),
                json);
            LiveEventData[] events = provider.LoadEvents();
            Assert.IsTrue(provider.LastRemoteParseSucceeded);
            Assert.AreEqual(1, events.Length);
            Assert.AreEqual("evt_keep", events[0].Id);
            UnityEngine.Object.DestroyImmediate(keep);
            UnityEngine.Object.DestroyImmediate(drop);
        }

        [Test]
        public void RemoteKillSwitch_SuppressesActiveEvent()
        {
            LiveEventData data = CreateEvent("evt_heat", "2026-01-01T00:00:00Z", "2027-01-01T00:00:00Z");
            RemoteConfigSnapshot snapshot = Snapshot(
                loadedFromRemote: true,
                liveEventEnabled: false);
            var service = LiveOpsRuntimeFactory.Create(snapshot, new LocalLiveEventProvider(null, new[] { data }));
            service.RefreshCatalog();
            Assert.IsFalse(service.HasActiveEvent);
            UnityEngine.Object.DestroyImmediate(data);
        }

        [Test]
        public void LocalDefaultKillSwitch_DoesNotSuppressHeatwave()
        {
            LiveEventData data = CreateEvent("evt_heat", "2026-01-01T00:00:00Z", "2027-01-01T00:00:00Z");
            RemoteConfigSnapshot snapshot = Snapshot(
                loadedFromRemote: false,
                liveEventEnabled: false);
            var service = LiveOpsRuntimeFactory.Create(snapshot, new LocalLiveEventProvider(null, new[] { data }));
            service.RefreshCatalog();
            Assert.IsTrue(service.HasActiveEvent);
            Assert.AreEqual("evt_heat", service.ActiveEvent.Id);
            UnityEngine.Object.DestroyImmediate(data);
        }

        [Test]
        public void Factory_InvalidServerUtc_StillResolvesLocalCatalog()
        {
            LiveEventData data = CreateEvent("evt_heat", "2026-01-01T00:00:00Z", "2027-01-01T00:00:00Z");
            RemoteConfigSnapshot snapshot = Snapshot(
                loadedFromRemote: false,
                liveEventEnabled: false,
                serverUtc: "bogus");
            var service = LiveOpsRuntimeFactory.Create(snapshot, new LocalLiveEventProvider(null, new[] { data }));
            service.RefreshCatalog();
            Assert.IsTrue(service.HasActiveEvent);
            UnityEngine.Object.DestroyImmediate(data);
        }

        [Test]
        public void Factory_BadRemoteCatalogJson_FallsBackToLocalEvent()
        {
            LiveEventData data = CreateEvent("evt_heat", "2026-01-01T00:00:00Z", "2027-01-01T00:00:00Z");
            RemoteConfigSnapshot snapshot = Snapshot(
                loadedFromRemote: true,
                liveEventEnabled: true,
                useRemoteCatalog: true,
                catalogJson: "not-json");
            var service = LiveOpsRuntimeFactory.Create(snapshot, new LocalLiveEventProvider(null, new[] { data }));
            service.RefreshCatalog();
            Assert.IsTrue(service.HasActiveEvent);
            UnityEngine.Object.DestroyImmediate(data);
        }

        [Test]
        public void RunSaveMapper_LiveEventId_RoundTripsBoost()
        {
            LiveEventData data = CreateEvent("evt_run", "2026-01-01T00:00:00Z", "2027-01-01T00:00:00Z");
            SetBoost(data, "passenger_office_worker", 1.5f);
            RunStartConfig config = RunStartConfig.CreateLiveEventRun(data);
            var run = new RunState();
            run.Initialize(config);
            run.Battle.StartRun();

            RunSaveData save = RunSaveMapper.CreateFromRunState(run);
            Assert.AreEqual("evt_run", save.liveEventId);
            Assert.AreEqual(1.5f, save.liveEventBoostAttackMultiplier, 0.001f);
            Assert.AreEqual(1, save.liveEventBoostedPassengerIds.Length);

            var restored = new RunState();
            restored.Initialize(RunSaveMapper.CreateStartConfigFromSave(save));
            RunSaveMapper.ApplyToRunState(restored, save, null);

            Assert.AreEqual("evt_run", restored.LiveEventId);
            Assert.AreEqual(50f, restored.GetLiveEventAttackPercentBonus("passenger_office_worker"), 0.01f);

            run.Dispose();
            restored.Dispose();
            UnityEngine.Object.DestroyImmediate(data);
        }

        [Test]
        public void ContinueAfterEventEnd_KeepsBoost_AndDoesNotWipeMetaTickets()
        {
            LiveEventData data = CreateEvent("evt_run", "2026-01-01T00:00:00Z", "2026-08-01T00:00:00Z");
            SetBoost(data, "passenger_office_worker", 1.4f);

            var session = new Core.GameSession();
            session.StartNewRun(RunStartConfig.CreateLiveEventRun(data));
            Assert.IsTrue(RunSaveSystem.TrySavePreparing(session));

            var meta = new MetaSaveData();
            meta.EnsureDefaults();
            meta.ticketFragments = 42;
            var progress = new LiveEventProgress { eventId = "evt_run" };
            progress.EnsureDefaults();
            meta.liveEventProgresses = new[] { progress };
            Assert.IsTrue(MetaSaveSystem.Save(meta));

            var endedClock = new FixedClock { UtcNow = new DateTime(2026, 8, 14, 12, 0, 0, DateTimeKind.Utc) };
            var ended = new LiveEventService(new LocalLiveEventProvider(), endedClock);
            ended.RefreshCatalog();
            MetaSaveData loadedMeta = MetaSaveSystem.LoadOrCreate();
            int ticketsBefore = loadedMeta.ticketFragments;
            ended.FinalizeEndedEvents(loadedMeta);
            MetaSaveSystem.Save(loadedMeta);

            Assert.AreEqual(ticketsBefore, loadedMeta.ticketFragments);
            Assert.AreEqual(42, loadedMeta.ticketFragments);
            Assert.IsTrue(RunSaveSystem.TryLoadPreparing(out RunSaveData loaded));
            Assert.AreEqual("evt_run", loaded.liveEventId);

            var restored = new RunState();
            restored.Initialize(RunSaveMapper.CreateStartConfigFromSave(loaded));
            RunSaveMapper.ApplyToRunState(restored, loaded, null);
            Assert.AreEqual("evt_run", restored.LiveEventId);
            Assert.Greater(restored.GetLiveEventAttackPercentBonus("passenger_office_worker"), 0f);
            Assert.IsFalse(ended.HasActiveEvent);

            session.ClearRun();
            restored.Dispose();
            UnityEngine.Object.DestroyImmediate(data);
        }

        private static RemoteConfigSnapshot Snapshot(
            bool loadedFromRemote,
            bool liveEventEnabled,
            bool useRemoteCatalog = false,
            string catalogJson = "",
            string serverUtc = "")
        {
            return new RemoteConfigSnapshot(
                interstitialIntervalSeconds: 180,
                rewardedDailyLimit: 20,
                runsBeforeInterstitial: 3,
                baseSummonCost: 10,
                summonCostIncrease: 2,
                resultRewardMultiplier: 1f,
                freeRevivePerRun: 1,
                liveEventEnabled: liveEventEnabled,
                loadedFromRemote: loadedFromRemote,
                quickRunRewardMultiplier: 1f,
                liveOpsUseRemoteCatalog: useRemoteCatalog,
                liveOpsCatalogJson: catalogJson,
                liveEventServerUtc: serverUtc);
        }

        private static LiveEventData CreateEvent(string id, string start, string end)
        {
            var data = ScriptableObject.CreateInstance<LiveEventData>();
            var so = new SerializedObject(data);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = id;
            so.FindProperty("startUtc").stringValue = start;
            so.FindProperty("endUtc").stringValue = end;
            so.FindProperty("dailyCurrencyCap").intValue = 500;
            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }

        private static void SetBoost(LiveEventData data, string passengerId, float multiplier)
        {
            var so = new SerializedObject(data);
            so.FindProperty("boostedPassengerAttackMultiplier").floatValue = multiplier;
            SerializedProperty boosted = so.FindProperty("boostedPassengerIds");
            boosted.arraySize = 1;
            boosted.GetArrayElementAtIndex(0).stringValue = passengerId;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
