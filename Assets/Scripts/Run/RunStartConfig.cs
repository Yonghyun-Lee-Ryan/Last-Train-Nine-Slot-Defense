namespace LastTrain.Run
{
    /// <summary>새 회차 시작 시 적용할 초기값.</summary>
    public sealed class RunStartConfig
    {
        /// <summary>README MVP 객차 최대 내구도 기본값.</summary>
        public const int DefaultTrainMaxHp = 100;

        public int InitialTrainMaxHp { get; set; } = DefaultTrainMaxHp;
        public int InitialTrainCurrentHp { get; set; } = DefaultTrainMaxHp;
        public int InitialCoins { get; set; }
        public int InitialStationIndex { get; set; } = 1;
        public string LineId { get; set; } = "line1";

        public static RunStartConfig CreateDefault()
        {
            return new RunStartConfig();
        }
    }
}
