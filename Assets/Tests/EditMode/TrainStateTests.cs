using LastTrain.Run;
using NUnit.Framework;

namespace LastTrain.Tests.EditMode
{
    public class TrainStateTests
    {
        [Test]
        public void ApplyDamage_ReducesHpAndFiresEvent()
        {
            var train = new TrainState(100, 100);
            int reportedCurrent = -1;
            int reportedMax = -1;
            train.HpChanged += (current, max) =>
            {
                reportedCurrent = current;
                reportedMax = max;
            };

            train.ApplyDamage(25);

            Assert.AreEqual(75, train.CurrentHp);
            Assert.AreEqual(75, reportedCurrent);
            Assert.AreEqual(100, reportedMax);
            Assert.IsFalse(train.IsDestroyed);
        }

        [Test]
        public void ApplyDamage_ToZero_FiresDestroyed()
        {
            var train = new TrainState(100, 100);
            bool destroyed = false;
            train.Destroyed += () => destroyed = true;

            train.ApplyDamage(100);

            Assert.AreEqual(0, train.CurrentHp);
            Assert.IsTrue(train.IsDestroyed);
            Assert.IsTrue(destroyed);
        }

        [Test]
        public void Heal_DoesNotExceedMaxHp()
        {
            var train = new TrainState(100, 50);
            train.Heal(80);

            Assert.AreEqual(100, train.CurrentHp);
        }
    }
}
