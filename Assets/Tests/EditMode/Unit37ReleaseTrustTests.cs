using System;
using System.IO;
using LastTrain.Integrations;
using LastTrain.Leaderboard;
using LastTrain.Release;
using LastTrain.Save;
using NUnit.Framework;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class Unit37ReleaseTrustTests
    {
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "lasttrain-unit37-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            RunSaveSystem.SetServiceForTests(new JsonSaveService(
                Path.Combine(_tempDir, "run.json"),
                Path.Combine(_tempDir, "meta.json")));
            InAppReviewPromptService.ResetForTests();
            InAppReviewPromptService.UtcNowProvider = () => new DateTime(2026, 8, 13, 12, 0, 0, DateTimeKind.Utc);
            PlayerPrefs.DeleteKey("lasttrain.review.last_prompt_utc");
            PlayerPrefs.DeleteKey("lasttrain.review.prompt_count");
        }

        [TearDown]
        public void TearDown()
        {
            InAppReviewPromptService.UtcNowProvider = null;
            InAppReviewPromptService.ResetForTests();
            RunSaveSystem.SetServiceForTests(null);
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }

        [Test]
        public void AppReleaseConfig_PrivacyUrl_IsNotExampleDotCom()
        {
            AppReleaseConfig config = Resources.Load<AppReleaseConfig>("AppReleaseConfig");
            Assert.IsNotNull(config);
            Assert.IsFalse(string.IsNullOrWhiteSpace(config.PrivacyPolicyUrl));
            Assert.IsFalse(config.PrivacyPolicyUrl.StartsWith("https://example.com", StringComparison.Ordinal));
            Assert.IsTrue(config.PrivacyPolicyUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase));
        }

        [Test]
        public void PlayerDataDeletion_RemovesMetaAndRunSaves()
        {
            MetaSaveData meta = MetaSaveSystem.LoadOrCreate();
            meta.ticketFragments = 99;
            Assert.IsTrue(MetaSaveSystem.Save(meta));
            Assert.IsTrue(RunSaveSystem.TryLoadMeta(out _));

            var privacy = new PrivacyConsentService();
            privacy.Initialize(autoGrantInEditor: true);
            privacy.GrantAllForTesting();

            bool deleted = PlayerDataDeletionService.DeleteAllLocalData(privacy, null);
            Assert.IsTrue(deleted);
            Assert.IsFalse(RunSaveSystem.TryLoadMeta(out _));
            Assert.IsFalse(privacy.CanRequestAds);
        }

        [Test]
        public void InAppReview_RequiresTwoClears_AndRespectsCooldown()
        {
            var meta = new MetaSaveData();
            meta.EnsureDefaults();
            meta.difficultyRecords = new[]
            {
                new MetaDifficultyRecord { difficultyId = "normal", clearCount = 1 },
            };
            Assert.IsFalse(InAppReviewPromptService.CanPrompt(meta));

            meta.difficultyRecords[0].clearCount = 2;
            Assert.IsTrue(InAppReviewPromptService.CanPrompt(meta));
            Assert.IsTrue(InAppReviewPromptService.TryPrompt(meta));
            Assert.IsFalse(InAppReviewPromptService.CanPrompt(meta));

            InAppReviewPromptService.UtcNowProvider = () =>
                new DateTime(2026, 8, 13, 12, 0, 0, DateTimeKind.Utc) + InAppReviewPromptService.Cooldown;
            Assert.IsTrue(InAppReviewPromptService.CanPrompt(meta));
        }

        [Test]
        public void LocalLeaderboardService_DoesNotRequireNetwork()
        {
            var service = new LocalLeaderboardService();
            var record = new LeaderboardRunRecord { runId = "run-1", score = 10 };
            Assert.AreEqual(LeaderboardSubmitResult.Success, service.Submit(record));
            Assert.AreEqual(1, service.SubmitCount);
        }
    }
}
