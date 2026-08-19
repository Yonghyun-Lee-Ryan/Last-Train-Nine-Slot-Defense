using LastTrain.Ability;
using LastTrain.Run;
using LastTrain.Synergy;

namespace LastTrain.Passenger
{
    /// <summary>준비 단계 한정 합성 Undo 1회. 전투 중에는 사용할 수 없다.</summary>
    public static class MergeUndoService
    {
        private static bool _hasSnapshot;
        private static int _sourceSlot = -1;
        private static int _targetSlot = -1;
        private static int _previousTargetStar = 1;
        private static string _targetInstanceId = string.Empty;
        private static PassengerRuntime _consumed;

        public static bool HasUndo => _hasSnapshot && _consumed != null;

        public static void Clear()
        {
            _hasSnapshot = false;
            _sourceSlot = -1;
            _targetSlot = -1;
            _previousTargetStar = 1;
            _targetInstanceId = string.Empty;
            _consumed = null;
        }

        public static bool CanUndo(RunState runState)
        {
            if (!HasUndo || runState?.Battle == null)
            {
                return false;
            }

            return runState.Battle.CurrentPhase == RunPhase.Preparing;
        }

        public static void RecordPreparingMerge(
            RunState runState,
            int sourceSlot,
            int targetSlot,
            PassengerRuntime consumed,
            PassengerRuntime target,
            int previousTargetStar)
        {
            if (runState?.Battle == null
                || runState.Battle.CurrentPhase != RunPhase.Preparing
                || consumed == null
                || target == null)
            {
                Clear();
                return;
            }

            _hasSnapshot = true;
            _sourceSlot = sourceSlot;
            _targetSlot = targetSlot;
            _previousTargetStar = previousTargetStar;
            _targetInstanceId = target.InstanceId ?? string.Empty;
            _consumed = consumed;
        }

        public static bool TryUndo(RunState runState)
        {
            if (!CanUndo(runState))
            {
                return false;
            }

            PassengerRuntime target = runState.GetPassengerAtSlot(_targetSlot);
            if (target == null
                || !string.Equals(target.InstanceId, _targetInstanceId, System.StringComparison.Ordinal))
            {
                Clear();
                return false;
            }

            PassengerRuntime occupant = runState.GetPassengerAtSlot(_sourceSlot);
            if (occupant != null)
            {
                if (!runState.TryRemovePassenger(_sourceSlot, out PassengerRuntime extra) || extra == null)
                {
                    return false;
                }

                runState.EnqueuePendingPassenger(extra);
            }

            if (!runState.TryPlacePassengerFromSave(_sourceSlot, _consumed))
            {
                return false;
            }

            target.SetStarLevel(_previousTargetStar);
            runState.History.UnrecordMerge();
            AbilityEffectApplier.RefreshPassengerBuffs(runState);
            SynergyEffectApplier.Refresh(runState);
            Clear();
            return true;
        }
    }
}
