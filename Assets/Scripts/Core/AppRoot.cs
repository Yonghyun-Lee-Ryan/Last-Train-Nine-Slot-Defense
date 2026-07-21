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

        private SceneLoader _sceneLoader;
        private GameSession _gameSession;
        private bool _subscribedRunEnded;

        /// <summary>비동기 Scene 전환 담당. AppRoot 생성 시 함께 초기화된다.</summary>
        public SceneLoader SceneLoader => _sceneLoader;

        /// <summary>현재 게임 세션. Scene 전환 후에도 유지된다.</summary>
        public GameSession GameSession => _gameSession ??= new GameSession();

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

            // 저장 파일은 회차 종료 시점에만 정리한다.
            if (!_subscribedRunEnded)
            {
                GameSession.RunEnded += HandleRunEnded;
                _subscribedRunEnded = true;
            }
        }

        private void Initialize()
        {
            ApplyApplicationSettings();

            _sceneLoader = GetComponent<SceneLoader>();
            if (_sceneLoader == null)
            {
                _sceneLoader = gameObject.AddComponent<SceneLoader>();
            }

            Debug.Log("[AppRoot] 초기화 완료.");
        }

        private void Start()
        {
            if (autoLoadMainMenu)
            {
                _sceneLoader.LoadScene(SceneNames.MainMenu);
            }
        }

        private void HandleRunEnded(RunResult _)
        {
            RunSaveSystem.DeleteRunSave();
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

        /// <summary>
        /// 앱 공통 설정을 적용한다. Portrait 고정, 프레임레이트, 슬립 방지 등.
        /// </summary>
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

                _gameSession?.ClearRun();
                Instance = null;
            }
        }
    }
}
