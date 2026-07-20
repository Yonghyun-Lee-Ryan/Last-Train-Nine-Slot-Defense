namespace LastTrain.Run
{
    /// <summary>회차 진행 단계. 개발 단위 7에서 StationManager가 사용한다.</summary>
    public enum RunPhase
    {
        None = 0,
        Preparing = 1,
        WaveStarting = 2,
        Fighting = 3,
        WaveCompleted = 4,
        StationCompleted = 5,
        RewardSelecting = 6,
        RunEnded = 7
    }
}
