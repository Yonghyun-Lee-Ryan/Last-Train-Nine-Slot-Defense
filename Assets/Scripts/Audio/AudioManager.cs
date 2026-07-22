using LastTrain.Core;
using LastTrain.Release;
using UnityEngine;
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
        private BgmId _currentBgm = BgmId.None;
        private float _bgmVolume = 0.7f;
        private float _sfxVolume = 0.85f;

        public void Initialize(GameSettingsService settings)
        {
            _settings = settings;
            _library.Load();
            EnsureRuntime();
            ApplySettings();
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
            if (_bgmVolume > 0.001f)
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

        public void PlaySfx(SfxId id, float pitch = 1f)
        {
            EnsureRuntime();
            ResolveVolumesFromSettings();
            if (_sfxVolume <= 0.001f)
            {
                return;
            }

            _library.EnsureLoaded();
            if (!_library.TryGetSfx(id, out AudioClip clip))
            {
                return;
            }

            // PlayOneShot의 volumeScale로 효과음 볼륨을 직접 적용한다.
            _sfxSource.mute = false;
            _sfxSource.volume = 1f;
            _sfxSource.pitch = Mathf.Clamp(pitch, 0.5f, 1.5f);
            _sfxSource.PlayOneShot(clip, Mathf.Clamp01(_sfxVolume));
            _sfxSource.pitch = 1f;
        }

        private void ResolveVolumesFromSettings()
        {
            if (_settings == null)
            {
                return;
            }

            _bgmVolume = _settings.BgmEnabled ? Mathf.Clamp01(_settings.BgmVolume) : 0f;
            _sfxVolume = _settings.SfxEnabled ? Mathf.Clamp01(_settings.SfxVolume) : 0f;
        }

        private void ApplyVolumesToSources()
        {
            if (_bgmSource != null)
            {
                _bgmSource.volume = Mathf.Clamp01(_bgmVolume);
                _bgmSource.mute = _bgmVolume <= 0.001f;
                if (_bgmVolume > 0.001f
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
                // 실제 배율은 PlayOneShot volumeScale에서 적용
                _sfxSource.volume = 1f;
                _sfxSource.mute = _sfxVolume <= 0.001f;
            }
        }

        private void EnsureRuntime()
        {
            EnsureSources();
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
                Time.timeScale = 1f;
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

    /// <summary>정적 진입점. AudioManager가 없어도 안전하게 no-op.</summary>
    public static class GameAudio
    {
        public static void PlaySfx(SfxId id, float pitch = 1f)
        {
            AudioManager.Instance?.PlaySfx(id, pitch);
        }

        public static void PlayBgm(BgmId id, bool restartIfSame = false)
        {
            AudioManager.Instance?.PlayBgm(id, restartIfSame);
        }

        public static void StopBgm()
        {
            AudioManager.Instance?.StopBgm();
        }

        public static void ApplySettings()
        {
            AudioManager.Instance?.ApplySettings();
        }
    }
}
