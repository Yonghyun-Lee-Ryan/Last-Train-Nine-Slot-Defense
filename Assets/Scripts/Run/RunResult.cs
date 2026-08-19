using System;
using LastTrain.Data;

namespace LastTrain.Run
{
    /// <summary>
    /// 회차 종료 시 결과 화면·메타 보상으로 전달하는 불변 스냅샷.
    /// </summary>
    public sealed class RunResult
    {
        public RunResult(
            string runId,
            string lineId,
            bool isVictory,
            RunEndReason endReason,
            int reachedStationIndex,
            int completedStationCount,
            int enemiesKilled,
            int bossesKilled,
            int mergeCount,
            int highestPassengerStar,
            int remainingTrainHp,
            int trainMaxHp,
            int finalCoins,
            int totalCoinsEarned,
            int totalCoinsSpent,
            int passengersSummoned,
            int passengersSold,
            int abilityCardsSelected,
            string[] discoveredPassengerIds = null,
            string[] discoveredEnemyIds = null,
            string[] discoveredBossIds = null,
            RunPassengerMasterySnapshot[] passengerMasteries = null,
            string difficultyId = null,
            float difficultyRewardMultiplier = 1f,
            float elapsedSeconds = 0f,
            bool isEndlessRun = false,
            bool adsUsed = false,
            StationType reachedStationType = StationType.Normal)
        {
            RunId = runId ?? string.Empty;
            LineId = lineId ?? string.Empty;
            IsVictory = isVictory;
            EndReason = endReason;
            ReachedStationIndex = reachedStationIndex;
            CompletedStationCount = completedStationCount;
            EnemiesKilled = enemiesKilled;
            BossesKilled = bossesKilled;
            MergeCount = mergeCount;
            HighestPassengerStar = highestPassengerStar;
            RemainingTrainHp = remainingTrainHp;
            TrainMaxHp = trainMaxHp;
            FinalCoins = finalCoins;
            TotalCoinsEarned = totalCoinsEarned;
            TotalCoinsSpent = totalCoinsSpent;
            PassengersSummoned = passengersSummoned;
            PassengersSold = passengersSold;
            AbilityCardsSelected = abilityCardsSelected;
            DiscoveredPassengerIds = discoveredPassengerIds ?? System.Array.Empty<string>();
            DiscoveredEnemyIds = discoveredEnemyIds ?? System.Array.Empty<string>();
            DiscoveredBossIds = discoveredBossIds ?? System.Array.Empty<string>();
            PassengerMasteries = passengerMasteries ?? System.Array.Empty<RunPassengerMasterySnapshot>();
            DifficultyId = string.IsNullOrWhiteSpace(difficultyId)
                ? Difficulty.DifficultyIds.Normal
                : difficultyId;
            DifficultyRewardMultiplier = difficultyRewardMultiplier > 0f ? difficultyRewardMultiplier : 1f;
            ElapsedSeconds = Math.Max(0f, elapsedSeconds);
            IsEndlessRun = isEndlessRun;
            AdsUsed = adsUsed;
            ReachedStationType = reachedStationType;
        }

        public string RunId { get; }
        public string LineId { get; }
        public bool IsVictory { get; }
        public RunEndReason EndReason { get; }
        public int ReachedStationIndex { get; }
        public int CompletedStationCount { get; }
        public int EnemiesKilled { get; }
        public int BossesKilled { get; }
        public int MergeCount { get; }
        public int HighestPassengerStar { get; }
        public int RemainingTrainHp { get; }
        public int TrainMaxHp { get; }
        public int FinalCoins { get; }
        public int TotalCoinsEarned { get; }
        public int TotalCoinsSpent { get; }
        public int PassengersSummoned { get; }
        public int PassengersSold { get; }
        public int AbilityCardsSelected { get; }
        public string[] DiscoveredPassengerIds { get; }
        public string[] DiscoveredEnemyIds { get; }
        public string[] DiscoveredBossIds { get; }
        public RunPassengerMasterySnapshot[] PassengerMasteries { get; }
        public string DifficultyId { get; }
        public float DifficultyRewardMultiplier { get; }
        public float ElapsedSeconds { get; }
        public bool IsEndlessRun { get; }
        public bool AdsUsed { get; }
        public StationType ReachedStationType { get; }
    }
}
