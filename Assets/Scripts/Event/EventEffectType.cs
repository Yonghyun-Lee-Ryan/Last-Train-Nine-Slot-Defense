namespace LastTrain.Event
{
    public enum EventEffectType
    {
        None = 0,
        AddCoins = 1,
        RemoveCoins = 2,
        HealTrain = 3,
        DamageTrain = 4,
        GrantPassenger = 5,
        RemoveRandomPassenger = 6,
        GrantAbility = 7,
        GrantRelic = 8,
        NextStationEnemyBuff = 9,
        NextStationRewardBonus = 10,
    }
}
