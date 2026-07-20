namespace LastTrain.Run
{
    /// <summary>
    /// 회차 종료 시 결과 화면으로 전달하는 불변 스냅샷.
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
            int mergeCount,
            int highestPassengerStar,
            int remainingTrainHp,
            int trainMaxHp,
            int finalCoins,
            int totalCoinsEarned,
            int totalCoinsSpent,
            int passengersSummoned,
            int passengersSold,
            int abilityCardsSelected)
        {
            RunId = runId ?? string.Empty;
            LineId = lineId ?? string.Empty;
            IsVictory = isVictory;
            EndReason = endReason;
            ReachedStationIndex = reachedStationIndex;
            CompletedStationCount = completedStationCount;
            EnemiesKilled = enemiesKilled;
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
        }

        public string RunId { get; }
        public string LineId { get; }
        public bool IsVictory { get; }
        public RunEndReason EndReason { get; }
        public int ReachedStationIndex { get; }
        public int CompletedStationCount { get; }
        public int EnemiesKilled { get; }
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
    }
}
