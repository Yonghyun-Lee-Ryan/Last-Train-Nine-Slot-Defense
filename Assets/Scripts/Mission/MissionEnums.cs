namespace LastTrain.Mission
{
    public enum MissionPeriod
    {
        Daily = 0,
        Weekly = 1,
    }

    public enum MissionConditionType
    {
        None = 0,
        MergeCount = 1,
        ReachPassengerStar = 2,
        ReachStationWithMinHp = 3,
        DealBossDamage = 4,
        DistinctPassengersPlaced = 5,
        ReachStationWithoutAds = 6,
        ShopPurchaseCount = 7,
        ClearRouteCount = 8,
        EliteKillCount = 9,
        RareOrHigherAbilitySelect = 10,
        SummonCount = 11,
        ClearDifficultyOrHigher = 12,
        DefeatFinalBoss = 13,
    }

    /// <summary>미션 진행에 쓰는 이벤트 종류. 매 프레임 폴링 대신 이벤트 구독으로 갱신한다.</summary>
    public enum MissionEventType
    {
        Merge = 0,
        PassengerStarReached = 1,
        StationCompleted = 2,
        BossDamaged = 3,
        DistinctPassengerPlaced = 4,
        ShopPurchased = 5,
        EnemyKilled = 6,
        AbilitySelected = 7,
        Summoned = 8,
        RunCleared = 9,
        FinalBossDefeated = 10,
        AdsUsed = 11,
    }
}
