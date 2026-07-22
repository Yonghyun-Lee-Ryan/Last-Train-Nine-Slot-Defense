using System;
using LastTrain.Event;

namespace LastTrain.Data
{
    [Serializable]
    public struct EventEffectData
    {
        public EventEffectType effectType;
        public string targetId;
        public float value;
    }
}
