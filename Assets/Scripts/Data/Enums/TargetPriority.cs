namespace LastTrain.Data
{
    /// <summary>승객 공격 시 적 선택 우선순위.</summary>
    public enum TargetPriority
    {
        Nearest = 0,
        Fastest = 1,
        LowestHealth = 2,
        BossFirst = 3
    }
}
