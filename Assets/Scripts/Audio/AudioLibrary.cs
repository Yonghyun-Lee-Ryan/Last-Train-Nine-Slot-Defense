using System.Collections.Generic;
using UnityEngine;

namespace LastTrain.Audio
{
    /// <summary>Resources/Audio 아래 클립을 로드해 ID로 제공한다.</summary>
    public sealed class AudioLibrary
    {
        private readonly Dictionary<SfxId, AudioClip> _sfx = new();
        private readonly Dictionary<BgmId, AudioClip> _bgm = new();
        private readonly Dictionary<SfxId, string> _sfxPaths = new();
        private readonly Dictionary<BgmId, string> _bgmPaths = new();
        private bool _pathsReady;

        public void Load()
        {
            EnsurePaths();
            _sfx.Clear();
            _bgm.Clear();

            foreach (KeyValuePair<SfxId, string> pair in _sfxPaths)
            {
                BindSfx(pair.Key, pair.Value);
            }

            foreach (KeyValuePair<BgmId, string> pair in _bgmPaths)
            {
                BindBgm(pair.Key, pair.Value);
            }
        }

        /// <summary>언로드된 클립이 있으면 Resources에서 다시 로드한다.</summary>
        public void EnsureLoaded()
        {
            EnsurePaths();

            foreach (KeyValuePair<SfxId, string> pair in _sfxPaths)
            {
                if (!_sfx.TryGetValue(pair.Key, out AudioClip clip) || clip == null)
                {
                    BindSfx(pair.Key, pair.Value);
                }
            }

            foreach (KeyValuePair<BgmId, string> pair in _bgmPaths)
            {
                if (!_bgm.TryGetValue(pair.Key, out AudioClip clip) || clip == null)
                {
                    BindBgm(pair.Key, pair.Value);
                }
            }
        }

        public bool TryGetSfx(SfxId id, out AudioClip clip)
        {
            EnsureLoaded();
            return _sfx.TryGetValue(id, out clip) && clip != null;
        }

        public bool TryGetBgm(BgmId id, out AudioClip clip)
        {
            EnsureLoaded();
            return _bgm.TryGetValue(id, out clip) && clip != null;
        }

        private void EnsurePaths()
        {
            if (_pathsReady)
            {
                return;
            }

            _pathsReady = true;
            _sfxPaths[SfxId.UiClick] = "Audio/Sfx/ui_click";
            _sfxPaths[SfxId.UiConfirm] = "Audio/Sfx/ui_confirm";
            _sfxPaths[SfxId.UiCancel] = "Audio/Sfx/ui_cancel";
            _sfxPaths[SfxId.UiError] = "Audio/Sfx/ui_error";
            _sfxPaths[SfxId.UiOpen] = "Audio/Sfx/ui_open";
            _sfxPaths[SfxId.UiClose] = "Audio/Sfx/ui_close";
            _sfxPaths[SfxId.UiToggle] = "Audio/Sfx/ui_toggle";
            _sfxPaths[SfxId.SummonOpen] = "Audio/Sfx/summon_open";
            _sfxPaths[SfxId.SummonSelect] = "Audio/Sfx/summon_select";
            _sfxPaths[SfxId.ShopBuy] = "Audio/Sfx/shop_buy";
            _sfxPaths[SfxId.Pause] = "Audio/Sfx/pause";
            _sfxPaths[SfxId.Resume] = "Audio/Sfx/resume";
            _sfxPaths[SfxId.Reward] = "Audio/Sfx/reward";
            _sfxPaths[SfxId.Switch] = "Audio/Sfx/switch";
            _sfxPaths[SfxId.CombatHit] = "Audio/Sfx/combat_hit";
            _sfxPaths[SfxId.CombatCrit] = "Audio/Sfx/combat_crit";
            _sfxPaths[SfxId.EnemyDeath] = "Audio/Sfx/enemy_death";
            _sfxPaths[SfxId.TrainDamage] = "Audio/Sfx/train_damage";
            _sfxPaths[SfxId.Coin] = "Audio/Sfx/coin";
            _sfxPaths[SfxId.Merge] = "Audio/Sfx/merge";
            _sfxPaths[SfxId.WaveStart] = "Audio/Sfx/wave_start";
            _sfxPaths[SfxId.StationClear] = "Audio/Sfx/station_clear";
            _sfxPaths[SfxId.Victory] = "Audio/Sfx/victory";
            _sfxPaths[SfxId.Defeat] = "Audio/Sfx/defeat";
            _sfxPaths[SfxId.BossSpawn] = "Audio/Sfx/boss_spawn";

            _bgmPaths[BgmId.Menu] = "Audio/Bgm/bgm_menu";
            _bgmPaths[BgmId.Battle] = "Audio/Bgm/bgm_battle";
            _bgmPaths[BgmId.Result] = "Audio/Bgm/bgm_result";
        }

        private void BindSfx(SfxId id, string path)
        {
            AudioClip clip = Resources.Load<AudioClip>(path);
            if (clip != null)
            {
                _sfx[id] = clip;
            }
            else
            {
                Debug.LogWarning($"[AudioLibrary] SFX 로드 실패: {path}");
            }
        }

        private void BindBgm(BgmId id, string path)
        {
            AudioClip clip = Resources.Load<AudioClip>(path);
            if (clip != null)
            {
                _bgm[id] = clip;
            }
            else
            {
                Debug.LogWarning($"[AudioLibrary] BGM 로드 실패: {path}");
            }
        }
    }
}
