using LastTrain.Release;
using UnityEngine;

namespace LastTrain.Audio
{
    /// <summary>
    /// 전투/UI 사운드 진입점. AudioManager가 없어도 안전하게 no-op.
    /// SFX 중첩 제한과 Mixer 볼륨 분리를 담당한다.
    /// </summary>
    public static class AudioService
    {
        private static AudioData _data;
        private static readonly float[] NextAllowedBySfx = new float[64];

        public static AudioData Data => _data;

        public static void Initialize(AudioData data)
        {
            _data = data;
            for (int i = 0; i < NextAllowedBySfx.Length; i++)
            {
                NextAllowedBySfx[i] = 0f;
            }
        }

        public static void EnsureInitialized()
        {
            if (_data != null)
            {
                return;
            }

            Initialize(AudioData.LoadOrNull());
        }

        public static void PlaySfx(SfxId id, float pitch = 1f)
        {
            EnsureInitialized();
            if (!CanPlaySfx(id))
            {
                return;
            }

            MarkPlayed(id);
            AudioManager.Instance?.PlaySfxInternal(id, pitch);
        }

        public static void PlayBgm(BgmId id, bool restartIfSame = false)
        {
            EnsureInitialized();
            AudioManager.Instance?.PlayBgm(id, restartIfSame);
        }

        public static void StopBgm()
        {
            AudioManager.Instance?.StopBgm();
        }

        public static void ApplySettings(GameSettingsService settings = null)
        {
            EnsureInitialized();
            AudioManager.Instance?.ApplySettings();
        }

        public static bool CanPlaySfx(SfxId id)
        {
            EnsureInitialized();
            int index = (int)id;
            if (index < 0 || index >= NextAllowedBySfx.Length)
            {
                return true;
            }

            return Time.unscaledTime >= NextAllowedBySfx[index];
        }

        public static void ResetThrottleForTests()
        {
            for (int i = 0; i < NextAllowedBySfx.Length; i++)
            {
                NextAllowedBySfx[i] = 0f;
            }
        }

        private static void MarkPlayed(SfxId id)
        {
            int index = (int)id;
            if (index < 0 || index >= NextAllowedBySfx.Length)
            {
                return;
            }

            float interval = _data != null ? _data.GetSfxMinInterval(id) : DefaultInterval(id);
            NextAllowedBySfx[index] = Time.unscaledTime + interval;
        }

        private static float DefaultInterval(SfxId id)
        {
            return id switch
            {
                SfxId.CombatHit => 0.04f,
                SfxId.CombatCrit => 0.08f,
                SfxId.EnemyDeath => 0.06f,
                SfxId.Coin => 0.05f,
                _ => 0f,
            };
        }
    }
}
