namespace LastTrain.Battle
{
    /// <summary>전투 속도 1/2/3 프리셋.</summary>
    public static class BattleSpeedPreset
    {
        public const int Min = 1;
        public const int Max = 3;

        public static int Clamp(int preset)
        {
            if (preset < Min)
            {
                return Min;
            }

            return preset > Max ? Max : preset;
        }

        public static int Cycle(int preset)
        {
            int current = Clamp(preset);
            return current >= Max ? Min : current + 1;
        }

        public static float ToTimeScale(int preset)
        {
            return Clamp(preset);
        }
    }
}
