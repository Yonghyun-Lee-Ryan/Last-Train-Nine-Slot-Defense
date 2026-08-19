using LastTrain.Save;
using LastTrain.Tutorial;
using NUnit.Framework;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class TutorialProgressServiceTests
    {
        [Test]
        public void ShouldOfferTutorial_WhenNotCompleted()
        {
            var meta = new MetaSaveData();
            meta.EnsureDefaults();
            Assert.IsTrue(TutorialProgressService.ShouldOfferTutorial(meta));
        }

        [Test]
        public void Skip_MarksCompletedAndSkippableRestart()
        {
            var meta = new MetaSaveData();
            meta.EnsureDefaults();
            TutorialProgressService.MarkSkipped(meta);
            Assert.IsFalse(TutorialProgressService.ShouldOfferTutorial(meta));
            Assert.IsTrue(TutorialProgressService.CanRestart(meta));
            Assert.IsTrue(PostSkipGuideService.ShouldShow(meta));
        }

        [Test]
        public void StateMachine_AdvancesOnMatchingEvent_AndPersistsStep()
        {
            TutorialStepData a = ScriptableObject.CreateInstance<TutorialStepData>();
            a.EditorSet(
                "a",
                TutorialStepKind.SummonPassenger,
                "A",
                "body",
                TutorialWaitEvent.SummonOpened,
                "SummonButton",
                TutorialInputMask.Summon,
                true);
            TutorialStepData b = ScriptableObject.CreateInstance<TutorialStepData>();
            b.EditorSet(
                "b",
                TutorialStepKind.PlacePassenger,
                "B",
                "body",
                TutorialWaitEvent.PassengerPlaced,
                "SummonButton",
                TutorialInputMask.Summon,
                true);

            var meta = new MetaSaveData();
            meta.EnsureDefaults();
            var machine = new TutorialStateMachine(new[] { a, b });
            machine.StartOrResume(meta);
            Assert.AreEqual(0, machine.CurrentIndex);
            Assert.IsTrue(machine.Allows(TutorialInputMask.Summon));

            machine.Notify(TutorialWaitEvent.SummonOpened);
            Assert.AreEqual(1, machine.CurrentIndex);

            machine.Notify(TutorialWaitEvent.PassengerPlaced);
            Assert.IsTrue(machine.IsFinished);
        }

        [Test]
        public void StateMachine_IgnoresUnrelatedEvents()
        {
            TutorialStepData step = ScriptableObject.CreateInstance<TutorialStepData>();
            step.EditorSet(
                "merge",
                TutorialStepKind.MergePassengers,
                "M",
                "body",
                TutorialWaitEvent.PassengersMerged,
                "Grid",
                TutorialInputMask.GridDrag,
                true);

            var meta = new MetaSaveData();
            meta.EnsureDefaults();
            var machine = new TutorialStateMachine(new[] { step });
            machine.StartOrResume(meta);
            machine.Notify(TutorialWaitEvent.SummonOpened);
            Assert.AreEqual(0, machine.CurrentIndex);
            Assert.IsTrue(machine.IsActive);
        }
    }
}
