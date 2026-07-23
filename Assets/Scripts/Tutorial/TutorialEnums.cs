namespace LastTrain.Tutorial
{
    public enum TutorialStepKind
    {
        SummonPassenger = 0,
        PlacePassenger = 1,
        ObserveAutoAttack = 2,
        MergePassengers = 3,
        ExplainTrainHp = 4,
        StationReward = 5,
        SelectAbility = 6,
        BossHint = 7,
    }

    /// <summary>튜토리얼이 기다리는 실제 게임 이벤트.</summary>
    public enum TutorialWaitEvent
    {
        None = 0,
        SummonOpened = 1,
        PassengerPlaced = 2,
        EnemyDamaged = 3,
        PassengersMerged = 4,
        Acknowledge = 5,
        StationCompleted = 6,
        AbilitySelected = 7,
        BossBriefingShown = 8,
    }

    [System.Flags]
    public enum TutorialInputMask
    {
        None = 0,
        Summon = 1 << 0,
        GridDrag = 1 << 1,
        Ready = 1 << 2,
        AbilityOffer = 1 << 3,
        Pause = 1 << 4,
        Acknowledge = 1 << 5,
        All = ~0,
    }
}
