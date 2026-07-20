namespace LastTrain.Data
{
    /// <summary>유물이 적용하는 효과 종류.</summary>
    public enum RelicEffectType
    {
        None = 0,

        /// <summary>첫 소환 무료</summary>
        FirstSummonFree = 1,

        /// <summary>직장인 계열 공격속도 (%)</summary>
        OfficeWorkerAttackSpeedPercent = 2,

        /// <summary>개발자 터렛 지속시간 (%)</summary>
        DeveloperTurretDurationPercent = 3,

        /// <summary>역 종료 코인 추가 (고정값)</summary>
        StationCompleteCoinBonus = 4,

        /// <summary>치명타 확률 (%)</summary>
        CritChancePercent = 5,

        /// <summary>객차 최대 내구도 (고정값)</summary>
        TrainMaxHpFlat = 6
    }
}
