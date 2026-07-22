namespace LastTrain.Data
{
    public static class StationTypeRules
    {
        public static bool UsesWaveManager(StationType stationType)
        {
            return RequiresWaves(stationType);
        }

        public static bool RequiresWaves(StationType stationType)
        {
            return stationType switch
            {
                StationType.Event => false,
                StationType.Shop => false,
                StationType.Rest => false,
                _ => true,
            };
        }

        public static string GetNonCombatDescription(StationType stationType)
        {
            return stationType switch
            {
                StationType.Event => "선택과 위험이 있는 이벤트",
                StationType.Shop => "코인으로 강화를 구매",
                StationType.Rest => "짧은 휴식",
                _ => string.Empty,
            };
        }
    }
}
