namespace LastTrain.Run
{
    /// <summary>활성 시너지로부터 계산된 회차 수정치.</summary>
    public sealed class SynergyModifiers
    {
        public static SynergyModifiers Empty { get; } = new();

        public float GlobalAttackPercent { get; set; }
        public float GlobalAttackSpeedPercent { get; set; }
        public float TrainHealPercent { get; set; }
        public float CritChancePercent { get; set; }
        public float FastEnemyDamagePercent { get; set; }

        public void Clear()
        {
            GlobalAttackPercent = 0f;
            GlobalAttackSpeedPercent = 0f;
            TrainHealPercent = 0f;
            CritChancePercent = 0f;
            FastEnemyDamagePercent = 0f;
        }
    }
}
