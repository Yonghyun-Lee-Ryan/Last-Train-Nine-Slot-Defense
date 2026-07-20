namespace LastTrain.Data
{
    /// <summary>능력 카드가 적용하는 효과 종류.</summary>
    public enum AbilityEffectType
    {
        None = 0,

        /// <summary>특정 또는 전체 승객 공격력 증가 (%)</summary>
        PassengerAttackPercent = 1,

        /// <summary>특정 또는 전체 승객 공격속도 증가 (%)</summary>
        PassengerAttackSpeedPercent = 2,

        /// <summary>객차 최대 내구도 증가 (고정값)</summary>
        TrainMaxHpFlat = 3,

        /// <summary>역 종료 시 객차 내구도 회복 (고정값)</summary>
        TrainHealOnStationComplete = 4,

        /// <summary>적 처치 코인 증가 (%)</summary>
        CoinOnKillPercent = 5,

        /// <summary>승객 판매 가격 증가 (%)</summary>
        SellPricePercent = 6,

        /// <summary>소환 비용 증가량 감소 (고정값)</summary>
        SummonCostIncreaseReduction = 7,

        /// <summary>앞줄 승객 공격력 증가 (%)</summary>
        FrontRowAttackPercent = 8,

        /// <summary>같은 직업군 N명 배치 시 공격속도 (%)</summary>
        SameRoleAttackSpeedPercent = 9,

        /// <summary>서로 다른 승객 N종 배치 시 전체 피해 (%)</summary>
        DiversePassengerDamagePercent = 10,

        /// <summary>간호사 회복량 (%)</summary>
        NurseHealPercent = 11,

        /// <summary>경찰관 보스 피해 (%)</summary>
        PoliceBossDamagePercent = 12,

        /// <summary>고양이 치명타 확률 (%)</summary>
        CatCritChancePercent = 13
    }
}
