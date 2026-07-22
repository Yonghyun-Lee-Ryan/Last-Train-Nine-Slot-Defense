using System;
using System.Collections.Generic;

namespace LastTrain.Event
{
    public sealed class EventProgress
    {
        public event Action Changed;

        public bool IsActive { get; private set; }
        public bool IsResolved { get; private set; }
        public string StationId { get; private set; } = string.Empty;
        public string EventId { get; private set; } = string.Empty;
        public int SelectedChoiceIndex { get; private set; } = -1;

        public void Reset()
        {
            IsActive = false;
            IsResolved = false;
            StationId = string.Empty;
            EventId = string.Empty;
            SelectedChoiceIndex = -1;
            Changed?.Invoke();
        }

        public void Begin(string stationId, string eventId)
        {
            IsActive = true;
            IsResolved = false;
            StationId = stationId ?? string.Empty;
            EventId = eventId ?? string.Empty;
            SelectedChoiceIndex = -1;
            Changed?.Invoke();
        }

        public void Resolve(int choiceIndex)
        {
            SelectedChoiceIndex = choiceIndex;
            IsActive = false;
            IsResolved = true;
            Changed?.Invoke();
        }

        public void Restore(
            string stationId,
            string eventId,
            bool isActive,
            bool isResolved,
            int selectedChoiceIndex)
        {
            StationId = stationId ?? string.Empty;
            EventId = eventId ?? string.Empty;
            IsActive = isActive;
            IsResolved = isResolved;
            SelectedChoiceIndex = selectedChoiceIndex;
            Changed?.Invoke();
        }
    }
}
