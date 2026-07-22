using System;
using UnityEngine;

namespace LastTrain.Difficulty
{
    public enum DifficultyUnlockType
    {
        AlwaysUnlocked = 0,
        DefeatFinalBossOnDifficulty = 1,
        AccountLevel = 2,
        UnlockedPassengerCount = 3,
    }

    [Serializable]
    public sealed class DifficultyUnlockCondition
    {
        [SerializeField] private DifficultyUnlockRequirement[] requirements = Array.Empty<DifficultyUnlockRequirement>();

        public DifficultyUnlockRequirement[] Requirements =>
            requirements != null && requirements.Length > 0
                ? requirements
                : Array.Empty<DifficultyUnlockRequirement>();

        public bool IsAlwaysUnlocked()
        {
            DifficultyUnlockRequirement[] reqs = Requirements;
            if (reqs.Length == 0)
            {
                return true;
            }

            for (int i = 0; i < reqs.Length; i++)
            {
                if (reqs[i] != null && reqs[i].unlockType != DifficultyUnlockType.AlwaysUnlocked)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
