namespace LastTrain.Run
{
    /// <summary>선택된 능력 카드로부터 계산된 회차 수정치.</summary>
    public sealed class AbilityModifiers
    {
        public static AbilityModifiers Empty { get; } = new();

        public float GlobalAttackPercent { get; set; }
        public float GlobalAttackSpeedPercent { get; set; }
        public float GlobalDamagePercent { get; set; }
        public float FrontRowAttackPercent { get; set; }
        public float CoinOnKillPercent { get; set; }
        public float SellPricePercent { get; set; }
        public float NurseHealPercent { get; set; }
        public float PoliceBossDamagePercent { get; set; }
        public float CatCritChancePercent { get; set; }
        public float SameRoleAttackSpeedPercent { get; set; }
        public float DiversePassengerDamagePercent { get; set; }
        public int DiversePassengerThreshold { get; set; } = 6;
        public int TrainMaxHpFlat { get; set; }
        public int TrainHealOnStationComplete { get; set; }
        public int SummonCostIncreaseReduction { get; set; }

        /// <summary>특정 승객 ID별 공격력 % (없으면 0).</summary>
        public System.Collections.Generic.Dictionary<string, float> PassengerAttackPercentById { get; } =
            new System.Collections.Generic.Dictionary<string, float>();

        /// <summary>특정 승객 ID별 공격속도 % (없으면 0).</summary>
        public System.Collections.Generic.Dictionary<string, float> PassengerAttackSpeedPercentById { get; } =
            new System.Collections.Generic.Dictionary<string, float>();

        public float GetPassengerAttackPercent(string passengerId)
        {
            if (string.IsNullOrWhiteSpace(passengerId))
            {
                return 0f;
            }

            return PassengerAttackPercentById.TryGetValue(passengerId, out float value) ? value : 0f;
        }

        public float GetPassengerAttackSpeedPercent(string passengerId)
        {
            if (string.IsNullOrWhiteSpace(passengerId))
            {
                return 0f;
            }

            return PassengerAttackSpeedPercentById.TryGetValue(passengerId, out float value) ? value : 0f;
        }
    }
}
