namespace LastTrain.Relic
{
    /// <summary>유물 효과가 수정하는 런타임 수치.</summary>
    public sealed class RelicModifiers
    {
        public static RelicModifiers Empty { get; } = new();

        public bool FirstSummonFree { get; set; }
        public float OfficeWorkerAttackSpeedPercent { get; set; }
        public float DeveloperTurretDurationPercent { get; set; }
        public int StationCompleteCoinBonus { get; set; }
        public float CritChancePercent { get; set; }
        public int TrainMaxHpFlat { get; set; }
        public float SellPricePercent { get; set; }
        public float BossFirstActionDelaySeconds { get; set; }
        public int EmergencyAutoHealFlat { get; set; }
        public bool EmergencyAutoHealUsed { get; set; }
        public float EventBadOutcomeReductionPercent { get; set; }
    }
}
