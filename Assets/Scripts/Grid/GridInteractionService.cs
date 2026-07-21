using LastTrain.Ability;
using LastTrain.Passenger;
using LastTrain.Run;
using LastTrain.Synergy;

namespace LastTrain.Grid
{
    /// <summary>드래그 드롭 결과.</summary>
    public enum GridDropResult
    {
        /// <summary>유효하지 않은 위치에 놓아 원래 슬롯으로 복귀.</summary>
        Reverted = 0,

        /// <summary>빈 슬롯으로 이동.</summary>
        Moved = 1,

        /// <summary>다른 승객과 위치 교환.</summary>
        Swapped = 2,

        /// <summary>동일 승객 합성.</summary>
        Merged = 3
    }

    /// <summary>
    /// Grid 드래그·드롭 판정 순수 로직.
    /// RunState를 변경하며 UI와 분리되어 EditMode 테스트가 가능하다.
    /// </summary>
    public static class GridInteractionService
    {
        public static GridDropResult TryDrop(RunState runState, int fromSlot, int toSlot)
        {
            if (runState == null)
            {
                return GridDropResult.Reverted;
            }

            if (fromSlot < 0 || fromSlot >= RunState.GridSlotCount)
            {
                return GridDropResult.Reverted;
            }

            if (toSlot < 0 || toSlot >= RunState.GridSlotCount)
            {
                return GridDropResult.Reverted;
            }

            if (fromSlot == toSlot)
            {
                return GridDropResult.Reverted;
            }

            PassengerRuntime fromPassenger = runState.GetPassengerAtSlot(fromSlot);
            if (fromPassenger == null)
            {
                return GridDropResult.Reverted;
            }

            PassengerRuntime targetPassenger = runState.GetPassengerAtSlot(toSlot);
            if (targetPassenger == null)
            {
                runState.TrySwapSlots(fromSlot, toSlot);
                RefreshPlacementBuffs(runState);
                return GridDropResult.Moved;
            }

            if (MergeService.TryMerge(runState, fromSlot, toSlot, out _))
            {
                RefreshPlacementBuffs(runState);
                return GridDropResult.Merged;
            }

            runState.TrySwapSlots(fromSlot, toSlot);
            RefreshPlacementBuffs(runState);
            return GridDropResult.Swapped;
        }

        private static void RefreshPlacementBuffs(RunState runState)
        {
            AbilityEffectApplier.RefreshPassengerBuffs(runState);
            SynergyEffectApplier.Refresh(runState);
        }
    }
}
