using System;
using LastTrain.Run;

namespace LastTrain.Economy
{
    /// <summary>코인 차감·지급 서비스. CurrencyState를 감싼다.</summary>
    public sealed class CurrencyService
    {
        private readonly RunState _runState;

        public CurrencyService(RunState runState)
        {
            _runState = runState ?? throw new ArgumentNullException(nameof(runState));
        }

        public int CurrentCoins => _runState.Currency.CurrentCoins;

        public bool CanAfford(int amount)
        {
            return _runState.Currency.CanAfford(amount);
        }

        public bool TrySpend(int amount)
        {
            return _runState.Currency.TrySpend(amount);
        }

        public void AddCoins(int amount)
        {
            _runState.Currency.AddCoins(amount);
        }
    }
}
