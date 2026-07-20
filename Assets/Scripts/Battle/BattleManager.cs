using System;
using System.Collections.Generic;
using LastTrain.Data;
using LastTrain.Enemy;
using LastTrain.Grid;
using LastTrain.Passenger;
using LastTrain.Run;
using UnityEngine;

namespace LastTrain.Battle
{
    /// <summary>
    /// 전투 틱·승객 공격·적 이동·객차 피해를 조율한다.
    /// </summary>
    public sealed class BattleManager : MonoBehaviour, IBattleFlowContext
    {
        [Header("References")]
        [SerializeField] private GridManager gridManager;
        [SerializeField] private ProjectilePool projectilePool;
        [SerializeField] private EnemyPool enemyPool;
        [SerializeField] private RectTransform spawnPoint;
        [SerializeField] private RectTransform trainTarget;

        [Header("Combat")]
        [SerializeField] private float rangeScale = BattleConstants.RangeToWorldScale;
        [SerializeField] private float moveSpeedScale = BattleConstants.MoveSpeedToWorldScale;
        [SerializeField] private float trainReachRadius = 32f;
        [SerializeField] private float stationDifficulty = 1f;

        private readonly EnemyRegistry _enemyRegistry = new();
        private readonly Dictionary<string, PassengerController> _passengerControllers = new();
        private readonly Dictionary<string, EnemyController> _activeEnemies = new();

        private RunState _runState;
        private bool _initialized;

        public EnemyRegistry EnemyRegistry => _enemyRegistry;
        public RectTransform SpawnPoint => spawnPoint;
        public RectTransform TrainTarget => trainTarget;

        public void Initialize(RunState runState, GridManager grid)
        {
            if (runState == null)
            {
                throw new ArgumentNullException(nameof(runState));
            }

            _runState = runState;
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

            SyncPassengerControllers();
            _initialized = true;
        }

        public EnemyController SpawnEnemy(EnemyData data, Vector2? spawnPositionOverride = null)
        {
            if (!_initialized || data == null || enemyPool == null || !IsWaveSpawnActive())
            {
                return null;
            }

            Vector2 spawnPosition = spawnPositionOverride
                                    ?? (spawnPoint != null ? (Vector2)spawnPoint.position : Vector2.zero);

            EnemyRuntime runtime = EnemyFactory.CreateRuntime(data, spawnPosition, stationDifficulty);
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
            return controller;
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
        }

        public float ToWorldRange(float dataRange)
        {
            return dataRange * rangeScale;
        }

        public void SetStationDifficulty(float difficulty)
        {
            stationDifficulty = UnityEngine.Mathf.Max(0.01f, difficulty);
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
            TickPassengerAttacks(deltaTime);
        }

        private void TickEnemyMovement(float deltaTime)
        {
            if (trainTarget == null || _activeEnemies.Count == 0)
            {
                return;
            }

            Vector2 targetPosition = trainTarget.position;
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

                bool reached = EnemyMovementService.TickMove(
                    runtime,
                    targetPosition,
                    deltaTime,
                    moveSpeedScale,
                    trainReachRadius);

                controller.SyncTransform();

                if (reached)
                {
                    reachedEnemies.Add(runtime);
                }
            }

            for (int i = 0; i < reachedEnemies.Count; i++)
            {
                TrainDamageService.TryApplyTrainDamage(_runState, reachedEnemies[i]);
            }
        }

        private void TickPassengerAttacks(float deltaTime)
        {
            if (projectilePool == null)
            {
                return;
            }

            IReadOnlyList<EnemyRuntime> enemies = _enemyRegistry.Enemies;
            var passengers = new List<PassengerController>(_passengerControllers.Values);

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
                controller.Tick(deltaTime, position, worldRange, enemies, projectilePool);
            }
        }

        private void HandleEnemyKilled(EnemyRuntime enemy)
        {
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

            if (_activeEnemies.TryGetValue(enemy.InstanceId, out EnemyController controller))
            {
                _activeEnemies.Remove(enemy.InstanceId);
                controller.Release();
            }
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
