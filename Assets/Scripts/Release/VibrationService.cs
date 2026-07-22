using UnityEngine;

namespace LastTrain.Release
{
    public static class VibrationService
    {
        public static void PlayLight(GameSettingsService settings)
        {
            if (settings == null || !settings.VibrationEnabled)
            {
                return;
            }

#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }
    }
}
