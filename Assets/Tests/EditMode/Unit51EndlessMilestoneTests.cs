using LastTrain.Endless;
using LastTrain.Save;
using NUnit.Framework;

namespace LastTrain.Tests.EditMode
{
    public class Unit51EndlessMilestoneTests
    {
        [Test]
        public void Track_HasTenSteps()
        {
            EndlessMilestoneTrack track = EndlessMilestoneCatalog.Load();
            Assert.IsNotNull(track);
            Assert.AreEqual(10, track.Steps.Length);
        }

        [Test]
        public void Claim_OnceOnly_AndKeepsBestScore()
        {
            var meta = new MetaSaveData();
            meta.EnsureDefaults();
            meta.endlessBestStationReached = 10;
            meta.endlessBestScore = 1200;

            EndlessMilestoneTrack track = EndlessMilestoneCatalog.Load();
            EndlessMilestoneStep station10 = Find(track, "ms_station_10");
            Assert.IsTrue(EndlessProgressService.IsMilestoneReached(meta, station10));
            Assert.IsTrue(EndlessProgressService.TryClaimMilestone(meta, station10));
            int tickets = meta.ticketFragments;
            Assert.IsFalse(EndlessProgressService.TryClaimMilestone(meta, station10));
            Assert.AreEqual(tickets, meta.ticketFragments);
            Assert.AreEqual(1200, meta.endlessBestScore);
        }

        [Test]
        public void ScoreMilestone_IgnoresStationOnlyProgress()
        {
            var meta = new MetaSaveData();
            meta.EnsureDefaults();
            meta.endlessBestStationReached = 30;
            meta.endlessBestScore = 100;

            EndlessMilestoneTrack track = EndlessMilestoneCatalog.Load();
            EndlessMilestoneStep score500 = Find(track, "ms_score_500");
            Assert.IsFalse(EndlessProgressService.IsMilestoneReached(meta, score500));
            Assert.IsTrue(EndlessProgressService.IsMilestoneReached(meta, Find(track, "ms_station_30")));
        }

        [Test]
        public void LocalBest_UpdatesWithoutClaim()
        {
            var meta = new MetaSaveData();
            meta.EnsureDefaults();
            meta.endlessBestScore = 50;
            meta.endlessBestStationReached = 2;
            Assert.IsFalse(EndlessProgressService.HasClaimedMilestone(meta, "ms_station_5"));
            Assert.AreEqual(50, meta.endlessBestScore);
        }

        private static EndlessMilestoneStep Find(EndlessMilestoneTrack track, string id)
        {
            for (int i = 0; i < track.Steps.Length; i++)
            {
                if (track.Steps[i] != null && track.Steps[i].id == id)
                {
                    return track.Steps[i];
                }
            }

            return null;
        }
    }
}
