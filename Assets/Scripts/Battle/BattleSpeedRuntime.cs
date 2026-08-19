using System;
using LastTrain.Core;
using LastTrain.Release;
using UnityEngine;

namespace LastTrain.Battle
{
    /// <summary>전역 timeScale과 설정 배속 프리셋을 동기화한다.</summary>
    public static class BattleSpeedRuntime
    {
        /// <summary>배속 UI·HUD가 광고 종료 후 timeScale을 다시 맞출 때 구독한다.</summary>
        public static event Action TimeScaleRestoreRequested;

        private static int _presetBeforeAdOverlay = 1;

        public static int GetPresetFromSettings()
        {
            if (AppRoot.Instance?.GameSettings != null)
            {
                return BattleSpeedPreset.Clamp(AppRoot.Instance.GameSettings.BattleSpeed);
            }

            return BattleSpeedPreset.Clamp(PlayerPrefs.GetInt("lasttrain.settings.battleSpeed", 1));
        }

        public static float GetTimeScaleFromSettings()
        {
            return BattleSpeedPreset.ToTimeScale(GetPresetFromSettings());
        }

        /// <summary>광고 표시 직전에 호출해 현재 배속 프리셋을 고정한다.</summary>
        public static void BeginAdOverlay()
        {
            _presetBeforeAdOverlay = GetPresetFromSettings();
        }

        /// <summary>일시정지(timeScale=0)가 아니면 설정 배속으로 timeScale을 복구한다.</summary>
        public static void RestoreTimeScaleFromSettings()
        {
            if (Mathf.Approximately(Time.timeScale, 0f))
            {
                return;
            }

            ApplyPreset(GetPresetFromSettings());
        }

        /// <summary>광고·OS 오버레이 직후에는 광고 시작 시점 배속으로 복구한다.</summary>
        public static void RestoreTimeScaleAfterAd()
        {
            ApplyPreset(_presetBeforeAdOverlay);
        }

        private static void ApplyPreset(int preset)
        {
            preset = BattleSpeedPreset.Clamp(preset);
            Time.timeScale = BattleSpeedPreset.ToTimeScale(preset);
            TimeScaleRestoreRequested?.Invoke();
        }
    }
}
