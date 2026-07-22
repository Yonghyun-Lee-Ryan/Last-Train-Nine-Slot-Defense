using System;
using LastTrain.Event;

namespace LastTrain.Data
{
    [Serializable]
    public struct EventConditionData
    {
        public EventConditionType conditionType;
        public string targetId;
        public int value;
    }
}
