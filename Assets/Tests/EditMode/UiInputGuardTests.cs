using LastTrain.UI;
using NUnit.Framework;
using UnityEngine;

namespace LastTrain.Tests.EditMode
{
    public class UiInputGuardTests
    {
        [Test]
        public void TryAcquire_SecondCallWhileLocked_Fails()
        {
            var guard = new UiInputGuard(1f);

            Assert.IsTrue(guard.TryAcquire());
            Assert.IsTrue(guard.IsLocked);
            Assert.IsFalse(guard.TryAcquire());
        }

        [Test]
        public void Reset_AllowsAcquireAgain()
        {
            var guard = new UiInputGuard(1f);
            guard.TryAcquire();
            guard.Reset();

            Assert.IsFalse(guard.IsLocked);
            Assert.IsTrue(guard.TryAcquire());
        }
    }
}
