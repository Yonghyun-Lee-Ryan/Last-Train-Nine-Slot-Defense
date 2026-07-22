using System;

namespace LastTrain.Difficulty
{
    [Serializable]
    public sealed class DifficultyUnlockRequirement
    {
        public DifficultyUnlockType unlockType = DifficultyUnlockType.AlwaysUnlocked;
        public string requiredDifficultyId = string.Empty;
        public int requiredAccountLevel = 1;
        public int requiredUnlockedPassengerCount;
    }

    public sealed class DifficultyUnlockProgress
    {
        public bool IsUnlocked { get; set; }
        public string ProgressText { get; set; } = string.Empty;
    }
}
