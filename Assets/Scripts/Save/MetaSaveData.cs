using System;

namespace LastTrain.Save
{
    [Serializable]
    public sealed class MetaSaveData
    {
        public const int CurrentVersion = 1;
        public int version = CurrentVersion;

        // Unit 16에서는 MVP로 스키마만 만든다.
        public string dummy = string.Empty;
    }
}

