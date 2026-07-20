using System;
using System.Collections.Generic;
using LastTrain.Enemy;
using LastTrain.Grid;
using LastTrain.Passenger;
using LastTrain.Run;
using UnityEngine;

namespace LastTrain.Battle
{
    /// <summary>
    /// 전투 틱·승객 컨트롤러·적 목록·발사체 풀을 조율한다.
    /// </summary>
    public sealed class BattleManager : MonoBehaviour
    {
        [SerializeField] private GridManager gridManager;
        [SerializeField] private ProjectilePool projectilePool;
        [SerializeField] private float rangeScale = BattleConstants.RangeToWorldScale;

        private readonly EnemyRegistry _enemyRegistry = new();
        private readonly Dictionary<string, PassengerController> _passengerControllers = new();

        private RunState _runState;
        private bool _initialized;

        public EnemyRegistry EnemyRegistry => _enemyRegistry;

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

            gridManager.PassengerDropped -= HandlePassengerDropped;
            gridManager.PassengerDropped += HandlePassengerDropped;

            SyncPassengerControllers();
            _initialized = true;
        }

        public void RegisterEnemy(EnemyRuntime enemy)
        {
            _enemyRegistry.Register(enemy);
        }

        public void ClearEnemies()
        {
            _enemyRegistry.Clear();
        }

        public float ToWorldRange(float dataRange)
        {
            return dataRange * rangeScale;
        }

        private void Update()
        {
            if (!_initialized || _runState == null || projectilePool == null)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            IReadOnlyList<EnemyRuntime> enemies = _enemyRegistry.Enemies;

            foreach (KeyValuePair<string, PassengerController> pair in _passengerControllers)
            {
                PassengerController controller = pair.Value;
                PassengerRuntime runtime = controller.Runtime;

                if (runtime.GridSlotIndex < 0)
                {
                    continue;
                }

                Vector2 position = GetSlotWorldPosition(runtime.GridSlotIndex);
                float worldRange = ToWorldRange(runtime.GetEffectiveRange());

                if (controller.Tick(deltaTime, position, worldRange, enemies, projectilePool))
                {
                    // Attack fired - optional hook for future VFX
                }
            }
        }

        private void OnDestroy()
        {
            if (gridManager != null)
            {
                gridManager.PassengerDropped -= HandlePassengerDropped;
            }
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
            GridSlot slot = gridManager.GetSlot(slotIndex);
            return slot.ContentAnchor.position;
        }
    }
}
