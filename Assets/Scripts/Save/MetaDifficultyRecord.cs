using System;

using LastTrain.Difficulty;

namespace LastTrain.Save
{
    [Serializable]
    public sealed class MetaDifficultyRecord
    {
        public string difficultyId = DifficultyIds.Normal;
        public int highestStationReached;
        public int clearCount;
        public int bestScore;
        public float fastestClearSeconds;
        public int bestRemainingHp;
        public string firstClearUtc = string.Empty;
        public bool clearedWithoutAds;
    }
}
