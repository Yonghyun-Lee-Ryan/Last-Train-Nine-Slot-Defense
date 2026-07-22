namespace LastTrain.Enemy
{
    /// <summary>보스·특수 적 스킬 ID 상수.</summary>
    public static class EnemyAbilityIds
    {
        public const string BossMvp = "boss_mvp";
        public const string BossDrunkManager = "boss_drunk_manager";
        public const string BossFinalConductor = "boss_final_conductor";
        public const string SpawnMinions = "ability_spawn_minions";
        public const string AttackSpeedDebuff = "ability_as_debuff";
        public const string EnrageMoveSpeed = "ability_enrage_move";
        public const string PeriodicShield = "ability_periodic_shield";
        public const string Blackout = "ability_blackout";
        public const string SplitOnDeath = "ability_split_on_death";
        public const string NearbyBuff = "ability_nearby_buff";
        public const string SeatBlock = "ability_seat_block";
    }

    public static class BossDebuffIds
    {
        public const string AttackSpeedSlow = "boss:as_slow";
        public const string BlackoutSlow = "boss:blackout_slow";
        public const string SeatBlock = "enemy:seat_block";
    }

    public static class EnemyBuffIds
    {
        public const string AuraSpeed = "enemy:aura_speed";
    }
}
