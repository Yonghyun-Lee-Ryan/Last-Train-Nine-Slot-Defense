using LastTrain.Battle;
using LastTrain.Core;
using LastTrain.Release;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace LastTrain.Audio
{
    /// <summary>BGM/SFX 재생. AppRoot가 생성하며 씬 전환에도 유지한다.</summary>
    public sealed class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private readonly AudioLibrary _library = new();
        private AudioSource _bgmSource;
        private AudioSource _sfxSource;
        private AudioListener _listener;
        private GameSettingsService _settings;
        private AudioData _audioData;
        private AudioMixer _mixer;
        private AudioMixerGroup _bgmGroup;
        private AudioMixerGroup _sfxGroup;
        private BgmId _currentBgm = BgmId.None;
        private float _bgmVolume = 0.7f;
        private float _sfxVolume = 0.85f;
        private float _masterVolume = 1f;

        public void Initialize(GameSettingsService settings)
        {
            _settings = settings;
            _library.Load();
            BindAudioData(AudioData.LoadOrNull());
            EnsureRuntime();
            ApplySettings();
        }

        public void BindAudioData(AudioData data)
        {
            _audioData = data;
            AudioService.Initialize(data);
            _mixer = data != null ? data.Mixer : null;
            ResolveMixerGroups();
            AssignMixerGroupsToSources();
        }

        public void ApplySettings()
        {
            if (_settings == null && AppRoot.Instance != null)
            {
                _settings = AppRoot.Instance.GameSettings;
            }

            EnsureRuntime();
            ResolveVolumesFromSettings();
            ApplyVolumesToSources();
            ApplyMixerVolumes();
            AudioListener.pause = false;
            AudioListener.volume = 1f;
        }

        public void PlayBgm(BgmId id, bool restartIfSame = false)
        {
            EnsureRuntime();
            _library.EnsureLoaded();
            ResolveVolumesFromSettings();

            if (!restartIfSame && _currentBgm == id && _bgmSource != null && _bgmSource.isPlaying
                && _bgmSource.clip != null)
            {
                ApplyVolumesToSources();
                ApplyMixerVolumes();
                return;
            }

            if (id == BgmId.None)
            {
                StopBgm();
                return;
            }

            if (!_library.TryGetBgm(id, out AudioClip clip))
            {
                return;
            }

            _currentBgm = id;
            _bgmSource.clip = clip;
            _bgmSource.loop = true;
            ApplyVolumesToSources();
            ApplyMixerVolumes();
            if (_bgmVolume > 0.001f && _masterVolume > 0.001f)
            {
                _bgmSource.UnPause();
                _bgmSource.Play();
            }
            else
            {
                _bgmSource.Pause();
            }
        }

        public void StopBgm()
        {
            EnsureRuntime();
            _currentBgm = BgmId.None;
            if (_bgmSource != null)
            {
                _bgmSource.Stop();
                _bgmSource.clip = null;
            }
        }

        /// <summary>외부에서는 AudioService.PlaySfx를 사용한다. 스로틀은 AudioService가 담당.</summary>
        public void PlaySfx(SfxId id, float pitch = 1f)
        {
            AudioService.PlaySfx(id, pitch);
        }

        internal void PlaySfxInternal(SfxId id, float pitch = 1f)
        {
            EnsureRuntime();
            ResolveVolumesFromSettings();
            if (_sfxVolume <= 0.001f || _masterVolume <= 0.001f)
            {
                return;
            }

            _library.EnsureLoaded();
            if (!_library.TryGetSfx(id, out AudioClip clip))
            {
                return;
            }

            float scale = _mixer != null
                ? 1f
                : Mathf.Clamp01(_sfxVolume * _masterVolume);

            _sfxSource.mute = false;
            _sfxSource.volume = 1f;
            _sfxSource.pitch = Mathf.Clamp(pitch, 0.5f, 1.5f);
            _sfxSource.PlayOneShot(clip, scale);
            _sfxSource.pitch = 1f;
        }

        private void ResolveVolumesFromSettings()
        {
            if (_settings == null)
            {
                return;
            }

            _masterVolume = 1f;
            _bgmVolume = _settings.BgmEnabled ? Mathf.Clamp01(_settings.BgmVolume) : 0f;
            _sfxVolume = _settings.SfxEnabled ? Mathf.Clamp01(_settings.SfxVolume) : 0f;
        }

        private void ApplyVolumesToSources()
        {
            if (_bgmSource != null)
            {
                // Mixer가 있으면 그룹 볼륨으로 제어하고, 없으면 소스 볼륨으로 폴백한다.
                _bgmSource.volume = _mixer != null ? 1f : Mathf.Clamp01(_bgmVolume * _masterVolume);
                _bgmSource.mute = _bgmVolume <= 0.001f || _masterVolume <= 0.001f;
                if (_bgmVolume > 0.001f
                    && _masterVolume > 0.001f
                    && _currentBgm != BgmId.None
                    && _bgmSource.clip != null
                    && !_bgmSource.isPlaying)
                {
                    _bgmSource.UnPause();
                    _bgmSource.Play();
                }
            }

            if (_sfxSource != null)
            {
                _sfxSource.volume = 1f;
                _sfxSource.mute = _sfxVolume <= 0.001f || _masterVolume <= 0.001f;
            }
        }

        private void ApplyMixerVolumes()
        {
            if (_mixer == null || _audioData == null)
            {
                return;
            }

            SetMixerVolumeDb(_audioData.MasterVolumeParam, _masterVolume);
            SetMixerVolumeDb(_audioData.BgmVolumeParam, _bgmVolume);
            SetMixerVolumeDb(_audioData.SfxVolumeParam, _sfxVolume);
        }

        private void SetMixerVolumeDb(string param, float linear01)
        {
            if (string.IsNullOrWhiteSpace(param))
            {
                return;
            }

            float clamped = Mathf.Clamp01(linear01);
            float db = clamped <= 0.0001f ? -80f : Mathf.Log10(clamped) * 20f;
            _mixer.SetFloat(param, db);
        }

        private void ResolveMixerGroups()
        {
            _bgmGroup = null;
            _sfxGroup = null;
            if (_mixer == null)
            {
                return;
            }

            AudioMixerGroup[] bgm = _mixer.FindMatchingGroups("BGM");
            if (bgm != null && bgm.Length > 0)
            {
                _bgmGroup = bgm[0];
            }

            AudioMixerGroup[] sfx = _mixer.FindMatchingGroups("SFX");
            if (sfx != null && sfx.Length > 0)
            {
                _sfxGroup = sfx[0];
            }
        }

        private void AssignMixerGroupsToSources()
        {
            EnsureSources();
            if (_bgmSource != null)
            {
                _bgmSource.outputAudioMixerGroup = _bgmGroup;
            }

            if (_sfxSource != null)
            {
                _sfxSource.outputAudioMixerGroup = _sfxGroup;
            }
        }

        private void EnsureRuntime()
        {
            EnsureSources();
            AssignMixerGroupsToSources();
            EnsureListener();
            AudioListener.pause = false;
        }

        private void EnsureListener()
        {
            if (_listener == null)
            {
                _listener = GetComponent<AudioListener>();
            }

            if (_listener == null)
            {
                _listener = gameObject.AddComponent<AudioListener>();
            }

            _listener.enabled = true;

            AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Include);
            for (int i = 0; i < listeners.Length; i++)
            {
                AudioListener other = listeners[i];
                if (other != null && other != _listener)
                {
                    other.enabled = false;
                }
            }
        }

        private void EnsureSources()
        {
            if (_bgmSource == null)
            {
                AudioSource[] sources = gameObject.GetComponents<AudioSource>();
                for (int i = 0; i < sources.Length; i++)
                {
                    if (sources[i] != null && sources[i].gameObject == gameObject)
                    {
                        _bgmSource = sources[i];
                        break;
                    }
                }

                if (_bgmSource == null)
                {
                    _bgmSource = gameObject.AddComponent<AudioSource>();
                }
            }

            _bgmSource.playOnAwake = false;
            _bgmSource.loop = true;
            _bgmSource.spatialBlend = 0f;
            _bgmSource.ignoreListenerPause = true;

            if (_sfxSource == null)
            {
                Transform sfxHost = transform.Find("SfxSource");
                if (sfxHost == null)
                {
                    var go = new GameObject("SfxSource");
                    go.transform.SetParent(transform, false);
                    sfxHost = go.transform;
                }

                _sfxSource = sfxHost.GetComponent<AudioSource>();
                if (_sfxSource == null)
                {
                    _sfxSource = sfxHost.gameObject.AddComponent<AudioSource>();
                }
            }

            _sfxSource.playOnAwake = false;
            _sfxSource.loop = false;
            _sfxSource.spatialBlend = 0f;
            _sfxSource.ignoreListenerPause = true;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!scene.IsValid())
            {
                return;
            }

            if (Time.timeScale <= 0f)
            {
                Time.timeScale = scene.name == SceneNames.Game
                    ? BattleSpeedRuntime.GetTimeScaleFromSettings()
                    : 1f;
            }

            EnsureRuntime();
            _library.EnsureLoaded();
            ApplySettings();

            switch (scene.name)
            {
                case SceneNames.MainMenu:
                    PlayBgm(BgmId.Menu, restartIfSame: true);
                    break;
                case SceneNames.Game:
                    PlayBgm(BgmId.Battle, restartIfSame: true);
                    break;
                case SceneNames.Result:
                    PlayBgm(BgmId.Result, restartIfSame: true);
                    break;
            }
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }

    /// <summary>정적 진입점. AudioManager가 없어도 안전하게 no-op. SFX는 AudioService 스로틀을 탄다.</summary>
    public static class GameAudio
    {
        public static void PlaySfx(SfxId id, float pitch = 1f)
        {
            AudioService.PlaySfx(id, pitch);
        }

        public static void PlayBgm(BgmId id, bool restartIfSame = false)
        {
            AudioService.PlayBgm(id, restartIfSame);
        }

        public static void StopBgm()
        {
            AudioService.StopBgm();
        }

        public static void ApplySettings()
        {
            AudioService.ApplySettings();
        }
    }
}
