using LastTrain.Release;
using UnityEngine;

namespace LastTrain.Performance
{
    /// <summary>Unit 54: 저사양 60 FPS 목표와 LowFx 권고.</summary>
    public static class LowEndFramePolicy
    {
        public const int TargetFrameRate = 60;
        public const int FrameBudgetMilliseconds = 17;
        public const int LowMemoryMegabytes = 3072;
        private const string LowFxKey = "lasttrain.settings.lowFx";

        public static bool ShouldRecommendLowFx(int systemMemoryMegabytes)
        {
            return systemMemoryMegabytes > 0 && systemMemoryMegabytes < LowMemoryMegabytes;
        }

        public static void ApplyFrameRate()
        {
            Application.targetFrameRate = TargetFrameRate;
            QualitySettings.vSyncCount = 0;
        }

        /// <summary>사용자가 LowFx를 한 번도 저장하지 않았고 저메모리면 자동 권고.</summary>
        public static bool TryRecommendLowFx(GameSettingsService settings, int systemMemoryMegabytes)
        {
            if (settings == null || !ShouldRecommendLowFx(systemMemoryMegabytes))
            {
                return false;
            }

            if (PlayerPrefs.HasKey(LowFxKey))
            {
                return settings.LowFxMode;
            }

            settings.SetLowFxMode(true);
            return true;
        }
    }
}
