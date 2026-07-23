using System;
using LastTrain.Ability;
using LastTrain.Audio;
using LastTrain.Difficulty;
using LastTrain.Run;

namespace LastTrain.Passenger
{
    /// <summary>승객 판매 순수 로직.</summary>
    public static class PassengerSellService
    {
        /// <summary>판매 성공 시 (slotIndex, passengerId, coinsGained, starLevel).</summary>
        public static event Action<int, string, int, int> Sold;

        public static int GetSellPrice(PassengerRuntime passenger, RunState runState = null)
        {
            if (passenger?.Data == null)
            {
                return 0;
            }

            int basePrice = passenger.Data.GetSellPrice(passenger.StarLevel);
            if (runState?.Abilities?.Modifiers != null)
            {
                basePrice = AbilityEffectCalculator.ApplyPercentBonus(
                    basePrice,
                    runState.Abilities.Modifiers.SellPricePercent);
            }

            if (runState?.Relics?.Modifiers != null)
            {
                basePrice = AbilityEffectCalculator.ApplyPercentBonus(
                    basePrice,
                    runState.Relics.Modifiers.SellPricePercent);
            }

            return DifficultyCalculator.ApplyShopPrice(
                basePrice,
                runState?.Difficulty,
                runState?.DifficultyModifiers?.SellPriceMultiplier ?? 1f);
        }

        /// <summary>
        /// 슬롯의 승객을 판매하고 코인을 지급한다.
        /// </summary>
        public static bool TrySell(RunState runState, int slotIndex, out int coinsGained)
        {
            coinsGained = 0;
            if (runState == null)
            {
                return false;
            }

            PassengerRuntime passenger = runState.GetPassengerAtSlot(slotIndex);
            if (passenger == null)
            {
                return false;
            }

            string passengerId = passenger.Data.Id;
            int starLevel = passenger.StarLevel;
            coinsGained = GetSellPrice(passenger, runState);
            if (!runState.TryConsumePassenger(slotIndex, out _))
            {
                coinsGained = 0;
                return false;
            }

            runState.RecordPassengerSold();
            if (coinsGained > 0)
            {
                runState.Currency.AddCoins(coinsGained);
                GameAudio.PlaySfx(SfxId.Coin);
            }

            AbilityEffectApplier.RefreshPassengerBuffs(runState);
            Synergy.SynergyEffectApplier.Refresh(runState);
            runState.TryPlacePendingPassengers();
            Sold?.Invoke(slotIndex, passengerId, coinsGained, starLevel);
            return true;
        }
    }
}
