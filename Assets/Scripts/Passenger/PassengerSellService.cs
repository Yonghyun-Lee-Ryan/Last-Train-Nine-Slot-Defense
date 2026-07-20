using System;
using LastTrain.Run;

namespace LastTrain.Passenger
{
    /// <summary>승객 판매 순수 로직.</summary>
    public static class PassengerSellService
    {
        public static int GetSellPrice(PassengerRuntime passenger)
        {
            if (passenger?.Data == null)
            {
                return 0;
            }

            return passenger.Data.GetSellPrice(passenger.StarLevel);
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

            coinsGained = GetSellPrice(passenger);
            if (!runState.TryConsumePassenger(slotIndex, out _))
            {
                coinsGained = 0;
                return false;
            }

            runState.RecordPassengerSold();
            if (coinsGained > 0)
            {
                runState.Currency.AddCoins(coinsGained);
            }

            return true;
        }
    }
}
