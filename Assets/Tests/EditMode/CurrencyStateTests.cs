using LastTrain.Run;
using NUnit.Framework;

namespace LastTrain.Tests.EditMode
{
    public class CurrencyStateTests
    {
        [Test]
        public void TrySpend_WithInsufficientCoins_ReturnsFalse()
        {
            var currency = new CurrencyState(10);

            bool spent = currency.TrySpend(15);

            Assert.IsFalse(spent);
            Assert.AreEqual(10, currency.CurrentCoins);
            Assert.AreEqual(0, currency.TotalSpent);
        }

        [Test]
        public void TrySpend_WithEnoughCoins_UpdatesTotals()
        {
            var currency = new CurrencyState(20);
            currency.AddCoins(10);

            bool spent = currency.TrySpend(15);

            Assert.IsTrue(spent);
            Assert.AreEqual(15, currency.CurrentCoins);
            Assert.AreEqual(10, currency.TotalEarned);
            Assert.AreEqual(15, currency.TotalSpent);
        }

        [Test]
        public void AddCoins_FiresCoinsChangedEvent()
        {
            var currency = new CurrencyState(0);
            int reported = -1;
            currency.CoinsChanged += coins => reported = coins;

            currency.AddCoins(7);

            Assert.AreEqual(7, reported);
        }
    }
}
