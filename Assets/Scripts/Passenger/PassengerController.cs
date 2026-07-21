using System.Collections.Generic;
using LastTrain.Battle;
using LastTrain.Core;
using LastTrain.Enemy;
using LastTrain.Passenger.Skills;
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
        private readonly IPassengerSkill _skill;

        public PassengerController(
            PassengerRuntime runtime,
            PassengerAttackController attackController = null,
            IPassengerSkill skill = null)
        {
            _runtime = runtime ?? throw new System.ArgumentNullException(nameof(runtime));
            _attackController = attackController ?? new PassengerAttackController();
            _skill = skill ?? NullPassengerSkill.Instance;
        }

        public PassengerRuntime Runtime => _runtime;
        public IPassengerSkill Skill => _skill;

        public bool Tick(
            float deltaTime,
            Vector2 worldPosition,
            float rangeInWorldUnits,
            IReadOnlyList<EnemyRuntime> enemies,
            IProjectileLauncher launcher,
            PassengerSkillContext? skillContext = null,
            float fastEnemyDamagePercent = 0f)
        {
            bool attacked = _attackController.Tick(
                deltaTime,
                _runtime,
                worldPosition,
                rangeInWorldUnits,
                enemies,
                launcher,
                fastEnemyDamagePercent);

            if (skillContext.HasValue)
            {
                _skill.Tick(deltaTime, skillContext.Value);
            }

            return attacked;
        }
    }
}
