using System;
using LastTrain.Ability;
using LastTrain.Ads;
using LastTrain.Analytics;
using LastTrain.Audio;
using LastTrain.Battle;
using LastTrain.Core;
using LastTrain.Data;
using LastTrain.Difficulty;
using LastTrain.Event;
using LastTrain.Relic;
using LastTrain.Run;
using LastTrain.Shop;
using LastTrain.Synergy;
using UnityEngine;

namespace LastTrain.UI
{
    /// <summary>
    /// Game Scene에서 BattleManager·StationManager를 초기화하고 웨이브를 진행한다.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class GameBattleBootstrap : MonoBehaviour
    {
        [SerializeField] private BattleManager battleManager;
        [SerializeField] private Grid.GridManager gridManager;
        [SerializeField] private GameDatabase gameDatabase;

        [Tooltip("true면 첫 웨이브를 자동 시작. HUD 준비 완료 버튼 사용 시 false 권장.")]
        [SerializeField] private bool autoStartFirstWave;

        private StationManager _stationManager;
        private NonCombatStationServices _nonCombatServices;
        private RouteData _currentRoute;
        private DifficultyModifierRunner _difficultyModifiers;
        private GameSession _gameSession;
        private AbilityPanelController _abilityPanel;
        private SynergyManager _synergyManager;
        private Mission.MissionRunBinder _missionBinder;
        private bool _paused;
        private bool _runEndHandled;

        public StationManager StationManager => _stationManager;
        public NonCombatStationServices NonCombatServices => _nonCombatServices;
        public SynergyManager SynergyManager => _synergyManager;
        public Mission.MissionRunBinder MissionBinder => _missionBinder;
        public GameDatabase GameDatabase => gameDatabase;
        public bool IsPaused => _paused;

        public void SetPaused(bool paused)
        {
            _paused = paused;
        }

        public void RegisterAbilityPanel(AbilityPanelController panel)
        {
            _abilityPanel = panel;
            if (_missionBinder != null && _gameSession?.RunState != null)
            {
                _missionBinder.Bind(
                    _gameSession.RunState,
                    _stationManager,
                    panel != null ? panel.AbilityManager : null,
                    _nonCombatServices?.Shop);
            }
        }

        private void Start()
        {
            if (battleManager == null)
            {
                Debug.LogError("[GameBattleBootstrap] battleManager가 연결되지 않았습니다.", this);
                return;
            }

            if (gridManager == null)
            {
                gridManager = FindAnyObjectByType<Grid.GridManager>();
            }

            AppRoot appRoot = AppRoot.Instance;
            if (appRoot == null || !appRoot.GameSession.HasActiveRun)
            {
                Debug.LogWarning("[GameBattleBootstrap] 활성 RunState가 없습니다. GameGridBootstrap 이후 실행되도록 순서를 확인하세요.", this);
                return;
            }

            _gameSession = appRoot.GameSession;
            RunState runState = _gameSession.RunState;

            if (gameDatabase == null)
            {
                Debug.LogError("[GameBattleBootstrap] gameDatabase가 연결되지 않았습니다.", this);
                return;
            }

            if (!TryResolveStartingStation(runState, out StationData startingStation))
            {
                Debug.LogError(
                    $"[GameBattleBootstrap] lineId={runState.LineId}, stationIndex={runState.Station.CurrentStationIndex} 역 데이터를 찾지 못했습니다.",
                    this);
                return;
            }

            battleManager.Initialize(runState, gridManager, gameDatabase);
            battleManager.SetStationDifficulty(startingStation.DifficultyMultiplier);

            Canvas canvas = gridManager != null ? gridManager.RootCanvas : FindAnyObjectByType<Canvas>();
            UiVfxInstaller.InstallIfMissing(canvas);

            _synergyManager = new SynergyManager(runState, gameDatabase.Synergies);
            _synergyManager.Recalculate();

            int seed = runState.RandomSeed != 0
                ? runState.RandomSeed
                : unchecked(runState.RunId?.GetHashCode() ?? Environment.TickCount);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!runState.IsDailyRun && LastTrain.DebugTools.DebugCombatSettings.FixedSeed.HasValue)
            {
                seed = LastTrain.DebugTools.DebugCombatSettings.FixedSeed.Value;
            }
#endif
            var random = new RandomService(seed);
            var relicManager = new RelicManager(runState, gameDatabase);
            _nonCombatServices = new NonCombatStationServices(
                new ShopService(runState, gameDatabase, relicManager, random),
                new EventService(runState, gameDatabase, relicManager, random),
                relicManager);

            _stationManager = new StationManager(ResolveStationByIndex, _nonCombatServices);
            _stationManager.WaveManager.SetDifficulty(runState.Difficulty);
            _difficultyModifiers = new DifficultyModifierRunner();
            _difficultyModifiers.BeginRun(runState);
            _stationManager.StationStarted += HandleStationStarted;
            _stationManager.AbilityRewardRequested += HandleAbilityRewardRequested;
            _stationManager.RunVictoryRequested += HandleRunVictoryRequested;
            _stationManager.StationRewardGranted += HandleStationRewardGranted;
            _stationManager.Initialize(runState, startingStation);

            if (_abilityPanel == null)
            {
                _abilityPanel = FindAnyObjectByType<AbilityPanelController>();
            }

            _missionBinder?.Dispose();
            _missionBinder = new Mission.MissionRunBinder(gameDatabase);
            _missionBinder.Bind(
                runState,
                _stationManager,
                _abilityPanel != null ? _abilityPanel.AbilityManager : null,
                _nonCombatServices.Shop);

            AnalyticsRunBinder binder = appRoot.AnalyticsRunBinder;
            binder?.BindBattle(_stationManager, battleManager, _synergyManager, gridManager);

            AppRoot.Instance?.Analytics?.BindRun(runState, runState.DifficultyId);

            Tutorial.TutorialDirector director = GetComponent<Tutorial.TutorialDirector>();
            if (director == null)
            {
                director = gameObject.AddComponent<Tutorial.TutorialDirector>();
            }

            director.Begin(
                _stationManager,
                _abilityPanel != null ? _abilityPanel.AbilityManager : null,
                gridManager);

            // Station/Synergy 준비 후 전투 피드백 이벤트 재구독
            UiVfxInstaller feedbackInstaller = canvas != null
                ? canvas.GetComponentInChildren<UiVfxInstaller>(true)
                : FindAnyObjectByType<UiVfxInstaller>();
            feedbackInstaller?.RebindFeedback();

            _gameSession.RunEnded += HandleRunEnded;

            _runEndHandled = false;

            if (autoStartFirstWave)
            {
                _stationManager.TryActivateStation();
            }
        }

        private bool TryResolveStartingStation(RunState runState, out StationData startingStation)
        {
            startingStation = null;
            string lineId = string.IsNullOrWhiteSpace(runState.LineId) ? RouteIds.Default : runState.LineId;
            if (string.Equals(lineId, RouteIds.Endless, StringComparison.Ordinal)
                && gameDatabase.EndlessRoute != null)
            {
                _currentRoute = null;
                return gameDatabase.EndlessRoute.TryGetStationByIndex(
                           runState.Station.CurrentStationIndex,
                           out startingStation)
                       && startingStation != null;
            }

            if (gameDatabase.TryGetRoute(lineId, out _currentRoute) && _currentRoute != null)
            {
                if (_currentRoute.TryGetStationByIndex(runState.Station.CurrentStationIndex, out startingStation)
                    && startingStation != null)
                {
                    return true;
                }

                startingStation = _currentRoute.GetFirstStation();
                return startingStation != null;
            }

            return gameDatabase.TryGetStationByIndex(runState.Station.CurrentStationIndex, out startingStation)
                   && startingStation != null;
        }

        private void Update()
        {
            if (_paused || _stationManager == null || _gameSession == null || !_gameSession.HasActiveRun)
            {
                return;
            }

            _stationManager.Tick(Time.deltaTime, battleManager);
            _gameSession.RunState?.TickElapsed(Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (_gameSession != null)
            {
                _gameSession.RunEnded -= HandleRunEnded;
            }

            if (_stationManager != null)
            {
                _stationManager.StationStarted -= HandleStationStarted;
                _stationManager.AbilityRewardRequested -= HandleAbilityRewardRequested;
                _stationManager.RunVictoryRequested -= HandleRunVictoryRequested;
                _stationManager.StationRewardGranted -= HandleStationRewardGranted;
                _stationManager.Cancel();
            }

            AppRoot.Instance?.AnalyticsRunBinder?.UnbindBattle();
            AppRoot.Instance?.AnalyticsRunBinder?.BindSummon(null);
            AppRoot.Instance?.AnalyticsRunBinder?.BindAbility(null);
            _missionBinder?.Dispose();
            _missionBinder = null;
        }

        private void HandleStationRewardGranted(StationData station, int rewardCoins)
        {
            AdCoordinator ads = AppRoot.Instance?.Ads;
            if (ads == null || station == null || rewardCoins <= 0 || _gameSession?.RunState == null)
            {
                return;
            }

            // 능력 카드 선택 UI와 역 보상 2배 광고가 겹치면 광고 리롤 버튼이 잠시 비활성화된다.
            if (station.GrantsAbilityChoice)
            {
                return;
            }

            ads.Limits.NotifyStationChanged(station.StationIndex);
            if (!ads.IsReady(RewardedAdPlacement.StationRewardDouble))
            {
                return;
            }

            // Mock/실광고: 역 클리어 보상 2배 기회 (취소해도 게임 진행 유지)
            ads.ShowStationRewardDouble(_gameSession.RunState, rewardCoins, null);
        }

        private void HandleStationStarted(StationData station)
        {
            if (station == null || _gameSession?.RunState == null)
            {
                return;
            }

            if (_gameSession.RunState.IsEndlessRun && gameDatabase?.EndlessRoute != null)
            {
                gameDatabase.EndlessRoute.PruneRuntimeCache(station.StationIndex);
            }

            // Modifier를 먼저 적용한 뒤 역 난이도에 반영한다.
            _difficultyModifiers?.OnStationStarted(station);

            if (battleManager != null)
            {
                float enemyMult = _gameSession.RunState.NextStationModifiers.ConsumeEnemyHealthMultiplier();
                float bonus = _gameSession.RunState.DifficultyModifiers.EnemyHealthBonusMultiplier;
                battleManager.SetStationDifficulty(
                    station.DifficultyMultiplier * Mathf.Max(0.01f, bonus),
                    enemyMult);
            }

            AppRoot.Instance?.Analytics?.BindRun(_gameSession.RunState, _gameSession.RunState?.DifficultyId);
        }

        private void HandleAbilityRewardRequested(StationData _)
        {
            if (_abilityPanel != null)
            {
                _abilityPanel.OpenRewardSelection();
                return;
            }

            // 패널이 없으면 즉시 다음 역으로 진행
            _stationManager?.ContinueAfterAbilityReward();
        }

        private void HandleRunVictoryRequested()
        {
            if (_gameSession == null || !_gameSession.HasActiveRun)
            {
                return;
            }

            _gameSession.EndRun(RunEndReason.Victory, isVictory: true);
        }

        private void HandleRunEnded(RunResult result)
        {
            if (_runEndHandled)
            {
                return;
            }

            _runEndHandled = true;
            _stationManager?.Cancel();
            battleManager?.ClearEnemies();

            if (result != null)
            {
                if (result.IsVictory)
                {
                    GameAudio.PlaySfx(SfxId.Victory);
                }
                else if (result.EndReason == RunEndReason.Defeat)
                {
                    GameAudio.PlaySfx(SfxId.Defeat);
                }
            }

            // Result Scene 전환 (오버레이가 있으면 오버레이가 전환을 담당)
            GameEndOverlayController overlay = FindAnyObjectByType<GameEndOverlayController>();
            if (overlay != null)
            {
                overlay.Show(result);
                return;
            }

            SceneFlow.Load(SceneNames.Result);
        }

        private StationData ResolveStationByIndex(int stationIndex)
        {
            if (gameDatabase == null)
            {
                return null;
            }

            string lineId = _gameSession?.RunState?.LineId;
            if (string.Equals(lineId, RouteIds.Endless, StringComparison.Ordinal)
                && gameDatabase.EndlessRoute != null
                && gameDatabase.EndlessRoute.TryGetStationByIndex(stationIndex, out StationData endlessStation))
            {
                return endlessStation;
            }

            if (!string.IsNullOrWhiteSpace(lineId)
                && gameDatabase.TryGetStationByRouteIndex(lineId, stationIndex, out StationData routeStation))
            {
                return routeStation;
            }

            if (_currentRoute != null && _currentRoute.TryGetStationByIndex(stationIndex, out routeStation))
            {
                return routeStation;
            }

            gameDatabase.TryGetStationByIndex(stationIndex, out StationData station);
            return station;
        }
    }
}
