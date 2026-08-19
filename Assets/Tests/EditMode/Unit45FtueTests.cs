using LastTrain.Analytics;
using LastTrain.Data;
using LastTrain.Save;
using LastTrain.Tutorial;
using NUnit.Framework;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class Unit45FtueTests
    {
        [Test]
        public void GameDatabase_TutorialSteps_AreQuickstartFive()
        {
            GameDatabase database = GameDatabaseLocator.Load();
            Assume.That(database, Is.Not.Null);
            Assert.AreEqual(5, database.TutorialSteps.Count);
            Assert.AreEqual(TutorialStepKind.SummonPassenger, database.TutorialSteps[0].StepKind);
            Assert.AreEqual(TutorialStepKind.PlacePassenger, database.TutorialSteps[1].StepKind);
            Assert.AreEqual(TutorialStepKind.ObserveAutoAttack, database.TutorialSteps[2].StepKind);
            Assert.AreEqual(TutorialStepKind.MergePassengers, database.TutorialSteps[3].StepKind);
            Assert.AreEqual(TutorialStepKind.SelectAbility, database.TutorialSteps[4].StepKind);
            Assert.IsTrue(database.TutorialSteps[2].Body.Contains("체력")
                          || database.TutorialSteps[2].Body.Contains("패배"));
        }

        [Test]
        public void PostSkipGuide_ShouldShowOnlyAfterSkipUntilDone()
        {
            var meta = new MetaSaveData();
            meta.EnsureDefaults();
            Assert.IsFalse(PostSkipGuideService.ShouldShow(meta));

            TutorialProgressService.MarkSkipped(meta);
            Assert.IsTrue(PostSkipGuideService.ShouldShow(meta));
            Assert.AreEqual(2, PostSkipGuideService.Tips.Length);
            Assert.AreEqual("SummonButton", PostSkipGuideService.Tips[0].UiTargetId);
            Assert.AreEqual("ReadyButton", PostSkipGuideService.Tips[1].UiTargetId);

            PostSkipGuideService.MarkDone(meta);
            Assert.IsFalse(PostSkipGuideService.ShouldShow(meta));
        }

        [Test]
        public void Skip_ResetsPostSkipGuideSoFirstBattleCanShowTips()
        {
            var meta = new MetaSaveData();
            meta.EnsureDefaults();
            meta.tutorialPostSkipGuideDone = true;
            TutorialProgressService.MarkSkipped(meta);
            Assert.IsFalse(meta.tutorialPostSkipGuideDone);
            Assert.IsTrue(PostSkipGuideService.ShouldShow(meta));
        }

        [Test]
        public void StateMachine_SkipAll_MarksSkippedAndTracksFunnelEvents()
        {
            TutorialStepData step = ScriptableObject.CreateInstance<TutorialStepData>();
            step.EditorSet(
                "summon",
                TutorialStepKind.SummonPassenger,
                "소환",
                "body",
                TutorialWaitEvent.SummonOpened,
                "SummonButton",
                TutorialInputMask.Summon,
                true);

            var meta = new MetaSaveData();
            meta.EnsureDefaults();
            bool skipped = false;
            var machine = new TutorialStateMachine(new[] { step });
            machine.Skipped += () => skipped = true;
            machine.StartOrResume(meta);
            machine.SkipAll(meta);

            Assert.IsTrue(skipped);
            Assert.IsTrue(machine.IsFinished);
            Assert.IsTrue(meta.tutorialSkipped);
            Assert.IsTrue(meta.tutorialCompleted);
            Assert.IsTrue(PostSkipGuideService.ShouldShow(meta));
            Assert.AreEqual("tutorial_skipped", AnalyticsEventNames.TutorialSkipped);
            Object.DestroyImmediate(step);
        }

        [Test]
        public void PostSkipTips_CoverSummonAndReadyWithoutFullOverlay()
        {
            Assert.AreEqual(2, PostSkipGuideService.Tips.Length);
            StringAssert.Contains("소환", PostSkipGuideService.Tips[0].Message);
            StringAssert.Contains("준비", PostSkipGuideService.Tips[1].Message);
        }
    }
}
