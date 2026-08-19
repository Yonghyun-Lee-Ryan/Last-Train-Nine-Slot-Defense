using LastTrain.Run;
using LastTrain.Save;
using LastTrain.UI;
using NUnit.Framework;

namespace LastTrain.Tests.EditMode
{
    public class AchievementVisibilityResolverTests
    {
        [Test]
        public void BuildEntry_Locked_HidesDisplayName()
        {
            var meta = new MetaSaveData();
            meta.EnsureDefaults();

            AchievementEntryView view = AchievementVisibilityResolver.BuildEntry(
                meta,
                MetaProgressionDefaults.AchFirstVictory);

            Assert.IsFalse(view.IsUnlocked);
            Assert.AreEqual(AchievementVisibilityResolver.LockedTitle, view.Title);
            Assert.AreEqual(AchievementVisibilityResolver.LockedDetail, view.Detail);
            StringAssert.DoesNotContain("첫 도착", view.Title);
        }

        [Test]
        public void BuildEntry_Unlocked_ShowsCatalogName()
        {
            var meta = new MetaSaveData();
            meta.EnsureDefaults();
            meta.unlockedAchievementIds = new[] { MetaProgressionDefaults.AchFirstVictory };

            AchievementEntryView view = AchievementVisibilityResolver.BuildEntry(
                meta,
                MetaProgressionDefaults.AchFirstVictory);

            Assert.IsTrue(view.IsUnlocked);
            Assert.AreEqual("첫 도착", view.Title);
            StringAssert.Contains("종착역", view.Detail);
        }

        [Test]
        public void TryApplyRunResult_DoesNotUnlockSameAchievementTwice()
        {
            var meta = new MetaSaveData();
            meta.EnsureDefaults();

            RunResult first = CreateVictory("run-ach-1");
            MetaProgressionService.TryApplyRunResult(meta, first);
            int countAfterFirst = meta.unlockedAchievementIds.Length;
            Assert.Contains(MetaProgressionDefaults.AchFirstVictory, meta.unlockedAchievementIds);

            RunResult second = CreateVictory("run-ach-2");
            MetaApplyResult apply = MetaProgressionService.TryApplyRunResult(meta, second);

            Assert.IsTrue(apply.Applied);
            Assert.AreEqual(0, apply.Breakdown.NewlyUnlockedAchievements.Count);
            Assert.AreEqual(countAfterFirst, meta.unlockedAchievementIds.Length);
        }

        [Test]
        public void Catalog_CoversAllKnownAchievementIds()
        {
            Assert.AreEqual(5, AchievementCatalog.AllIds.Length);
            for (int i = 0; i < AchievementCatalog.AllIds.Length; i++)
            {
                Assert.IsTrue(
                    AchievementCatalog.TryGetDisplay(AchievementCatalog.AllIds[i], out _, out _),
                    AchievementCatalog.AllIds[i]);
            }
        }

        private static RunResult CreateVictory(string runId)
        {
            return new RunResult(
                runId: runId,
                lineId: "line_default",
                isVictory: true,
                endReason: RunEndReason.Victory,
                reachedStationIndex: 1,
                completedStationCount: 1,
                enemiesKilled: 0,
                bossesKilled: 0,
                mergeCount: 0,
                highestPassengerStar: 1,
                remainingTrainHp: 50,
                trainMaxHp: 100,
                finalCoins: 0,
                totalCoinsEarned: 0,
                totalCoinsSpent: 0,
                passengersSummoned: 0,
                passengersSold: 0,
                abilityCardsSelected: 0);
        }
    }
}
