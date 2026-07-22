using LastTrain.Data;
using LastTrain.Run;

namespace LastTrain.Event
{
    public static class EventConditionEvaluator
    {
        public static bool IsChoiceVisible(RunState runState, EventChoiceData choice)
        {
            if (choice?.conditions == null || choice.conditions.Length == 0)
            {
                return true;
            }

            for (int i = 0; i < choice.conditions.Length; i++)
            {
                if (!Evaluate(runState, choice.conditions[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool Evaluate(RunState runState, EventConditionData condition)
        {
            switch (condition.conditionType)
            {
                case EventConditionType.RequiresPassenger:
                    return HasPassenger(runState, condition.targetId);

                case EventConditionType.RequiresRelic:
                    return runState?.Relics != null && runState.Relics.HasRelic(condition.targetId);

                case EventConditionType.MinCoins:
                    return runState?.Currency != null && runState.Currency.CurrentCoins >= condition.value;

                case EventConditionType.MaxCoins:
                    return runState?.Currency != null && runState.Currency.CurrentCoins <= condition.value;

                default:
                    return true;
            }
        }

        private static bool HasPassenger(RunState runState, string passengerId)
        {
            if (runState == null || string.IsNullOrWhiteSpace(passengerId))
            {
                return false;
            }

            for (int i = 0; i < RunState.GridSlotCount; i++)
            {
                PassengerRuntime passenger = runState.GetPassengerAtSlot(i);
                if (passenger?.Data?.Id == passengerId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
