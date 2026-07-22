using LastTrain.Core;
using LastTrain.Run;
using NUnit.Framework;

namespace LastTrain.Tests.EditMode
{
    public class GameSessionTests
    {
        private GameSession _session;

        [SetUp]
        public void SetUp()
        {
            _session = new GameSession();
        }

        [Test]
        public void StartNewRun_InitializesRunState()
        {
            RunState run = _session.StartNewRun();

            Assert.IsTrue(_session.HasActiveRun);
            Assert.AreSame(run, _session.RunState);
            Assert.AreEqual(RunPhase.Preparing, run.Battle.CurrentPhase);
            Assert.AreEqual(100, run.Train.CurrentHp);
        }

        [Test]
        public void EndRun_ProducesRunResult()
        {
            _session.StartNewRun();
            _session.RunState.RecordEnemyKill(8);

            RunResult result = _session.EndRun(RunEndReason.Victory, isVictory: true);

            Assert.IsTrue(result.IsVictory);
            Assert.AreEqual(58, result.FinalCoins);
            Assert.AreSame(result, _session.LastResult);
            Assert.IsFalse(_session.HasActiveRun);
        }

        [Test]
        public void TrainDestroyed_WithoutReviveHandler_AutoEndsAsDefeat()
        {
            _session.StartNewRun();
            RunEndReason endReason = RunEndReason.None;

            _session.RunEnded += result => endReason = result.EndReason;
            _session.RunState.Train.ApplyDamage(100);

            Assert.AreEqual(RunEndReason.Defeat, endReason);
            Assert.IsFalse(_session.HasActiveRun);
            Assert.IsFalse(_session.IsPendingDefeat);
        }

        [Test]
        public void TrainDestroyed_WithReviveHandler_WaitsUntilDecline()
        {
            _session.StartNewRun();
            bool reviveOffered = false;
            _session.ReviveOffered += () => reviveOffered = true;

            _session.RunState.Train.ApplyDamage(100);

            Assert.IsTrue(reviveOffered);
            Assert.IsTrue(_session.IsPendingDefeat);
            Assert.IsTrue(_session.HasActiveRun);

            _session.DeclineReviveAndEnd();

            Assert.IsFalse(_session.HasActiveRun);
            Assert.IsFalse(_session.IsPendingDefeat);
            Assert.AreEqual(RunEndReason.Defeat, _session.LastResult.EndReason);
        }

        [Test]
        public void MarkReviveUsed_ClearsAvailabilityFlag()
        {
            _session.StartNewRun();
            _session.ReviveOffered += () => { };
            _session.RunState.Train.ApplyDamage(100);

            Assert.IsTrue(_session.IsPendingDefeat);
            Assert.AreEqual(0, _session.RunState.Train.CurrentHp);

            _session.RunState.Train.SetCurrentHp(35);
            _session.MarkReviveUsed();
            _session.ClearPendingDefeat();

            Assert.IsFalse(_session.IsPendingDefeat);
            Assert.IsTrue(_session.HasActiveRun);
            Assert.AreEqual(35, _session.RunState.Train.CurrentHp);
            Assert.IsFalse(_session.ReviveAvailableThisRun);
        }

        [Test]
        public void StartNewRun_FiresRunStartedEvent()
        {
            bool fired = false;
            _session.RunStarted += _ => fired = true;

            _session.StartNewRun();

            Assert.IsTrue(fired);
        }

        [Test]
        public void EndRun_CalledTwice_IsIdempotent()
        {
            _session.StartNewRun();
            _session.RunState.RecordEnemyKill(3);

            int endedCount = 0;
            _session.RunEnded += _ => endedCount++;

            Assert.DoesNotThrow(() => _session.EndRun(RunEndReason.Victory, isVictory: true));
            Assert.AreEqual(1, endedCount);

            Assert.DoesNotThrow(() => _session.EndRun(RunEndReason.Victory, isVictory: true));
            Assert.AreEqual(1, endedCount);
            Assert.IsFalse(_session.HasActiveRun);
        }
    }
}
