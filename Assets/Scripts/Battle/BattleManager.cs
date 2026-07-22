using System;
using System.Collections.Generic;
using LastTrain.Audio;
using LastTrain.Core;
using LastTrain.Data;
using LastTrain.Difficulty;
using LastTrain.Enemy;
using LastTrain.Grid;
using LastTrain.Passenger;
using LastTrain.Passenger.Skills;
using LastTrain.Run;
using UnityEngine;

namespace LastTrain.Battle
{
    /// <summary>
    /// 전투 틱·승객 공격·적 이동·객차 피해를 조율한다.
    /// </summary>
    public sealed class BattleManager : MonoBehaviour, IBattleFlowContext, IEnemySpawner
    {
        [Header("References")]
        [SerializeField] private GridManager gridManager;
        [SerializeField] private ProjectilePool projectilePool;
        [SerializeField] private EnemyPool enemyPool;
        [SerializeField] private RectTransform spawnPoint;
        [SerializeField] private RectTransform[] enemyWaypoints = Array.Empty<RectTransform>();
        [SerializeField] private RectTransform trainTarget;
        [SerializeField] private GameDatabase gameDatabase;
        [SerializeField] private EnemyData bossMinionData;

        [Header("Combat")]
        [SerializeField] private float rangeScale = BattleConstants.RangeToWorldScale;
        [SerializeField] private float moveSpeedScale = BattleConstants.MoveSpeedToWorldScale;
        [SerializeField] private float trainReachRadius = 32f;
        [SerializeField] private float spawnTargetProtectionDistance =
            BattleConstants.SpawnTargetProtectionDistance;
        [SerializeField] private float stationDifficulty = 1f;

        private readonly EnemyRegistry _enemyRegistry = new();
        private readonly Dictionary<string, PassengerController> _passengerControllers = new();
        private readonly Dictionary<string, EnemyController> _activeEnemies = new();
        private readonly TemporaryTurretService _turretService = new();
        private readonly List<BossBrain> _bossBrains = new();
        private readonly System.Random _eliteRandom = new();
        private RandomService _skillRandom;

        private RunState _runState;
        private DifficultyRuntime _difficulty;
        private bool _initialized;

        public event Action<EnemyRuntime> BossSpawned;
        public event Action<EnemyRuntime> BossDespawned;
        public event Action<EnemyRuntime, float, float> BossHealthChanged;
        public event Action<BossPhase, BossPhase> BossPhaseChanged;

        public EnemyRegistry EnemyRegistry => _enemyRegistry;
        public RectTransform SpawnPoint => spawnPoint;
        public RectTransform TrainTarget => trainTarget;
        public EnemyRuntime ActiveBoss => _bossBrains.Count > 0 ? _bossBrains[0].Owner : null;

        public void Initialize(RunState runState, GridManager grid, GameDatabase database = null)
        {
            if (runState == null)
            {
                throw new ArgumentNullException(nameof(runState));
            }

            _runState = runState;
            _difficulty = runState.Difficulty;
            if (database != null)
            {
                gameDatabase = database;
            }

            if (grid != null)
            {
                gridManager = grid;
            }

            if (gridManager == null)
            {
                Debug.LogError("[BattleManager] gridManager가 연결되지 않았습니다.", this);
                return;
            }

            if (projectilePool != null)
            {
                projectilePool.Initialize();
            }

            if (enemyPool != null)
            {
                enemyPool.Initialize();
            }

            gridManager.PassengerDropped -= HandlePassengerDropped;
            gridManager.PassengerDropped += HandlePassengerDropped;

            _skillRandom ??= new RandomService(runState.GetHashCode());
            SyncPassengerControllers();
            _initialized = true;
        }

        /// <summary>디버그로 슬롯 승객을 바꾼 뒤 전투 컨트롤러를 재동기화한다.</summary>
        public void RefreshPassengerControllers()
        {
            SyncPassengerControllers();
        }

        /// <summary>스킬/소환 RNG를 고정 시드로 재설정한다.</summary>
        public void ReseedSkillRandom(int seed)
        {
            _skillRandom ??= new RandomService(seed);
            _skillRandom.Reseed(seed);
        }

        /// <summary>디버그용: Fighting 페이즈에서 보스/적을 강제 스폰한다.</summary>
        public EnemyController DebugForceSpawn(EnemyData data)
        {
            if (!_initialized || data == null || enemyPool == null || _runState == null)
            {
                return null;
            }

            if (_runState.Battle.CurrentPhase != RunPhase.Fighting)
            {
                _runState.Battle.SetPhase(RunPhase.Fighting);
            }

            return SpawnEnemy(data);
        }

        public EnemyController SpawnEnemy(EnemyData data, Vector2? spawnPositionOverride = null)
        {
            if (!_initialized || data == null || enemyPool == null || !IsWaveSpawnActive())
            {
                return null;
            }

            Vector2 spawnPosition = spawnPositionOverride
                                    ?? (spawnPoint != null ? (Vector2)spawnPoint.position : Vector2.zero);

            EnemyRuntime runtime = EnemyFactory.CreateRuntime(
                data,
                spawnPosition,
                stationDifficulty,
                _difficulty);
            ApplyElitePromotion(runtime);
            runtime.SetRouteWaypointIndex(GetInitialRouteWaypointIndex(spawnPosition));
            runtime.SetTargetable(false);
            runtime.Died += HandleEnemyKilled;
            runtime.ReachedTrain += HandleEnemyReachedTrain;

            EnemyController controller = enemyPool.Spawn(runtime);
            if (controller == null)
            {
                runtime.Died -= HandleEnemyKilled;
                runtime.ReachedTrain -= HandleEnemyReachedTrain;
                return null;
            }

            _activeEnemies[runtime.InstanceId] = controller;
            _enemyRegistry.Register(runtime);
            _runState?.RecordEnemyEncounter(runtime);
            TryAttachBossBrain(runtime);
            return controller;
        }

        public bool TrySpawn(EnemyData data, Vector2? position = null)
        {
            return SpawnEnemy(data, position) != null;
        }

        /// <summary>개발 단위 5 호환: 런타임만 등록(이동 View 없음).</summary>
        public void RegisterEnemy(EnemyRuntime enemy)
        {
            if (enemy == null)
            {
                return;
            }

            enemy.Died += HandleEnemyKilled;
            enemy.ReachedTrain += HandleEnemyReachedTrain;
            _enemyRegistry.Register(enemy);
            _runState?.RecordEnemyEncounter(enemy);
        }

        public void ClearEnemies()
        {
            var controllers = new List<EnemyController>(_activeEnemies.Values);
            for (int i = 0; i < controllers.Count; i++)
            {
                EnemyController controller = controllers[i];
                if (controller?.Runtime != null)
                {
                    UnsubscribeEnemyEvents(controller.Runtime);
                }

                controller?.Release();
            }

            _activeEnemies.Clear();
            _enemyRegistry.Clear();
            _turretService.Clear();
            DisposeAllBossBrains();
        }

        public float ToWorldRange(float dataRange)
        {
            return dataRange * rangeScale;
        }

        public void SetStationDifficulty(float difficulty, float eventEnemyHealthMultiplier = 1f)
        {
            stationDifficulty = UnityEngine.Mathf.Max(
                0.01f,
                difficulty * UnityEngine.Mathf.Max(0.01f, eventEnemyHealthMultiplier));
        }

        private void ApplyElitePromotion(EnemyRuntime runtime)
        {
            if (runtime?.Data == null
                || runtime.EnemyType != EnemyType.Normal
                || _difficulty == null
                || !DifficultyCalculator.ShouldPromoteToElite(_difficulty, _eliteRandom))
            {
                return;
            }

            runtime.IsElitePromoted = true;
            runtime.MoveSpeedMultiplier *= 1.1f;
            runtime.TrainDamageMultiplier *= 1.15f;
        }

        public bool TrySpawnEnemy(EnemyData enemyData)
        {
            return SpawnEnemy(enemyData) != null;
        }

        public int GetAliveEnemyCount()
        {
            return _enemyRegistry.Enemies.Count;
        }

        private bool IsCombatActive()
        {
            return _runState != null
                   && _runState.Battle.IsRunActive
                   && BattlePhaseUtility.IsCombatActive(_runState.Battle.CurrentPhase);
        }

        private bool IsWaveSpawnActive()
        {
            return _runState != null
                   && _runState.Battle.IsRunActive
                   && BattlePhaseUtility.IsWaveSpawnActive(_runState.Battle.CurrentPhase);
        }

        private void Update()
        {
            if (!_initialized || _runState == null || !IsCombatActive())
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            TickEnemyMovement(deltaTime);
            TickBossBrains(deltaTime);
            TickTemporaryTurrets(deltaTime);
            TickPassengerAttacks(deltaTime);
        }

        private void TickBossBrains(float deltaTime)
        {
            for (int i = _bossBrains.Count - 1; i >= 0; i--)
            {
                BossBrain brain = _bossBrains[i];
                if (brain == null || brain.Owner == null || !brain.Owner.IsAlive)
                {
                    RemoveBossBrainAt(i);
                    continue;
                }

                brain.Tick(deltaTime);
            }
        }

        private void TickEnemyMovement(float deltaTime)
        {
            if (trainTarget == null || _activeEnemies.Count == 0)
            {
                return;
            }

            var controllers = new List<EnemyController>(_activeEnemies.Values);
            var reachedEnemies = new List<EnemyRuntime>();

            for (int i = 0; i < controllers.Count; i++)
            {
                EnemyController controller = controllers[i];
                EnemyRuntime runtime = controller?.Runtime;
                if (controller == null || runtime == null || !runtime.IsAlive)
                {
                    continue;
                }

                bool movingToWaypoint = TryGetWaypointPosition(runtime.RouteWaypointIndex, out Vector2 targetPosition);
                if (!movingToWaypoint)
                {
                    targetPosition = trainTarget.position;
                }

                runtime.SetRouteSegment(
                    GetRouteSegmentStart(runtime.RouteWaypointIndex, runtime.SpawnPosition),
                    targetPosition);
                bool reachedCurrentTarget = EnemyMovementService.TickMove(
                    runtime,
                    targetPosition,
                    deltaTime,
                    moveSpeedScale,
                    movingToWaypoint ? 8f : trainReachRadius);

                if (!runtime.IsTargetable
                    && Vector2.Distance(runtime.SpawnPosition, runtime.Position)
                    >= spawnTargetProtectionDistance)
                {
                    runtime.SetTargetable(true);
                }

                controller.SyncTransform();

                if (reachedCurrentTarget && movingToWaypoint)
                {
                    runtime.AdvanceRouteWaypoint();
                }
                else if (reachedCurrentTarget)
                {
                    reachedEnemies.Add(runtime);
                }
            }

            for (int i = 0; i < reachedEnemies.Count; i++)
            {
                TrainDamageService.TryApplyTrainDamage(_runState, reachedEnemies[i]);
            }
        }

        private bool TryGetWaypointPosition(int waypointIndex, out Vector2 position)
        {
            if (enemyWaypoints != null
                && waypointIndex >= 0
                && waypointIndex < enemyWaypoints.Length
                && enemyWaypoints[waypointIndex] != null)
            {
                position = enemyWaypoints[waypointIndex].position;
                return true;
            }

            position = default;
            return false;
        }

        private Vector2 GetRouteSegmentStart(int waypointIndex, Vector2 fallback)
        {
            if (waypointIndex <= 0)
            {
                return spawnPoint != null ? (Vector2)spawnPoint.position : fallback;
            }

            int previousIndex = Mathf.Min(waypointIndex - 1, enemyWaypoints.Length - 1);
            RectTransform previous = enemyWaypoints[previousIndex];
            return previous != null ? (Vector2)previous.position : fallback;
        }

        private int GetInitialRouteWaypointIndex(Vector2 position)
        {
            if (enemyWaypoints == null || enemyWaypoints.Length == 0 || trainTarget == null)
            {
                return 0;
            }

            var routePoints = new List<Vector2>(enemyWaypoints.Length + 2);
            routePoints.Add(spawnPoint != null ? (Vector2)spawnPoint.position : position);
            for (int i = 0; i < enemyWaypoints.Length; i++)
            {
                if (enemyWaypoints[i] != null)
                {
                    routePoints.Add(enemyWaypoints[i].position);
                }
            }

            routePoints.Add(trainTarget.position);
            float bestDistanceSq = float.MaxValue;
            int bestSegmentIndex = 0;
            for (int i = 0; i < routePoints.Count - 1; i++)
            {
                Vector2 segmentStart = routePoints[i];
                Vector2 segment = routePoints[i + 1] - segmentStart;
                float segmentLengthSq = segment.sqrMagnitude;
                float t = segmentLengthSq > 0.0001f
                    ? Mathf.Clamp01(Vector2.Dot(position - segmentStart, segment) / segmentLengthSq)
                    : 0f;
                float distanceSq = (position - (segmentStart + segment * t)).sqrMagnitude;
                if (distanceSq < bestDistanceSq)
                {
                    bestDistanceSq = distanceSq;
                    bestSegmentIndex = i;
                }
            }

            return Mathf.Min(bestSegmentIndex, enemyWaypoints.Length);
        }

        private void TickTemporaryTurrets(float deltaTime)
        {
            _turretService.Tick(deltaTime, _enemyRegistry.Enemies);
        }

        private void TickPassengerAttacks(float deltaTime)
        {
            if (projectilePool == null)
            {
                return;
            }

            IReadOnlyList<EnemyRuntime> enemies = _enemyRegistry.Enemies;
            var passengers = new List<PassengerController>(_passengerControllers.Values);
            AbilityModifiers modifiers = _runState.Abilities?.Modifiers ?? AbilityModifiers.Empty;
            SynergyModifiers synergyModifiers = _runState.Synergies?.Modifiers ?? SynergyModifiers.Empty;
            Relic.RelicModifiers relicModifiers = _runState.Relics?.Modifiers ?? Relic.RelicModifiers.Empty;
            float fastEnemyBonus = synergyModifiers.FastEnemyDamagePercent;
            float bossDamageBonus = modifiers.PoliceBossDamagePercent;
            Vector2 spawnPos = spawnPoint != null ? (Vector2)spawnPoint.position : Vector2.zero;
            Vector2 trainPos = trainTarget != null ? (Vector2)trainTarget.position : Vector2.zero;

            for (int i = 0; i < passengers.Count; i++)
            {
                PassengerController controller = passengers[i];
                PassengerRuntime runtime = controller.Runtime;

                if (runtime.GridSlotIndex < 0)
                {
                    continue;
                }

                Vector2 position = GetSlotWorldPosition(runtime.GridSlotIndex);
                float worldRange = ToWorldRange(runtime.GetEffectiveRange());
                var skillContext = new PassengerSkillContext(
                    runtime,
                    position,
                    worldRange,
                    enemies,
                    _runState.Train,
                    modifiers,
                    spawnPos,
                    trainPos,
                    _turretService,
                    _skillRandom,
                    synergyModifiers,
                    relicModifiers.CritChancePercent,
                    relicModifiers.DeveloperTurretDurationPercent);

                controller.Tick(
                    deltaTime,
                    position,
                    worldRange,
                    enemies,
                    projectilePool,
                    skillContext,
                    fastEnemyBonus,
                    bossDamageBonus);
            }
        }

        private void HandleEnemyKilled(EnemyRuntime enemy)
        {
            CombatVisualEvents.RaiseEnemyKilled(enemy);
            EnemyRewardService.TryGrantKillReward(_runState, enemy);
            ReleaseEnemyController(enemy);
        }

        private void HandleEnemyReachedTrain(EnemyRuntime enemy)
        {
            ReleaseEnemyController(enemy);
        }

        private void ReleaseEnemyController(EnemyRuntime enemy)
        {
            if (enemy == null)
            {
                return;
            }

            UnsubscribeEnemyEvents(enemy);
            RemoveBossBrain(enemy);

            if (_activeEnemies.TryGetValue(enemy.InstanceId, out EnemyController controller))
            {
                _activeEnemies.Remove(enemy.InstanceId);
                controller.Release();
            }
        }

        private void TryAttachBossBrain(EnemyRuntime runtime)
        {
            EnemyData minion = ResolveBossMinionData();
            EnemyData splitMinion = ResolveSplitMinionData(runtime);
            BossBrain brain = BossBrain.Create(
                runtime,
                _runState,
                this,
                minion,
                () => _enemyRegistry.Enemies,
                splitMinion);
            if (brain == null)
            {
                return;
            }

            brain.HealthChanged += HandleBossHealthChanged;
            brain.PhaseChanged += HandleBossPhaseChanged;
            _bossBrains.Add(brain);
            float bossDelay = _runState?.Relics?.Modifiers?.BossFirstActionDelaySeconds ?? 0f;
            if (bossDelay > 0f)
            {
                runtime.PauseAbilities(bossDelay);
            }
            BossSpawned?.Invoke(runtime);
            BossHealthChanged?.Invoke(runtime, runtime.CurrentHealth, runtime.MaxHealth);
            GameAudio.PlaySfx(SfxId.BossSpawn);
        }

        private void RemoveBossBrain(EnemyRuntime enemy)
        {
            if (enemy == null)
            {
                return;
            }

            for (int i = _bossBrains.Count - 1; i >= 0; i--)
            {
                if (_bossBrains[i]?.Owner == enemy)
                {
                    RemoveBossBrainAt(i);
                }
            }
        }

        private void RemoveBossBrainAt(int index)
        {
            if (index < 0 || index >= _bossBrains.Count)
            {
                return;
            }

            BossBrain brain = _bossBrains[index];
            EnemyRuntime owner = brain?.Owner;
            if (brain != null)
            {
                brain.HealthChanged -= HandleBossHealthChanged;
                brain.PhaseChanged -= HandleBossPhaseChanged;
                brain.Dispose();
            }

            _bossBrains.RemoveAt(index);
            if (owner != null)
            {
                BossDespawned?.Invoke(owner);
            }
        }

        private void DisposeAllBossBrains()
        {
            for (int i = _bossBrains.Count - 1; i >= 0; i--)
            {
                RemoveBossBrainAt(i);
            }
        }

        private void HandleBossHealthChanged(EnemyRuntime enemy, float current, float max)
        {
            BossHealthChanged?.Invoke(enemy, current, max);
        }

        private void HandleBossPhaseChanged(BossPhase previous, BossPhase next)
        {
            BossPhaseChanged?.Invoke(previous, next);
        }

        private EnemyData ResolveBossMinionData()
        {
            if (bossMinionData != null)
            {
                return bossMinionData;
            }

            if (gameDatabase != null && gameDatabase.TryGetEnemy("enemy_normal", out EnemyData normal))
            {
                return normal;
            }

            return null;
        }

        private EnemyData ResolveSplitMinionData(EnemyRuntime runtime)
        {
            if (runtime?.Data == null || string.IsNullOrWhiteSpace(runtime.Data.SplitMinionId))
            {
                return null;
            }

            if (gameDatabase != null
                && gameDatabase.TryGetEnemy(runtime.Data.SplitMinionId, out EnemyData splitMinion))
            {
                return splitMinion;
            }

            return ResolveBossMinionData();
        }

        private void UnsubscribeEnemyEvents(EnemyRuntime enemy)
        {
            enemy.Died -= HandleEnemyKilled;
            enemy.ReachedTrain -= HandleEnemyReachedTrain;
        }

        private void OnDestroy()
        {
            if (gridManager != null)
            {
                gridManager.PassengerDropped -= HandlePassengerDropped;
            }

            ClearEnemies();
        }

        private void HandlePassengerDropped(int originSlot, int targetSlot, GridDropResult result)
        {
            SyncPassengerControllers();
        }

        private void SyncPassengerControllers()
        {
            if (_runState == null)
            {
                return;
            }

            var activeIds = new HashSet<string>();

            for (int slotIndex = 0; slotIndex < RunState.GridSlotCount; slotIndex++)
            {
                PassengerRuntime passenger = _runState.GetPassengerAtSlot(slotIndex);
                if (passenger == null)
                {
                    continue;
                }

                activeIds.Add(passenger.InstanceId);
                if (!_passengerControllers.ContainsKey(passenger.InstanceId))
                {
                    _passengerControllers[passenger.InstanceId] = PassengerFactory.CreateController(passenger);
                }
            }

            var removeKeys = new List<string>();
            foreach (KeyValuePair<string, PassengerController> pair in _passengerControllers)
            {
                if (!activeIds.Contains(pair.Key))
                {
                    removeKeys.Add(pair.Key);
                }
            }

            for (int i = 0; i < removeKeys.Count; i++)
            {
                _passengerControllers.Remove(removeKeys[i]);
            }
        }

        private Vector2 GetSlotWorldPosition(int slotIndex)
        {
            if (gridManager == null || !gridManager.TryGetSlot(slotIndex, out GridSlot slot))
            {
                return Vector2.zero;
            }

            return slot.ContentAnchor.position;
        }
    }
}
