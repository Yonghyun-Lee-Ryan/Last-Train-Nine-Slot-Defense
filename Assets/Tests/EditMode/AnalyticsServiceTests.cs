using System;
using System.Collections.Generic;
using LastTrain.Ads;
using LastTrain.Analytics;
using LastTrain.Core;
using LastTrain.Run;
using NUnit.Framework;

namespace LastTrain.Tests.EditMode
{
    public class AnalyticsServiceTests
    {
        private sealed class RecordingAnalytics : IAnalyticsService
        {
            public readonly List<(string Name, IDictionary<string, object> Params)> Events = new();

            public void Track(string eventName, IDictionary<string, object> parameters = null)
            {
                Events.Add((eventName, parameters));
            }

            public void Track(AnalyticsEvent analyticsEvent)
            {
                Events.Add((analyticsEvent.Name, analyticsEvent.Parameters));
            }
        }

        [Test]
        public void Track_MergesCommonContextParameters()
        {
            var recorder = new RecordingAnalytics();
            var context = new AnalyticsContext("session-fixed");
            context.BindRun("run-1", "line1", "hard");
            context.StationIndex = 2;
            context.WaveIndex = 1;
            var analytics = new AnalyticsCoordinator(recorder, context);

            analytics.Track("custom_event", new Dictionary<string, object> { ["foo"] = 1 });

            Assert.AreEqual(1, recorder.Events.Count);
            Assert.AreEqual("custom_event", recorder.Events[0].Name);
            IDictionary<string, object> p = recorder.Events[0].Params;
            Assert.AreEqual("session-fixed", p["session_id"]);
            Assert.AreEqual("run-1", p["run_id"]);
            Assert.AreEqual("line1", p["route_id"]);
            Assert.AreEqual("hard", p["difficulty_id"]);
            Assert.AreEqual(2, p["station_index"]);
            Assert.AreEqual(1, p["wave_index"]);
            Assert.AreEqual(1, p["foo"]);
        }

        [Test]
        public void EventNames_AreSnakeCase()
        {
            Assert.AreEqual("run_started", AnalyticsEventNames.RunStarted);
            Assert.AreEqual("rewarded_ad_completed", AnalyticsEventNames.RewardedAdCompleted);
            Assert.AreEqual("passenger_merged", AnalyticsEventNames.PassengerMerged);
            Assert.AreEqual("achievement_unlocked", AnalyticsEventNames.AchievementUnlocked);
        }

        [Test]
        public void SafeAnalytics_SwallowsInnerExceptions()
        {
            var throwing = new ThrowingAnalytics();
            var safe = new SafeAnalyticsService(throwing, _ => { });
            Assert.DoesNotThrow(() => safe.Track("x", null));
        }

        [Test]
        public void TrackRunEnded_FailureIncludesCauseAndHp()
        {
            var recorder = new RecordingAnalytics();
            var analytics = new AnalyticsCoordinator(recorder);
            var session = new GameSession();
            session.StartNewRun();
            analytics.BindRun(session.RunState);

            RunResult result = session.EndRun(RunEndReason.Defeat, isVictory: false);
            analytics.TrackRunEnded(result, null);

            Assert.AreEqual(AnalyticsEventNames.RunFailed, recorder.Events[0].Name);
            Assert.AreEqual(RunEndReason.Defeat.ToString(), recorder.Events[0].Params["end_reason"]);
            Assert.IsTrue(recorder.Events[0].Params.ContainsKey("train_hp"));
        }

        [Test]
        public void AdCoordinator_TracksOfferedStartedCompleted()
        {
            var recorder = new RecordingAnalytics();
            var analytics = new AnalyticsCoordinator(recorder);
            var limits = new AdLimitService { Cooldown = TimeSpan.Zero };
            limits.BeginRun();
            var ads = new AdCoordinator(
                new MockAdService { AutoResult = AdResult.Completed },
                limits,
                new AdRewardService(limits))
            {
                Analytics = analytics,
            };

            int grants = 0;
            ads.ShowRewarded(RewardedAdPlacement.FreeSummon, () => grants++);

            Assert.AreEqual(1, grants);
            CollectionAssert.Contains(
                recorder.Events.ConvertAll(e => e.Name),
                AnalyticsEventNames.RewardedAdOffered);
            CollectionAssert.Contains(
                recorder.Events.ConvertAll(e => e.Name),
                AnalyticsEventNames.RewardedAdStarted);
            CollectionAssert.Contains(
                recorder.Events.ConvertAll(e => e.Name),
                AnalyticsEventNames.RewardedAdCompleted);
        }

        [Test]
        public void NoOpAnalytics_DoesNotThrow()
        {
            var noop = new NoOpAnalyticsService();
            Assert.DoesNotThrow(() => noop.Track("x", null));
            Assert.DoesNotThrow(() => noop.Track(new AnalyticsEvent("y")));
        }

        private sealed class ThrowingAnalytics : IAnalyticsService
        {
            public void Track(string eventName, IDictionary<string, object> parameters = null)
            {
                throw new InvalidOperationException("boom");
            }

            public void Track(AnalyticsEvent analyticsEvent)
            {
                throw new InvalidOperationException("boom");
            }
        }
    }
}
