using System;

namespace LastTrain.Run
{
    /// <summary>회차 내 코인 상태.</summary>
    public sealed class CurrencyState
    {
        public event Action<int> CoinsChanged;

        public int CurrentCoins { get; private set; }
        public int TotalEarned { get; private set; }
        public int TotalSpent { get; private set; }

        public CurrencyState(int initialCoins)
        {
            if (initialCoins < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCoins), "initialCoins는 0 이상이어야 합니다.");
            }

            CurrentCoins = initialCoins;
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            CurrentCoins += amount;
            TotalEarned += amount;
            CoinsChanged?.Invoke(CurrentCoins);
        }

        public bool TrySpend(int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (CurrentCoins < amount)
            {
                return false;
            }

            CurrentCoins -= amount;
            TotalSpent += amount;
            CoinsChanged?.Invoke(CurrentCoins);
            return true;
        }

        public bool CanAfford(int amount)
        {
            return amount <= 0 || CurrentCoins >= amount;
        }

        /// <summary>저장 데이터로부터 상태를 복원한다.</summary>
        public void RestoreFromSave(int currentCoins, int totalEarned, int totalSpent)
        {
            CurrentCoins = Math.Max(0, currentCoins);
            TotalEarned = Math.Max(0, totalEarned);
            TotalSpent = Math.Max(0, totalSpent);

            CoinsChanged?.Invoke(CurrentCoins);
        }
    }
}
