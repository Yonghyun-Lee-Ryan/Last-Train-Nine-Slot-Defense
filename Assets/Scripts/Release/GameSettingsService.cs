using UnityEngine;

namespace LastTrain.Release
{
    /// <summary>사운드·진동·알림 설정을 PlayerPrefs에 저장하고 런타임에 적용한다.</summary>
    public sealed class GameSettingsService
    {
        private const string BgmKey = "lasttrain.settings.bgm";
        private const string SfxKey = "lasttrain.settings.sfx";
        private const string BgmVolumeKey = "lasttrain.settings.bgmVolume";
        private const string SfxVolumeKey = "lasttrain.settings.sfxVolume";
        private const string VibrationKey = "lasttrain.settings.vibration";
        private const string NotificationKey = "lasttrain.settings.notification";
        private const string ScreenShakeKey = "lasttrain.settings.screenShake";
        private const string DamageNumbersKey = "lasttrain.settings.damageNumbers";
        private const string CoinNumbersKey = "lasttrain.settings.coinNumbers";
        private const string LowFxKey = "lasttrain.settings.lowFx";
        private const string BattleSpeedKey = "lasttrain.settings.battleSpeed";

        public bool BgmEnabled { get; private set; } = true;
        public bool SfxEnabled { get; private set; } = true;
        public float BgmVolume { get; private set; } = 0.7f;
        public float SfxVolume { get; private set; } = 0.85f;
        public bool VibrationEnabled { get; private set; } = true;
        public bool NotificationsEnabled { get; private set; } = true;
        public bool ScreenShakeEnabled { get; private set; } = true;
        public bool DamageNumbersEnabled { get; private set; } = true;
        public bool CoinNumbersEnabled { get; private set; } = true;
        public bool LowFxMode { get; private set; }
        public int BattleSpeed { get; private set; } = 1;

        public void Load()
        {
            BgmEnabled = PlayerPrefs.GetInt(BgmKey, 1) == 1;
            SfxEnabled = PlayerPrefs.GetInt(SfxKey, 1) == 1;
            BgmVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(BgmVolumeKey, 0.7f));
            SfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, 0.85f));
            VibrationEnabled = PlayerPrefs.GetInt(VibrationKey, 1) == 1;
            NotificationsEnabled = PlayerPrefs.GetInt(NotificationKey, 1) == 1;
            ScreenShakeEnabled = PlayerPrefs.GetInt(ScreenShakeKey, 1) == 1;
            DamageNumbersEnabled = PlayerPrefs.GetInt(DamageNumbersKey, 1) == 1;
            CoinNumbersEnabled = PlayerPrefs.GetInt(CoinNumbersKey, 1) == 1;
            LowFxMode = PlayerPrefs.GetInt(LowFxKey, 0) == 1;
            BattleSpeed = LastTrain.Battle.BattleSpeedPreset.Clamp(PlayerPrefs.GetInt(BattleSpeedKey, 1));
            ApplyAudio();
        }

        public void SetBgmEnabled(bool enabled)
        {
            BgmEnabled = enabled;
            PlayerPrefs.SetInt(BgmKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
            ApplyAudio();
        }

        public void SetSfxEnabled(bool enabled)
        {
            SfxEnabled = enabled;
            PlayerPrefs.SetInt(SfxKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
            ApplyAudio();
        }

        public void SetBgmVolume(float volume)
        {
            BgmVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(BgmVolumeKey, BgmVolume);
            ApplyAudio();
            // 드래그 중 매 프레임 Save는 비용이 커서 값만 반영하고, 토글/종료 시 Save한다.
        }

        public void SetSfxVolume(float volume)
        {
            SfxVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(SfxVolumeKey, SfxVolume);
            ApplyAudio();
        }

        public void Persist()
        {
            PlayerPrefs.Save();
        }

        public void SetVibrationEnabled(bool enabled)
        {
            VibrationEnabled = enabled;
            PlayerPrefs.SetInt(VibrationKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void SetNotificationsEnabled(bool enabled)
        {
            NotificationsEnabled = enabled;
            PlayerPrefs.SetInt(NotificationKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void SetScreenShakeEnabled(bool enabled)
        {
            ScreenShakeEnabled = enabled;
            PlayerPrefs.SetInt(ScreenShakeKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void SetDamageNumbersEnabled(bool enabled)
        {
            DamageNumbersEnabled = enabled;
            PlayerPrefs.SetInt(DamageNumbersKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void SetCoinNumbersEnabled(bool enabled)
        {
            CoinNumbersEnabled = enabled;
            PlayerPrefs.SetInt(CoinNumbersKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void SetLowFxMode(bool enabled)
        {
            LowFxMode = enabled;
            PlayerPrefs.SetInt(LowFxKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void SetBattleSpeed(int preset)
        {
            BattleSpeed = LastTrain.Battle.BattleSpeedPreset.Clamp(preset);
            PlayerPrefs.SetInt(BattleSpeedKey, BattleSpeed);
            PlayerPrefs.Save();
        }

        public void ResetToDefaults()
        {
            BgmEnabled = true;
            SfxEnabled = true;
            BgmVolume = 0.7f;
            SfxVolume = 0.85f;
            VibrationEnabled = true;
            NotificationsEnabled = true;
            ScreenShakeEnabled = true;
            DamageNumbersEnabled = true;
            CoinNumbersEnabled = true;
            LowFxMode = false;
            BattleSpeed = 1;
            PlayerPrefs.DeleteKey(BgmKey);
            PlayerPrefs.DeleteKey(SfxKey);
            PlayerPrefs.DeleteKey(BgmVolumeKey);
            PlayerPrefs.DeleteKey(SfxVolumeKey);
            PlayerPrefs.DeleteKey(VibrationKey);
            PlayerPrefs.DeleteKey(NotificationKey);
            PlayerPrefs.DeleteKey(ScreenShakeKey);
            PlayerPrefs.DeleteKey(DamageNumbersKey);
            PlayerPrefs.DeleteKey(CoinNumbersKey);
            PlayerPrefs.DeleteKey(LowFxKey);
            PlayerPrefs.DeleteKey(BattleSpeedKey);
            PlayerPrefs.Save();
            ApplyAudio();
        }

        public void ApplyAudio()
        {
            LastTrain.Audio.GameAudio.ApplySettings();
        }

        public bool CanPlaySfx() => SfxEnabled && SfxVolume > 0.001f;
        public bool CanPlayBgm() => BgmEnabled && BgmVolume > 0.001f;
    }
}
