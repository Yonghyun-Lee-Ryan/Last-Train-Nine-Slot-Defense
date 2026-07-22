using System;

namespace LastTrain.Save
{
    /// <summary>영구 메타 진행 저장. Unit 16 호환을 위해 version=1을 유지하고 필드를 확장한다.</summary>
    [Serializable]
    public sealed class MetaSaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;

        /// <summary>Unit 16 호환용 잔여 필드. 신규 로직에서는 사용하지 않는다.</summary>
        public string dummy = string.Empty;

        public int ticketFragments;
        public int accountXp;
        public int accountLevel = 1;

        public string[] unlockedPassengerIds = Array.Empty<string>();
        public string[] unlockedAbilityIds = Array.Empty<string>();
        public string[] unlockedRelicIds = Array.Empty<string>();

        public string[] discoveredPassengerIds = Array.Empty<string>();
        public string[] discoveredEnemyIds = Array.Empty<string>();
        public string[] discoveredBossIds = Array.Empty<string>();

        public string[] unlockedAchievementIds = Array.Empty<string>();
        public string[] rewardedRunIds = Array.Empty<string>();

        /// <summary>메인 메뉴/결과에서 신규 발견 알림용. 확인 후 비운다.</summary>
        public string[] pendingNewDiscoveryIds = Array.Empty<string>();

        public MetaPassengerMasteryEntry[] passengerMasteries = Array.Empty<MetaPassengerMasteryEntry>();

        public void EnsureDefaults()
        {
            if (version <= 0)
            {
                version = CurrentVersion;
            }

            if (accountLevel < 1)
            {
                accountLevel = 1;
            }

            unlockedPassengerIds ??= Array.Empty<string>();
            unlockedAbilityIds ??= Array.Empty<string>();
            unlockedRelicIds ??= Array.Empty<string>();
            discoveredPassengerIds ??= Array.Empty<string>();
            discoveredEnemyIds ??= Array.Empty<string>();
            discoveredBossIds ??= Array.Empty<string>();
            unlockedAchievementIds ??= Array.Empty<string>();
            rewardedRunIds ??= Array.Empty<string>();
            pendingNewDiscoveryIds ??= Array.Empty<string>();
            passengerMasteries ??= Array.Empty<MetaPassengerMasteryEntry>();
            dummy ??= string.Empty;

            if (unlockedPassengerIds.Length == 0)
            {
                unlockedPassengerIds = (string[])MetaProgressionDefaults.DefaultUnlockedPassengerIds.Clone();
            }
        }
    }

    [Serializable]
    public sealed class MetaPassengerMasteryEntry
    {
        public string passengerId = string.Empty;
        public int useCount;
        public int highestStar = 1;
        public int bossKillParticipations;
    }
}
