using LastTrain.Ads;
using LastTrain.Analytics;
using LastTrain.Audio;
using LastTrain.Integrations;
using LastTrain.Release;
using LastTrain.Run;
using LastTrain.Save;
using UnityEngine;

namespace LastTrain.Core
{
    /// <summary>
    /// 앱 전역 진입점. Bootstrap Scene에 배치한다.
    /// DontDestroyOnLoad로 유지되며 Scene 전환과 무관하게 살아남는다.
    /// 중복 생성을 방지하고, 초기화 완료 후 MainMenu로 이동한다.
    ///
    /// 다른 시스템에서 SceneLoader가 필요하면 AppRoot.Instance.SceneLoader로 접근한다.
    /// 무분별한 Singleton 확산을 막기 위해 전역 접근점은 AppRoot 하나로 제한한다.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class AppRoot : MonoBehaviour
    {
        public static AppRoot Instance { get; private set; }

        [Tooltip("Bootstrap 초기화 완료 후 자동으로 MainMenu로 이동할지 여부")]
        [SerializeField] private bool autoLoadMainMenu = true;

        [Header("Integration (Unit 21)")]
        [SerializeField] private AdUnitConfig adUnitConfig;
        [SerializeField] private RemoteConfigDefaults remoteConfigDefaults;

        private SceneLoader _sceneLoader;
        private GameSession _gameSession;
        private IntegrationBootstrap _integrations;
        private AdCoordinator _ads;
        private AnalyticsCoordinator _analytics;
        private AnalyticsRunBinder _analyticsRunBinder;
        private readonly GameSettingsService _gameSettings = new GameSettingsService();
        private bool _subscribedRunEnded;
        private bool _subscribedRunStarted;
        private bool _subscribedRevive;

        /// <summary>비동기 Scene 전환 담당. AppRoot 생성 시 함께 초기화된다.</summary>
        public SceneLoader SceneLoader => _sceneLoader;

        /// <summary>현재 게임 세션. Scene 전환 후에도 유지된다.</summary>
        public GameSession GameSession => _gameSession ??= new GameSession();

        /// <summary>광고·Firebase·Remote Config 통합 부트스트랩.</summary>
        public IntegrationBootstrap Integrations => _integrations;

        /// <summary>개인정보 동의 상태.</summary>
        public PrivacyConsentService Privacy => _integrations?.Privacy;

        /// <summary>광고 추상화 진입점. SDK 타입을 노출하지 않는다.</summary>
        public AdCoordinator Ads => _ads;

        /// <summary>분석 이벤트 진입점. SDK 타입을 노출하지 않는다.</summary>
        public AnalyticsCoordinator Analytics => _analytics;

        /// <summary>전투 씬 이벤트 바인더. GameBattleBootstrap이 사용한다.</summary>
        public AnalyticsRunBinder AnalyticsRunBinder => _analyticsRunBinder;

        /// <summary>사운드·진동·알림 설정.</summary>
        public GameSettingsService GameSettings => _gameSettings;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[AppRoot] 이미 인스턴스가 존재합니다. 중복 AppRoot를 제거합니다.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Initialize();

            if (!_subscribedRunEnded)
            {
                GameSession.RunEnded += HandleRunEnded;
                _subscribedRunEnded = true;
            }

            if (!_subscribedRunStarted)
            {
                GameSession.RunStarted += HandleRunStarted;
                _subscribedRunStarted = true;
            }

            if (!_subscribedRevive)
            {
                GameSession.ReviveOffered += HandleReviveOffered;
                _subscribedRevive = true;
            }
        }

        private void Initialize()
        {
            ApplyApplicationSettings();
            _gameSettings.Load();
            EnsureAudio();

            _sceneLoader = GetComponent<SceneLoader>();
            if (_sceneLoader == null)
            {
                _sceneLoader = gameObject.AddComponent<SceneLoader>();
            }

            EnsureIntegrations();
            EnsureAnalytics();
            EnsureAds();
            _analytics.Track(AnalyticsEventNames.AppStarted);
            Debug.Log("[AppRoot] 초기화 완료.");
        }

        private void EnsureIntegrations()
        {
            if (_integrations != null)
            {
                return;
            }

            _integrations = new IntegrationBootstrap();
            _integrations.Initialize(
                adUnitConfig,
                remoteConfigDefaults,
                _sceneLoader,
                () => GameSession);
        }

        private void EnsureAnalytics()
        {
            if (_analytics != null)
            {
                return;
            }

            EnsureIntegrations();
            _analytics = _integrations.CreateAnalyticsCoordinator();
            _analyticsRunBinder = new AnalyticsRunBinder(_analytics);
        }

        private void EnsureAds()
        {
            if (_ads != null)
            {
                _ads.Analytics ??= _analytics;
                return;
            }

            EnsureIntegrations();
            EnsureAnalytics();
            _ads = _integrations.CreateAdCoordinator(_analytics);
        }

        private void EnsureAudio()
        {
            AudioManager audio = AudioManager.Instance;
            if (audio == null)
            {
                audio = GetComponentInChildren<AudioManager>(true);
            }

            if (audio == null)
            {
                var go = new GameObject("AudioManager");
                audio = go.AddComponent<AudioManager>();
            }

            audio.Initialize(_gameSettings);
        }

        /// <summary>동의 변경 후 광고·분석 서비스를 PlayerPrefs 상태에 맞게 재구성한다.</summary>
        public void ApplyPrivacyConsent(bool adsGranted, bool analyticsGranted)
        {
            EnsureIntegrations();
            Privacy?.SetAdsConsent(adsGranted);
            Privacy?.SetAnalyticsConsent(analyticsGranted);
            Privacy?.MarkConsentPromptCompleted();
            RebuildIntegrationServices();
        }

        private void RebuildIntegrationServices()
        {
            _analyticsRunBinder?.Dispose();
            _analyticsRunBinder = null;
            _integrations?.Dispose();
            _integrations = null;
            _ads = null;
            _analytics = null;

            EnsureIntegrations();
            EnsureAnalytics();
            EnsureAds();
        }

        private void Start()
        {
            if (autoLoadMainMenu)
            {
                _sceneLoader.LoadScene(SceneNames.MainMenu);
            }
        }

        private void HandleRunStarted(RunState runState)
        {
            EnsureAnalytics();
            EnsureAds();
            _ads.Limits.BeginRun();
            _analytics.BindRun(runState, runState?.DifficultyId);
            _analytics.Track(AnalyticsEventNames.RunStarted, new System.Collections.Generic.Dictionary<string, object>
            {
                ["initial_coins"] = runState?.Currency?.CurrentCoins ?? 0,
                ["train_max_hp"] = runState?.Train?.MaxHp ?? 0,
                ["difficulty_id"] = runState?.DifficultyId ?? Difficulty.DifficultyIds.Normal,
            });
        }

        private void HandleReviveOffered()
        {
            EnsureAds();
            GameSession session = GameSession;
            if (session == null || !session.IsPendingDefeat)
            {
                return;
            }

            if (!_ads.IsReady(RewardedAdPlacement.Revive) || !session.ReviveAvailableThisRun)
            {
                session.DeclineReviveAndEnd();
                return;
            }

            // UI가 없으면 AppRoot가 바로 Mock/광고를 띄운다.
            GameAudio.PlaySfx(SfxId.UiOpen);
            _ads.ShowRevive(session, result =>
            {
                if (result == AdResult.Completed)
                {
                    GameAudio.PlaySfx(SfxId.Reward);
                }
                else
                {
                    session.DeclineReviveAndEnd();
                }
            });
        }

        private void HandleRunEnded(RunResult result)
        {
            EnsureAnalytics();
            RunState snapshot = GameSession?.RunState;
            _analytics.TrackRunEnded(result, snapshot);

            if (result != null)
            {
                MetaApplyResult apply = MetaSaveSystem.ApplyRunResult(result);
                TrackMetaRewards(apply);
            }

            RunSaveSystem.DeleteRunSave();
            _analytics.ClearRun();
            _integrations?.NotifyRunCompleted();
        }

        private void TrackMetaRewards(MetaApplyResult apply)
        {
            if (apply == null || !apply.Applied || apply.WasDuplicate)
            {
                return;
            }

            MetaRewardBreakdown breakdown = apply.Breakdown;
            _analytics.Track(AnalyticsEventNames.MetaRewardReceived, new System.Collections.Generic.Dictionary<string, object>
            {
                ["tickets"] = breakdown?.TotalTickets ?? 0,
                ["ticket_fragments_after"] = apply.TicketFragmentsAfter,
                ["account_level_after"] = apply.AccountLevelAfter,
            });

            if (breakdown?.NewlyUnlockedPassengers == null)
            {
                return;
            }

            for (int i = 0; i < breakdown.NewlyUnlockedPassengers.Count; i++)
            {
                string id = breakdown.NewlyUnlockedPassengers[i];
                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                _analytics.Track(AnalyticsEventNames.PassengerUnlocked, new System.Collections.Generic.Dictionary<string, object>
                {
                    ["passenger_id"] = id,
                });
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus)
            {
                return;
            }

            RunSaveSystem.TrySavePreparing(GameSession);
        }

        private void OnApplicationQuit()
        {
            RunSaveSystem.TrySavePreparing(GameSession);
        }

        private void ApplyApplicationSettings()
        {
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;

            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                if (_subscribedRunEnded)
                {
                    GameSession.RunEnded -= HandleRunEnded;
                    _subscribedRunEnded = false;
                }

                if (_subscribedRunStarted)
                {
                    GameSession.RunStarted -= HandleRunStarted;
                    _subscribedRunStarted = false;
                }

                if (_subscribedRevive)
                {
                    GameSession.ReviveOffered -= HandleReviveOffered;
                    _subscribedRevive = false;
                }

                _analyticsRunBinder?.Dispose();
                _integrations?.Dispose();
                _integrations = null;
                _gameSession?.ClearRun();
                Instance = null;
            }
        }
    }
}
