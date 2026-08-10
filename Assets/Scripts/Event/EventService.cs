using System;
using LastTrain.Core;
using LastTrain.Data;
using LastTrain.Relic;
using LastTrain.Run;

namespace LastTrain.Event
{
    public enum EventChoiceResult
    {
        Success = 0,
        EventNotActive = 1,
        InvalidChoice = 2,
        AlreadyResolved = 3,
        EffectFailed = 4,
    }

    public sealed class EventService
    {
        private readonly RunState _runState;
        private readonly GameDatabase _database;
        private readonly RelicManager _relicManager;
        private readonly RandomService _random;

        public EventService(
            RunState runState,
            GameDatabase database,
            RelicManager relicManager,
            RandomService random)
        {
            _runState = runState ?? throw new ArgumentNullException(nameof(runState));
            _database = database;
            _relicManager = relicManager;
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public static int CreateSeed(RunState runState, int stationIndex)
        {
            unchecked
            {
                int hash = 23;
                hash = hash * 31 + (runState?.RunId?.GetHashCode() ?? 0);
                hash = hash * 31 + stationIndex;
                return hash;
            }
        }

        public bool TryOpenEvent(StationData station)
        {
            if (station == null || _runState.Events.IsActive)
            {
                return false;
            }

            EventData eventData = PickEvent(station.StationIndex);
            if (eventData == null)
            {
                return false;
            }

            _runState.Events.Begin(station.Id, eventData.Id);
            _runState.Battle.SetPhase(RunPhase.EventOpen);
            return true;
        }

        public EventData GetCurrentEvent()
        {
            if (string.IsNullOrWhiteSpace(_runState.Events.EventId) || _database == null)
            {
                return null;
            }

            return _database.TryGetEvent(_runState.Events.EventId, out EventData data) ? data : null;
        }

        public bool IsChoiceVisible(EventChoiceData choice)
        {
            return EventConditionEvaluator.IsChoiceVisible(_runState, choice);
        }

        public EventChoiceResult TrySelectChoice(int choiceIndex)
        {
            if (_runState.Events.IsResolved)
            {
                return EventChoiceResult.AlreadyResolved;
            }

            if (!_runState.Events.IsActive)
            {
                return EventChoiceResult.EventNotActive;
            }

            EventData eventData = GetCurrentEvent();
            if (eventData == null)
            {
                return EventChoiceResult.InvalidChoice;
            }

            EventChoiceData[] choices = eventData.Choices;
            if (choiceIndex < 0 || choiceIndex >= choices.Length)
            {
                return EventChoiceResult.InvalidChoice;
            }

            EventChoiceData choice = choices[choiceIndex];
            if (!IsChoiceVisible(choice))
            {
                return EventChoiceResult.InvalidChoice;
            }

            float reduction = _runState.Relics?.Modifiers?.EventBadOutcomeReductionPercent ?? 0f;
            if (!EventEffectApplier.ApplyAll(_runState, _database, _relicManager, choice.effects, reduction))
            {
                return EventChoiceResult.EffectFailed;
            }

            _runState.Events.Resolve(choiceIndex);
            _runState.Battle.SetPhase(RunPhase.Preparing);
            return EventChoiceResult.Success;
        }

        /// <summary>데이터/UI 오류 시 이벤트 역을 안전하게 빠져나온다.</summary>
        public bool TrySkipEvent()
        {
            if (!_runState.Events.IsActive || _runState.Events.IsResolved)
            {
                return false;
            }

            _runState.Events.Resolve(-1);
            _runState.Battle.SetPhase(RunPhase.Preparing);
            return true;
        }

        private EventData PickEvent(int stationIndex)
        {
            if (_database?.Events == null || _database.Events.Count == 0)
            {
                return null;
            }

            _random.Reseed(CreateSeed(_runState, stationIndex));
            return _database.Events[_random.Next(_database.Events.Count)];
        }
    }
}
