namespace LastTrain.Data
{
    /// <summary>오늘의 막차에만 적용되는 일일 규칙 종류.</summary>
    public enum DailyRuleKind
    {
        None = 0,
        LockSeat = 1,
        SummonCostMul = 2,
        EnemySpeedMul = 3,
        GrantRelic = 4,
        ReducedPrepTime = 5,
    }
}
