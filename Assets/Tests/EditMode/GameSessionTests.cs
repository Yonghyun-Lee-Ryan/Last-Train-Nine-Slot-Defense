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
            Assert.AreEqual(8, result.FinalCoins);
            Assert.AreSame(result, _session.LastResult);
            Assert.IsFalse(_session.HasActiveRun);
        }

        [Test]
        public void TrainDestroyed_AutoEndsRunAsDefeat()
        {
            _session.StartNewRun();
            RunEndReason endReason = RunEndReason.None;

            _session.RunEnded += result => endReason = result.EndReason;
            _session.RunState.Train.ApplyDamage(100);

            Assert.AreEqual(RunEndReason.Defeat, endReason);
            Assert.IsFalse(_session.HasActiveRun);
        }

        [Test]
        public void StartNewRun_FiresRunStartedEvent()
        {
            bool fired = false;
            _session.RunStarted += _ => fired = true;

            _session.StartNewRun();

            Assert.IsTrue(fired);
        }
    }
}
