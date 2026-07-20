namespace LastTrain.Battle
{
    /// <summary>전투 좌표계 상수. PassengerData.range(게임 단위)를 Canvas 월드 거리로 변환한다.</summary>
    public static class BattleConstants
    {
        /// <summary>range 1 = Canvas 월드 거리 150px (1080×1920 기준).</summary>
        public const float RangeToWorldScale = 150f;

        public static float ToWorldRange(float dataRange)
        {
            return dataRange * RangeToWorldScale;
        }
    }
}
