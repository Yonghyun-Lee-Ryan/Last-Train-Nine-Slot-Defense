using System;
using UnityEngine;

namespace LastTrain.Data
{
    [Serializable]
    public sealed class EventChoiceData
    {
        public string choiceId = string.Empty;
        [TextArea(1, 3)]
        public string text = string.Empty;
        public EventConditionData[] conditions = Array.Empty<EventConditionData>();
        public EventEffectData[] effects = Array.Empty<EventEffectData>();
    }
}
