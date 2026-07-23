using System;
using LastTrain.Difficulty;

namespace LastTrain.Save
{
    /// <summary>영구 메타 진행 저장. 필드 확장 시 CurrentVersion을 올리고 ISaveMigration을 추가한다.</summary>
    [Serializable]
    public sealed class MetaSaveData
    {
        public const int CurrentVersion = 2;

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

        public string[] unlockedDifficultyIds = Array.Empty<string>();
        public MetaDifficultyRecord[] difficultyRecords = Array.Empty<MetaDifficultyRecord>();
        public string[] pendingUnlockedDifficultyIds = Array.Empty<string>();

        // Unit 28: 일일·주간 미션
        public string missionDailyKey = string.Empty;
        public string missionWeeklyKey = string.Empty;
        public string missionLastTrustedUtc = string.Empty;
        public Mission.MissionProgressSave[] missionProgresses = Array.Empty<Mission.MissionProgressSave>();

        // Unit 29: 무한 모드·로컬 랭킹
        public string anonymousUserId = string.Empty;
        public int endlessBestScore;
        public int endlessBestStationReached;
        public string endlessBestRunId = string.Empty;
        public string[] endlessSubmittedRunIds = Array.Empty<string>();

        // Unit 30: 튜토리얼
        public bool tutorialCompleted;
        public bool tutorialSkipped;
        public int tutorialStepIndex;

        // Unit 34: 시즌·라이브 이벤트 진행 (기본 진행과 분리)
        public LiveOps.LiveEventProgress[] liveEventProgresses = Array.Empty<LiveOps.LiveEventProgress>();

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
            unlockedDifficultyIds ??= Array.Empty<string>();
            difficultyRecords ??= Array.Empty<MetaDifficultyRecord>();
            pendingUnlockedDifficultyIds ??= Array.Empty<string>();
            missionDailyKey ??= string.Empty;
            missionWeeklyKey ??= string.Empty;
            missionLastTrustedUtc ??= string.Empty;
            missionProgresses ??= Array.Empty<Mission.MissionProgressSave>();
            anonymousUserId ??= string.Empty;
            endlessBestRunId ??= string.Empty;
            endlessSubmittedRunIds ??= Array.Empty<string>();
            liveEventProgresses ??= Array.Empty<LiveOps.LiveEventProgress>();
            if (tutorialStepIndex < 0)
            {
                tutorialStepIndex = 0;
            }

            for (int i = 0; i < liveEventProgresses.Length; i++)
            {
                liveEventProgresses[i]?.EnsureDefaults();
            }

            dummy ??= string.Empty;

            if (unlockedPassengerIds.Length == 0)
            {
                unlockedPassengerIds = (string[])MetaProgressionDefaults.DefaultUnlockedPassengerIds.Clone();
            }

            if (!ContainsDifficultyId(unlockedDifficultyIds, DifficultyIds.Normal))
            {
                unlockedDifficultyIds = AppendDifficultyId(unlockedDifficultyIds, DifficultyIds.Normal);
            }
        }

        private static bool ContainsDifficultyId(string[] ids, string id)
        {
            if (ids == null || string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            for (int i = 0; i < ids.Length; i++)
            {
                if (string.Equals(ids[i], id, System.StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static string[] AppendDifficultyId(string[] ids, string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return ids ?? System.Array.Empty<string>();
            }

            var list = new System.Collections.Generic.List<string>(ids ?? System.Array.Empty<string>());
            if (!ContainsDifficultyId(list.ToArray(), id))
            {
                list.Add(id);
            }

            return list.ToArray();
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
