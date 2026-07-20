using System.Collections.Generic;
using LastTrain.Battle;
using LastTrain.Enemy;
using LastTrain.Run;
using UnityEngine;

namespace LastTrain.Passenger
{
    /// <summary>
    /// Grid 승객 1명의 전투 행동. View(PassengerView)와 분리된 로직 계층.
    /// </summary>
    public sealed class PassengerController
    {
        private readonly PassengerRuntime _runtime;
        private readonly PassengerAttackController _attackController;

        public PassengerController(PassengerRuntime runtime, PassengerAttackController attackController = null)
        {
            _runtime = runtime ?? throw new System.ArgumentNullException(nameof(runtime));
            _attackController = attackController ?? new PassengerAttackController();
        }

        public PassengerRuntime Runtime => _runtime;

        public bool Tick(
            float deltaTime,
            Vector2 worldPosition,
            float rangeInWorldUnits,
            IReadOnlyList<EnemyRuntime> enemies,
            IProjectileLauncher launcher)
        {
            return _attackController.Tick(
                deltaTime,
                _runtime,
                worldPosition,
                rangeInWorldUnits,
                enemies,
                launcher);
        }
    }
}
