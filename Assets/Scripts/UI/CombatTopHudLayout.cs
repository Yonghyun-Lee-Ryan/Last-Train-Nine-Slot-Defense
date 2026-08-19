using LastTrain.Run;

namespace LastTrain.UI
{
    /// <summary>전투 상단 HUD(역·웨이브·브리핑·시너지·위협 예고) 수직 배치.</summary>
    public static class CombatTopHudLayout
    {
        public const float StationRowY = -120f;
        public const float StatusLineY = -165f;
        public const float ThreatTickerY = -204f;
        public const float ThreatTickerHeight = 100f;
        public const float ThreatTickerWidth = 720f;

        public const float SynergyLeftX = 8f;
        public const float SynergyWidth = 300f;
        public const float SynergyTopNoThreat = -128f;
        public const float SynergyTopWithThreat = -312f;
        public const float SynergyMaxHeight = 200f;

        public const float OwnedAbilityY = -404f;
        public const float OwnedAbilityHeight = 36f;

        public static bool ShouldShowSideChrome(RunPhase phase)
        {
            return phase != RunPhase.RewardSelecting
                   && phase != RunPhase.ShopOpen
                   && phase != RunPhase.EventOpen
                   && phase != RunPhase.RunEnded;
        }

        public static bool ShouldShowThreatTicker(RunPhase phase, bool hasEntries)
        {
            return hasEntries && ShouldShowSideChrome(phase);
        }

        public static float GetSynergyTop(bool threatTickerVisible)
        {
            return threatTickerVisible ? SynergyTopWithThreat : SynergyTopNoThreat;
        }
    }
}
